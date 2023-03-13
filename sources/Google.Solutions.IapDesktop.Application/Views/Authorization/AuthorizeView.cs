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

using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Authorization
{
    public partial class AuthorizeView : Form, IView<AuthorizeViewModel>
    {
        public AuthorizeView()
        {
            InitializeComponent();

            // Don't maximize when double-clicking title bar.
            this.MaximumSize = this.Size;
        }

        public void Bind(AuthorizeViewModel viewModel, IBindingContext bindingContext)
        {
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
    }
}
