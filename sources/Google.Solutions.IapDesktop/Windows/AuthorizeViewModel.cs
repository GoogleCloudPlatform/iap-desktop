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

using Google.Apis.Util;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Windows
{
    internal class AuthorizeViewModel : ViewModelBase
    {
        private readonly ISignInAdapter signInAdapter;
        private readonly IDeviceEnrollment deviceEnrollment;

        private CancellationTokenSource cancelCurrentSignin = null;

        public AuthorizeViewModel(
            Control view,
            ISignInAdapter signInAdapter,
            IDeviceEnrollment deviceEnrollment)
        {
            this.View = view.ThrowIfNull(nameof(view));
            this.signInAdapter = signInAdapter.ThrowIfNull(nameof(signInAdapter));
            this.deviceEnrollment = deviceEnrollment;

            //
            // NB. Properties are access from a non-GUI thrad, so
            // they must be thread-safe.
            //
            this.Authorization = ObservableProperty.Build<IAuthorization>(null, this);
            this.AuthorizationError = ObservableProperty.Build<Exception>(null);
            this.IsWaitControlVisible = ObservableProperty.Build(false, this);
            this.IsSignOnControlVisible = ObservableProperty.Build(false, this);
            this.IsCancelButtonVisible = ObservableProperty.Build(false, this);

            this.CancelSignInCommand = ObservableCommand.Build(
                string.Empty,
                CancelSignIn);
            this.TryLoadExistingAuthorizationCommand = ObservableCommand.Build(
                string.Empty,
                TryLoadExistingAuthorizationAsync);
            this.SignInWithDefaultBrowserCommand = ObservableCommand.Build(
                string.Empty,
                () => SignInAsync(BrowserPreference.Default));
            this.SignInWithChromeCommand= ObservableCommand.Build(
                string.Empty,
                () => SignInAsync(BrowserPreference.Chrome));
            this.SignInWithChromeGuestModeCommand = ObservableCommand.Build(
                string.Empty,
                () => SignInAsync(BrowserPreference.ChromeGuest));
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Authorization result. Set after a successful authorization.
        /// </summary>
        public ObservableProperty<IAuthorization> Authorization { get; set; }

        /// <summary>
        /// Authorization error. Set after a failed authorization.
        /// </summary>
        public ObservableProperty<Exception> AuthorizationError { get; set; }

        public ObservableProperty<bool> IsWaitControlVisible { get; set; }

        public ObservableProperty<bool> IsSignOnControlVisible { get; set; }

        public ObservableProperty<bool> IsCancelButtonVisible { get; set; }

        public bool IsChromeSingnInButtonEnabled => ChromeBrowser.IsAvailable;

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
                    var authorization = await AppAuthorization.TryLoadExistingAuthorizationAsync(
                            this.signInAdapter,
                            this.deviceEnrollment,
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
                this.Authorization.Value = await AppAuthorization // TODO: Use browserPreference
                    .CreateAuthorizationAsync(
                        this.signInAdapter,
                        this.deviceEnrollment,
                        this.cancelCurrentSignin.Token)
                    .ConfigureAwait(true);
            }
            catch (Exception e) when (!e.IsCancellation())
            {
                this.AuthorizationError.Value = e;
                throw;
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
