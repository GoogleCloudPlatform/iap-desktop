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
using Google.Solutions.Mvvm.Binding.Commands;
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
            AuthSettingsRepository authSettingsRepository,
            IBindingContext bindingContext)
        {
            InitializeComponent();

            // Don't maximize when double-clicking title bar.
            this.MaximumSize = this.Size;


            var viewModel = new AuthorizeViewModel()
            {
                View = this,
                DeviceEnrollment = deviceEnrollment,
                ClientSecrets = clientSecrets,
                Scopes = scopes,
                TokenStore = authSettingsRepository
            };

            //
            // Bind controls.
            //
            this.spinner.BindReadonlyObservableProperty(
                c => c.Visible,
                viewModel,
                m => m.IsWaitControlVisible,
                bindingContext);
            this.signInButton.BindReadonlyObservableProperty(
                c => c.Visible,
                viewModel,
                m => m.IsSignOnControlVisible,
                bindingContext);
            viewModel.IsSignOnControlVisible.PropertyChanged += (_, __) =>
            {
                if (viewModel.IsSignOnControlVisible.Value)
                {
                    this.signInButton.Focus();
                }
            };

            this.cancelSignInLabel.BindReadonlyObservableProperty(
                c => c.Visible,
                viewModel,
                m => m.IsCancelButtonVisible,
                bindingContext);
            this.cancelSignInLink.BindReadonlyObservableProperty(
                c => c.Visible,
                viewModel,
                m => m.IsCancelButtonVisible,
                bindingContext);

            //
            // Bind sign-in commands.
            //
            this.cancelSignInLink.BindObservableCommand(
                viewModel,
                m => m.CancelSignInCommand,
                bindingContext);
            this.signInButton.BindObservableCommand(
                viewModel,
                m => m.SignInWithDefaultBrowserCommand,
                bindingContext);
            this.signInWithDefaultBrowserMenuItem.BindObservableCommand(
                viewModel,
                m => m.SignInWithDefaultBrowserCommand,
                bindingContext);
            this.signInWithChromeMenuItem.BindObservableCommand(
                viewModel,
                m => m.SignInWithChromeCommand,
                bindingContext);
            this.signInWithChromeGuestMenuItem.BindObservableCommand(
                viewModel,
                m => m.SignInWithChromeGuestModeCommand,
                bindingContext);

            viewModel.Authorization.PropertyChanged += (_, __) =>
            {
                if (viewModel.Authorization.Value != null)
                {
                    //
                    // We're all set, close the dialog.
                    //
                    this.AuthorizationResult = viewModel.Authorization.Value;
                    this.DialogResult = DialogResult.OK;
                    Close();
                }
            };
            viewModel.AuthorizationError.PropertyChanged += (_, __) =>
            {
                if (viewModel.AuthorizationError.Value != null)
                {
                    //
                    // Give up, close the dialog.
                    //
                    this.AuthorizationError = viewModel.AuthorizationError.Value;
                    this.DialogResult = DialogResult.Cancel;
                    Close();
                }
            };

            //
            // Hide focus rectangle.
            //
            this.signInButton.NotifyDefault(false);

            //
            // Try to authorize using saved credentials.
            //
            // NB. Wait until handle has been created so that BeginInvoke
            // calls work properly.
            //
            this.HandleCreated += (_, __) => viewModel
                .TryLoadExistingAuthorizationCommand
                .ExecuteAsync(CancellationToken.None)
                .ContinueWith(
                    t => Debug.Assert(false, "Should never throw an exception"),
                    TaskContinuationOptions.OnlyOnFaulted);
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
            IControlTheme theme,
            IBindingContext bindingContext)
        {
            var dialog = new AuthorizeDialog(
                clientSecrets,
                scopes,
                deviceEnrollment,
                authSettingsRepository,
                bindingContext);
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
