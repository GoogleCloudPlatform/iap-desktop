//
// Copyright 2022 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile.Auth;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Platform.Net;
using Google.Solutions.Settings.Collection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Windows.Auth
{
    public class AuthorizeViewModel : ViewModelBase
    {
        private readonly ServiceEndpoint<GaiaOidcClient> gaiaEndpoint;
        private readonly ServiceEndpoint<WorkforcePoolClient> stsEndpoint;
        private readonly IOidcOfflineCredentialStore offlineStore;
        private readonly IRepository<IAccessSettings> accessSettings;
        private readonly UserAgent userAgent;

        private CancellationTokenSource? cancelCurrentSignin = null;

        public AuthorizeViewModel(
            ServiceEndpoint<GaiaOidcClient> gaiaEndpoint,
            ServiceEndpoint<WorkforcePoolClient> stsEndpoint,
            IInstall install,
            IOidcOfflineCredentialStore offlineStore,
            IRepository<IAccessSettings> accessSettings,
            HelpClient helpClient,
            UserAgent userAgent)
        {
            this.gaiaEndpoint = gaiaEndpoint.ExpectNotNull(nameof(gaiaEndpoint));
            this.stsEndpoint = stsEndpoint.ExpectNotNull(nameof(stsEndpoint));
            this.offlineStore = offlineStore.ExpectNotNull(nameof(offlineStore));
            this.accessSettings = accessSettings.ExpectNotNull(nameof(accessSettings));
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));

            //
            // NB. Properties are accessed from a non-GUI thread, so
            // they must be thread-safe.
            //
            this.WindowTitle = ObservableProperty.Build($"Sign in - {Install.ProductName}");
            this.IntroductionText = ObservableProperty.Build(
                "Sign in to access your \r\nGoogle Cloud VMs.");
            this.Version = ObservableProperty.Build($"Version {install.CurrentVersion}");
            this.IsShowOptionsMenuEnabled = ObservableProperty.Build(true);

            this.IsWaitControlVisible = ObservableProperty.Build(false, this);
            this.IsSignOnControlVisible = ObservableProperty.Build(true, this);
            this.IsCancelButtonVisible = ObservableProperty.Build(false, this);
            this.IsChromeSingnInButtonEnabled = ObservableProperty.Build(ChromeBrowser.IsAvailable);
            this.IsAuthorizationComplete = ObservableProperty.Build(false, this);

            this.HelpCommand = ObservableCommand.Build(
                string.Empty,
                () => helpClient.OpenTopic(HelpTopics.SignInTroubleshooting));
            this.CancelSignInCommand = ObservableCommand.Build(
                string.Empty,
                CancelSignIn);
            this.TryLoadExistingAuthorizationCommand = ObservableCommand.Build(
                string.Empty,
                TryLoadExistingAuthorizationAsync);
            this.SignInWithDefaultBrowserCommand = ObservableCommand.Build(
                string.Empty,
                () => SignInAsync(BrowserPreference.Default));
            this.SignInWithChromeCommand = ObservableCommand.Build(
                string.Empty,
                () => SignInAsync(BrowserPreference.Chrome),
                this.IsChromeSingnInButtonEnabled);
            this.SignInWithChromeGuestModeCommand = ObservableCommand.Build(
                string.Empty,
                () => SignInAsync(BrowserPreference.ChromeGuest),
                this.IsChromeSingnInButtonEnabled);
            this.ShowOptionsCommand = ObservableCommand.Build(
                string.Empty,
                () => this.ShowOptions?.Invoke(this, EventArgs.Empty),
                this.IsShowOptionsMenuEnabled);
        }

        private protected virtual Authorization CreateAuthorization()
        {
            Debug.Assert(this.Authorization == null);
            Debug.Assert(this.DeviceEnrollment != null);
            Debug.Assert(this.ClientRegistrations != null);

            Precondition.ExpectNotNull(this.DeviceEnrollment, nameof(this.DeviceEnrollment));
            Precondition.ExpectNotNull(this.ClientRegistrations, nameof(this.ClientRegistrations));

            OidcIssuer issuer;
            WorkforcePoolProviderLocator? providerLocator = null;
            if (this.accessSettings.GetSettings().WorkforcePoolProvider.Value
                is var provider &&
                !string.IsNullOrEmpty(provider) &&
                WorkforcePoolProviderLocator.TryParse(provider, out providerLocator))
            {
                //
                // Use workforce identity.
                //
                issuer = OidcIssuer.Sts;
            }
            else
            {
                //
                // Default to Gaia.
                //
                issuer = OidcIssuer.Gaia;
            }

            var registration = this.ClientRegistrations
                .EnsureNotNull()
                .FirstOrDefault(r => r.Issuer == issuer);

            if (registration == null)
            {
                throw new ArgumentException(
                    $"Missing client registration for issuer {issuer}");
            }

            IOidcClient client;
            if (registration.Issuer == OidcIssuer.Sts)
            {
                Debug.Assert(providerLocator != null);

                client = new WorkforcePoolClient(
                    this.stsEndpoint,
                    this.DeviceEnrollment.ExpectNotNull(nameof(this.DeviceEnrollment)),
                    this.offlineStore,
                    providerLocator!,
                    registration,
                    this.userAgent);
            }
            else
            {
                client = new GaiaOidcClient(
                    this.gaiaEndpoint,
                    this.DeviceEnrollment.ExpectNotNull(nameof(this.DeviceEnrollment)),
                    this.offlineStore,
                    registration,
                    this.userAgent);
            }

            return new Authorization(
                client,
                this.DeviceEnrollment);
        }

        //---------------------------------------------------------------------
        // Events to interact with view.
        //---------------------------------------------------------------------

        /// <summary>
        /// One or more requires scopes haven't been granted.
        /// </summary>
        public EventHandler<RecoverableExceptionEventArgs>? OAuthScopeNotGranted;

        /// <summary>
        /// An error occurred that might be due to network misconfiguration.
        /// </summary>
        public EventHandler<RecoverableExceptionEventArgs>? NetworkError;

        /// <summary>
        /// User requested to show options.
        /// </summary>
        public EventHandler<EventArgs>? ShowOptions;

        //---------------------------------------------------------------------
        // Input properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Device enrollment, must be initialized.
        /// </summary>
        public IDeviceEnrollment? DeviceEnrollment { get; set; }

        /// <summary>
        /// List of client registrations. There must be at least one
        /// registration for each supported issuer.
        /// </summary>
        public IList<OidcClientRegistration>? ClientRegistrations { get; set; }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableProperty<string> WindowTitle { get; }

        public ObservableProperty<string> IntroductionText { get; }

        public ObservableProperty<string> Version { get; }

        public ObservableProperty<bool> IsWaitControlVisible { get; }

        public ObservableProperty<bool> IsSignOnControlVisible { get; }

        public ObservableProperty<bool> IsCancelButtonVisible { get; }

        public ObservableProperty<bool> IsChromeSingnInButtonEnabled { get; }

        public ObservableProperty<bool> IsShowOptionsMenuEnabled { get; }

        public ObservableProperty<bool> IsAuthorizationComplete { get; }

        //---------------------------------------------------------------------
        // Input/output properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Authorization
        /// * If set to null, a new Authorization is created.
        /// * If non-null, a reauthorization is performed.
        /// </summary>
        public IAuthorization Authorization { get; private set; }

        /// <summary>
        /// Reauthorize using an existing authorization object.
        /// </summary>
        public void UseExistingAuthorization(IAuthorization authorization)
        {
            Debug.Assert(this.Authorization == null);

            this.Authorization = authorization.ExpectNotNull(nameof(authorization));
            this.WindowTitle.Value = "Session expired";
            this.IsShowOptionsMenuEnabled.Value = false;
            this.IntroductionText.Value =
                "Your session has expired.\nSign in again to continue using IAP Destop.";
        }

        //---------------------------------------------------------------------
        // Observable commands.
        //---------------------------------------------------------------------

        public ObservableCommand HelpCommand { get; }
        public ObservableCommand CancelSignInCommand { get; }
        public ObservableCommand TryLoadExistingAuthorizationCommand { get; }
        public ObservableCommand SignInWithDefaultBrowserCommand { get; }
        public ObservableCommand SignInWithChromeCommand { get; }
        public ObservableCommand SignInWithChromeGuestModeCommand { get; }
        public ObservableCommand ShowOptionsCommand { get; }

        //---------------------------------------------------------------------
        // Sign-in logic.
        //---------------------------------------------------------------------

        private void CancelSignIn()
        {
            Debug.Assert(this.cancelCurrentSignin != null);
            this.cancelCurrentSignin?.Cancel();
        }

        private Task TryLoadExistingAuthorizationAsync()
        {
            this.IsSignOnControlVisible.Value = false;
            this.IsWaitControlVisible.Value = true;
            this.IsCancelButtonVisible.Value = false;

            //
            // This method is called on the GUI thread, but we don't want to 
            // block that. So continue on a background thread, but force
            // all events back to the GUI thread.
            //
            return Task.Run(async () =>
            {
                try
                {
                    var authorization = CreateAuthorization();

                    if (await authorization
                        .TryAuthorizeSilentlyAsync(CancellationToken.None)
                        .ConfigureAwait(false))
                    {
                        //
                        // We have existing credentials, there is no need to even
                        // show the "Sign In" button.
                        //
                        this.Authorization = authorization;
                        this.IsAuthorizationComplete.Value = true;
                    }
                    else
                    {
                        //
                        // No valid credentials present, request user to authroize
                        // by showing the "Sign In" button.
                        //
                        this.IsSignOnControlVisible.Value = true;
                        this.IsWaitControlVisible.Value = false;
                    }
                }
                catch (Exception)
                {
                    //
                    // Something went wrong trying to load existing credentials.
                    //
                    this.IsSignOnControlVisible.Value = true;
                    this.IsWaitControlVisible.Value = false;
                }
            });
        }

        private async Task SignInAsync(BrowserPreference browserPreference)
        {
            this.cancelCurrentSignin?.Dispose();
            this.cancelCurrentSignin = new CancellationTokenSource();

            this.IsSignOnControlVisible.Value = false;
            this.IsWaitControlVisible.Value = true;
            this.IsCancelButtonVisible.Value = true;

            try
            {
                var retry = true;
                while (retry)
                {
                    try
                    {
                        Authorization authorization;
                        if (this.Authorization == null)
                        {
                            //
                            // First-time authorization.
                            //
                            authorization = CreateAuthorization();
                        }
                        else
                        {
                            //
                            // We're reauthorizing. Don't let the user change issuers.
                            //
                            authorization = (Authorization)this.Authorization;
                        }

                        await authorization
                            .AuthorizeAsync(
                                browserPreference,
                                this.cancelCurrentSignin.Token)
                            .ConfigureAwait(true);

                        //
                        // Authorization successful.
                        //
                        retry = false;

                        this.Authorization = authorization;
                        this.IsAuthorizationComplete.Value = true;
                    }
                    catch (OAuthScopeNotGrantedException e)
                    {
                        var args = new RecoverableExceptionEventArgs(e);
                        this.OAuthScopeNotGranted?.Invoke(this, args);
                        retry = args.Retry;
                    }
                    catch (Exception e) when (!e.IsCancellation())
                    {
                        var args = new RecoverableExceptionEventArgs(e);
                        this.NetworkError?.Invoke(this, args);
                        retry = args.Retry;
                    }
                }
            }
            finally
            {
                this.IsSignOnControlVisible.Value = true;
                this.IsWaitControlVisible.Value = false;
                this.IsCancelButtonVisible.Value = false;
            }
        }
    }
}
