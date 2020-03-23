//
// Copyright 2019 Google LLC
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
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Solutions.Compute.Auth;

namespace Google.Solutions.IapDesktop.Windows
{
    public partial class AuthorizeDialog : Form
    {
        private IAuthorization authorization;

        public AuthorizeDialog()
        {
            InitializeComponent();
        }

        private void ToggleSignInButton()
        {
            this.spinner.Visible = !this.spinner.Visible;
            this.signInButton.Visible = !this.signInButton.Visible;
            this.signInLabel.Visible = !this.signInLabel.Visible;

            this.signInButton.Focus();
        }
        
        public static IAuthorization Authorize(
            Control parent, 
            ClientSecrets clientSecrets, 
            string[] scopes,
            IDataStore dataStore)
        {
            var initializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = scopes,
                DataStore = dataStore
            };

            using (var dialog = new AuthorizeDialog())
            {
                Task.Run(async () =>
                {
                    try
                    {
                        // Try to authorize using OAuth.
                        dialog.authorization = await OAuthAuthorization.TryLoadExistingAuthorizationAsync(
                            initializer,
                            Resources.AuthorizationSuccessful);

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

                    dialog.authorization = await OAuthAuthorization.CreateAuthorizationAsync(
                        initializer,
                        Resources.AuthorizationSuccessful);

                    dialog.Close();
                };

                dialog.ShowDialog(parent);
                return dialog.authorization;
            }
        }
    }
}
