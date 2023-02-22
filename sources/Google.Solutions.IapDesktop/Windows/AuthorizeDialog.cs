//
// Copyright 2020 Google LLC
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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Theme;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Windows
{
    public partial class AuthorizeDialog : Form
    {
        public IAuthorization AuthorizationResult { get; private set; }

        public Exception AuthorizationError { get; private set; }


        public AuthorizeDialog(
            ClientSecrets clientSecrets,
            IEnumerable<string> scopes,
            IDeviceEnrollment deviceEnrollment,
            AuthSettingsRepository authSettingsRepository)
        {
            InitializeComponent();

            // Don't maximize when double-clicking title bar.
            this.MaximumSize = this.Size;

            var codeReceiver = new BrowserCodeReceiver();
            var signInAdapter = new SignInAdapter(
                deviceEnrollment.Certificate,
                clientSecrets,
                scopes,
                authSettingsRepository,
                codeReceiver);

            var viewModel = new AuthorizeViewModel(this, signInAdapter, deviceEnrollment);

            //
            // Bind controls.
            //
            this.spinner.BindReadonlyProperty(
                c => c.Visible,
                viewModel,
                m => m.IsWaitControlVisible,
                this.Container);
            this.signInButton.BindReadonlyProperty(
                c => c.Visible,
                viewModel,
                m => m.IsSignOnControlVisible,
                this.Container);
            viewModel.OnPropertyChange(
                m => m.IsSignOnControlVisible,
                visible =>
                {
                    if (visible)
                    {
                        this.signInButton.Focus();
                    }
                });

            this.cancelSignInLabel.BindReadonlyProperty(
                c => c.Visible,
                viewModel,
                m => m.IsCancelButtonVisible,
                this.Container);
            this.cancelSignInLink.BindReadonlyProperty(
                c => c.Visible,
                viewModel,
                m => m.IsCancelButtonVisible,
                this.Container);
            this.signInWithChromeMenuItem.BindReadonlyProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsChromeSingnInButtonEnabled,
                this.Container);

            viewModel.OnPropertyChange(
                m => m.Authorization,
                authz =>
                {
                    if (authz != null)
                    {
                        //
                        // We're all set, close the dialog.
                        //
                        this.AuthorizationResult = authz;
                        this.DialogResult = DialogResult.OK;
                        Close();
                    }
                });

            //
            // Manual sign-in.
            //
            CancellationTokenSource cancellationSource = null;
            async void signIn(BrowserPreference browserPreference)
            {
                try
                {
                    cancellationSource?.Dispose();
                    cancellationSource = new CancellationTokenSource();

                    //
                    // Adjust browser preference.
                    //
                    codeReceiver.BrowserPreference = browserPreference;

                    await viewModel
                        .SignInAsync(cancellationSource.Token)
                        .ConfigureAwait(true);
                    Debug.Assert(this.AuthorizationResult != null);
                }
                catch (OperationCanceledException)
                {
                    //
                    // User clicked cancel-link.
                    //
                }
                catch (Exception e)
                {
                    this.AuthorizationError = e;
                    this.DialogResult = DialogResult.Cancel;
                    Close();
                }
            };

            this.signInButton.Click += (s, a) => signIn(BrowserPreference.Default);
            this.signInWithDefaultBrowserMenuItem.Click += (s, a) => signIn(BrowserPreference.Default);
            this.signInWithChromeMenuItem.Click += (s, a) => signIn(BrowserPreference.Chrome);
            this.signInWithChromeGuestMenuItem.Click += (s, a) => signIn(BrowserPreference.ChromeGuest);
            this.cancelSignInLink.Click += (s, a) =>
            {
                cancellationSource?.Cancel();
            };

            //
            // Try to authorize using saved credentials.
            //
            viewModel.TryLoadExistingAuthorizationAsync(CancellationToken.None)
                .ContinueWith(
                    _ => Debug.Assert(false, "Should never throw an exception"),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        //---------------------------------------------------------------------
        // Custom code receiver.
        //---------------------------------------------------------------------

        private class BrowserCodeReceiver : LocalServerCodeReceiver
        {
            public BrowserPreference BrowserPreference { get; set; }
                = BrowserPreference.Default;

            public BrowserCodeReceiver()
                : base(Resources.AuthorizationSuccessful)
            {
            }

            protected override bool OpenBrowser(string url)
            {
                Browser
                    .Get(this.BrowserPreference)
                    .Navigate(url);
                return true;
            }
        }

        //---------------------------------------------------------------------
        // Statics.
        //---------------------------------------------------------------------

        public static IAuthorization Authorize(
            Control parent,
            ClientSecrets clientSecrets,
            IEnumerable<string> scopes,
            IDeviceEnrollment deviceEnrollment,
            AuthSettingsRepository authSettingsRepository,
            IControlTheme theme)
        {
            var dialog = new AuthorizeDialog(
                clientSecrets,
                scopes,
                deviceEnrollment,
                authSettingsRepository);
            theme.ApplyTo(dialog);
            if (dialog.ShowDialog(parent) == DialogResult.OK)
            {
                Debug.Assert(dialog.AuthorizationResult != null);
                return dialog.AuthorizationResult;
            }
            else
            {
                if (dialog.AuthorizationError != null)
                {
                    throw dialog.AuthorizationError;
                }
                else
                {
                    //
                    // User just closed the dialog.
                    //
                    return null;
                }
            }
        }
    }
}
