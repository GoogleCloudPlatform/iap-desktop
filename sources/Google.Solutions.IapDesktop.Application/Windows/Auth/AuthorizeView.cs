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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.Properties;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Mvvm.Controls;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Auth
{
    [SkipCodeCoverage("UI")]
    public partial class AuthorizeView : CompositeForm, IView<AuthorizeViewModel>
    {
        public AuthorizeView()
        {
            InitializeComponent();

            //
            // Don't maximize when double-clicking title bar.
            //
            this.MaximumSize = this.Size;

            //
            // Show the right icon in Alt+Tab.
            //
            this.Icon = Resources.logo;

            this.introLabel.Location = new System.Drawing.Point(0, 116);
            this.introLabel.Size = new System.Drawing.Size(this.Width, 48);
            this.signInButton.CenterHorizontally(this);
        }

        public void Bind(AuthorizeViewModel viewModel, IBindingContext bindingContext)
        {
            //
            // Bind controls.
            //
            this.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.WindowTitle,
                bindingContext);
            this.introLabel.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.IntroductionText,
                bindingContext);
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
            this.helpLink.BindReadonlyObservableProperty(
                c => c.Visible,
                viewModel,
                m => m.IsCancelButtonVisible,
                bindingContext);
            this.versionLabel.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.Version,
                bindingContext);

            //
            // Bind sign-in commands.
            //
            this.cancelSignInLink.BindObservableCommand(
                viewModel,
                m => m.CancelSignInCommand,
                bindingContext);
            this.helpLink.BindObservableCommand(
                viewModel,
                m => m.HelpCommand,
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
            this.showOptionsToolStripMenuItem.BindObservableCommand(
                viewModel,
                m => m.ShowOptionsCommand,
                bindingContext);

            viewModel.IsAuthorizationComplete.PropertyChanged += (_, __) =>
            {
                if (viewModel.IsAuthorizationComplete.Value)
                {
                    //
                    // We're all set, close the dialog.
                    //
                    this.DialogResult = DialogResult.OK;
                    Close();
                }
            };

            //
            // Hide focus rectangle.
            //
            this.signInButton.NotifyDefault(false);

            if (viewModel.Authorization == null)
            {
                //
                // There's no authorization object yet, so this isn't a
                // reauthorization, but an initial authorization.
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
}
