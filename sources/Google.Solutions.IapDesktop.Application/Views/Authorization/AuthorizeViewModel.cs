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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Properties;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Platform.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Views.Authorization
{
    public class AuthorizeViewModel : ViewModelBase
    {
        private CancellationTokenSource cancelCurrentSignin = null;

        public AuthorizeViewModel(IInstall install)
        {
            //
            // NB. Properties are access from a non-GUI thread, so
            // they must be thread-safe.
            //
            this.WindowTitle = ObservableProperty.Build($"Sign in - {Install.FriendlyName}");
            this.Version = ObservableProperty.Build($"Version {install.CurrentVersion}");
            this.Authorization = ObservableProperty.Build<IAuthorization>(null, this);
            this.IsWaitControlVisible = ObservableProperty.Build(false, this);
            this.IsSignOnControlVisible = ObservableProperty.Build(false, this);
            this.IsCancelButtonVisible = ObservableProperty.Build(false, this);
            this.IsChromeSingnInButtonEnabled = ObservableProperty.Build(ChromeBrowser.IsAvailable);

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
        }

        protected virtual ISignInAdapter CreateSignInAdapter(BrowserPreference preference)
        {
            Precondition.ExpectNotNull(this.DeviceEnrollment, nameof(this.DeviceEnrollment));
            Precondition.ExpectNotNull(this.ClientSecrets, nameof(this.ClientSecrets));
            Precondition.ExpectNotNull(this.Scopes, nameof(this.Scopes));
            Precondition.ExpectNotNull(this.TokenStore, nameof(this.TokenStore));

            return new SignInAdapter(
                this.DeviceEnrollment.Certificate,
                this.ClientSecrets,
                this.Scopes,
                this.TokenStore,
                new BrowserCodeReceiver(preference));
        }

        //---------------------------------------------------------------------
        // Events to interact with view.
        //---------------------------------------------------------------------

        /// <summary>
        /// One or more requires scopes haven't been granted.
        /// </summary>
        public EventHandler<RecoverableExceptionEventArgs> OAuthScopeNotGranted;

        /// <summary>
        /// An error occurred that might be due to network misconfiguration.
        /// </summary>
        public EventHandler<RecoverableExceptionEventArgs> NetworkError;

        //---------------------------------------------------------------------
        // Input properties.
        //---------------------------------------------------------------------

        public IDeviceEnrollment DeviceEnrollment { get; set; }
        public ClientSecrets ClientSecrets { get; set; }
        public IEnumerable<string> Scopes { get; set; }
        public IDataStore TokenStore { get; set; }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableProperty<string> WindowTitle { get; }
        public ObservableProperty<string> Version { get; }

        public ObservableProperty<bool> IsWaitControlVisible { get; set; }

        public ObservableProperty<bool> IsSignOnControlVisible { get; set; }

        public ObservableProperty<bool> IsCancelButtonVisible { get; set; }

        public ObservableProperty<bool> IsChromeSingnInButtonEnabled { get; }

        //---------------------------------------------------------------------
        // Output properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Authorization result. Set after a successful authorization.
        /// </summary>
        public ObservableProperty<IAuthorization> Authorization { get; set; }

        //---------------------------------------------------------------------
        // Observable commands.
        //---------------------------------------------------------------------

        public ObservableCommand CancelSignInCommand { get; }
        public ObservableCommand TryLoadExistingAuthorizationCommand { get; }
        public ObservableCommand SignInWithDefaultBrowserCommand { get; }
        public ObservableCommand SignInWithChromeCommand { get; }
        public ObservableCommand SignInWithChromeGuestModeCommand { get; }

        //---------------------------------------------------------------------
        // Sign-in logic.
        //---------------------------------------------------------------------

        private void CancelSignIn()
        {
            Debug.Assert(this.cancelCurrentSignin != null);
            this.cancelCurrentSignin.Cancel();
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
                    // Try to authorize using OAuth.
                    var authorization = await Services.Auth.Authorization.TryLoadExistingAuthorizationAsync(
                            CreateSignInAdapter(BrowserPreference.Default),
                            this.DeviceEnrollment,
                            CancellationToken.None)
                        .ConfigureAwait(false);

                    if (authorization != null)
                    {
                        //
                        // We have existing credentials, there is no need to even
                        // show the "Sign In" button.
                        //
                        this.Authorization.Value = authorization;
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
                bool retry = true;
                while (retry)
                {
                    try
                    {
                        //
                        // Clear any existing token to force a fresh authentication.
                        //
                        await this.TokenStore.ClearAsync();

                        this.Authorization.Value = await Services.Auth.Authorization
                            .CreateAuthorizationAsync(
                                CreateSignInAdapter(browserPreference),
                                this.DeviceEnrollment,
                                this.cancelCurrentSignin.Token)
                            .ConfigureAwait(true);

                        //
                        // Authorization successful.
                        //
                        retry = false;
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

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class BrowserCodeReceiver : LocalServerCodeReceiver
        {
            private readonly BrowserPreference browserPreference;

            public BrowserCodeReceiver(BrowserPreference browserPreference)
                : base(Resources.AuthorizationSuccessful)
            {
                this.browserPreference = browserPreference;
            }

            protected override bool OpenBrowser(string url)
            {
                Browser
                    .Get(this.browserPreference)
                    .Navigate(url);
                return true;
            }
        }
    }
}
