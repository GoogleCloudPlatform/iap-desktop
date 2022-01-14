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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Windows
{
    public partial class AuthorizeDialog : Form
    {
        private IAuthorization authorization;

        public AuthorizeDialog()
        {
            InitializeComponent();

            // Don't maximize when double-clicking title bar.
            this.MaximumSize = this.Size;
        }

        private void ToggleSignInButton()
        {
            this.spinner.Visible = !this.spinner.Visible;
            this.signInButton.Visible = !this.signInButton.Visible;

            this.signInButton.Focus();
        }

        public static IAuthorization Authorize(
            Control parent,
            ClientSecrets clientSecrets,
            string[] scopes,
            IDataStore dataStore)
        {
            // N.B. Do not dispose the adapter (and embedded GoogleAuthorizationCodeFlow)
            // as it might be needed for token refreshes later.
            var oauthAdapter = new SignInAdapter(
                clientSecrets,
                scopes,
                dataStore,
                Resources.AuthorizationSuccessful);

            Exception caughtException = null;
            using (var dialog = new AuthorizeDialog())
            {
                Task.Run(async () =>
                {
                    try
                    {
                        // Try to authorize using OAuth.
                        dialog.authorization = await AppAuthorization.TryLoadExistingAuthorizationAsync(
                                oauthAdapter,
                                CancellationToken.None)
                            .ConfigureAwait(true);

                        if (dialog.authorization != null)
                        {
                            // We have existing credentials, there is no need to even
                            // show the "Sign In" button.
                            parent.BeginInvoke((Action)(() => dialog.Close()));
                        }
                        else
                        {
                            // No valid credentials present, request user to authroize
                            // by showing the "Sign In" button.
                            parent.BeginInvoke((Action)(() => dialog.ToggleSignInButton()));
                        }
                    }
                    catch (Exception)
                    {
                        // Something went wrong trying to load existing credentials.
                        parent.BeginInvoke((Action)(() => dialog.ToggleSignInButton()));
                    }
                });

                dialog.signInButton.Click += async (sender, args) =>
                {
                    // Switch to showing spinner so that a user cannot click twice.
                    dialog.ToggleSignInButton();

                    try
                    {
                        dialog.authorization = await AppAuthorization.CreateAuthorizationAsync(
                                oauthAdapter,
                                CancellationToken.None)
                            .ConfigureAwait(true);
                    }
                    catch (Exception e)
                    {
                        caughtException = e;
                    }

                    dialog.Close();
                };

                dialog.ShowDialog(parent);

#pragma warning disable CA1508 // Avoid dead conditional code
                if (caughtException != null)
                {
                    throw caughtException;
                }
                else
                {
                    return dialog.authorization;
                }
#pragma warning restore CA1508 // Avoid dead conditional code
            }
        }
    }
}
