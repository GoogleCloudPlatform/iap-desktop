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
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using System;
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
            ISignInAdapter signInAdapter,
            IDeviceEnrollment deviceEnrollment)
        {
            InitializeComponent();

            // Don't maximize when double-clicking title bar.
            this.MaximumSize = this.Size;

            var viewModel = new AuthorizeViewModel(this, signInAdapter, deviceEnrollment);

            this.spinner.BindProperty(
                c => c.Visible,
                viewModel,
                m => m.IsWaitControlVisible,
                this.Container);
            this.signInButton.BindProperty(
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

            this.signInButton.Click += async (sender, args) =>
            {
                try
                {
                    await viewModel
                        .SignInAsync(CancellationToken.None)
                        .ConfigureAwait(true);
                    Debug.Assert(this.AuthorizationResult != null);
                }
                catch (Exception e)
                {
                    this.AuthorizationError = e;
                    this.DialogResult = DialogResult.Cancel;
                    Close();
                }
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
        // Statics.
        //---------------------------------------------------------------------

        public static IAuthorization Authorize(
            Control parent,
            ISignInAdapter signInAdapter,
            IDeviceEnrollment deviceEnrollment)
        {
            var dialog = new AuthorizeDialog(signInAdapter, deviceEnrollment);
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
