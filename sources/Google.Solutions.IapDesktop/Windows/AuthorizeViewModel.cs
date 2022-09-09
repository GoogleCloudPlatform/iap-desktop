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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Windows
{
    internal class AuthorizeViewModel : ViewModelBase
    {
        private readonly ISignInAdapter signInAdapter;
        private readonly IDeviceEnrollment deviceEnrollment;

        private bool isWaitControlVisible = false;
        private bool isSignOnControlVisible = false;
        private bool isCancelButtonVisible = false;
        private IAuthorization authorization;

        public AuthorizeViewModel(
            Control view,
            ISignInAdapter signInAdapter,
            IDeviceEnrollment deviceEnrollment)
        {
            this.View = view.ThrowIfNull(nameof(view));
            this.signInAdapter = signInAdapter.ThrowIfNull(nameof(signInAdapter));
            this.deviceEnrollment = deviceEnrollment.ThrowIfNull(nameof(deviceEnrollment));
        }

        private void FireAndForgetOnGuiThread(Action action)
        {
            ((Control)this.View).BeginInvoke(action);
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public IAuthorization Authorization
        {
            get => this.authorization;
            private set
            {
                this.authorization = value;
                RaisePropertyChange();
            }
        }

        public bool IsWaitControlVisible
        {
            get => this.isWaitControlVisible;
            private set
            {
                this.isWaitControlVisible = value;
                RaisePropertyChange();
            }
        }

        public bool IsSignOnControlVisible
        {
            get => this.isSignOnControlVisible;
            private set
            {
                this.isSignOnControlVisible = value;
                RaisePropertyChange();
            }
        }

        public bool IsCancelButtonVisible
        {
            get => this.isCancelButtonVisible;
            private set
            {
                this.isCancelButtonVisible = value;
                RaisePropertyChange();
            }
        }

        public bool IsChromeSingnInButtonEnabled => ChromeBrowser.IsAvailable;

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public Task TryLoadExistingAuthorizationAsync(CancellationToken cancellationToken)
        {
            this.IsSignOnControlVisible = false;
            this.IsWaitControlVisible = true;
            this.IsCancelButtonVisible = false;

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
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (authorization != null)
                    {
                        //
                        // We have existing credentials, there is no need to even
                        // show the "Sign In" button.
                        //
                        FireAndForgetOnGuiThread(() =>
                        {
                            this.Authorization = authorization;
                        });
                    }
                    else
                    {
                        //
                        // No valid credentials present, request user to authroize
                        // by showing the "Sign In" button.
                        //
                        FireAndForgetOnGuiThread(() =>
                        {
                            this.IsSignOnControlVisible = true;
                            this.IsWaitControlVisible = false;
                        });
                    }
                }
                catch (Exception)
                {
                    //
                    // Something went wrong trying to load existing credentials.
                    //
                    FireAndForgetOnGuiThread(() =>
                    {
                        this.IsSignOnControlVisible = true;
                        this.IsWaitControlVisible = false;
                    });
                }
            });
        }

        public async Task SignInAsync(CancellationToken cancellationToken)
        {
            this.IsSignOnControlVisible = false;
            this.IsWaitControlVisible = true;
            this.IsCancelButtonVisible = true;

            try
            {
                this.Authorization = await AppAuthorization.CreateAuthorizationAsync(
                        this.signInAdapter,
                        this.deviceEnrollment,
                        cancellationToken)
                    .ConfigureAwait(true);
            }
            finally
            {
                this.IsSignOnControlVisible = true;
                this.IsWaitControlVisible = false;
                this.IsCancelButtonVisible = false;
            }
        }
    }
}
