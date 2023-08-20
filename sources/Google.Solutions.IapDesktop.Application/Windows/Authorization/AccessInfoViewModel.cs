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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Platform.Net;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

#pragma warning disable CA1822 // Mark members as static

namespace Google.Solutions.IapDesktop.Application.Windows.Authorization
{
    public class AccessInfoViewModel : ViewModelBase
    {
        private readonly IDeviceEnrollment enrollment;

        public string AccessInfoText { get; }
        public string DetailsLinkCaption { get; }
        public bool IsDetailsLinkVisible { get; }

        public AccessInfoViewModel(
            IAuthorization authorization,
            ServiceRoute route)
        {
            this.enrollment = authorization.DeviceEnrollment;

            if (route.UsePrivateServiceConnect)
            {
                this.AccessInfoText =
                    "Connected to Google Cloud " +
                    "through Private Service Connect";
                this.IsDetailsLinkVisible = false;
            }
            else
            {
                switch (this.enrollment.State)
                {
                    case DeviceEnrollmentState.Disabled:
                        this.AccessInfoText =
                            "Cerfificate-based access is disabled.";
                        this.IsDetailsLinkVisible = false;
                        this.DetailsLinkCaption = "More information";
                        break;

                    case DeviceEnrollmentState.NotEnrolled:
                        this.AccessInfoText =
                            "Cerfificate-based access is enabled," +
                            "but no suitable certificate was found.";
                        this.IsDetailsLinkVisible = false;
                        this.DetailsLinkCaption = "More information";
                        break;

                    case DeviceEnrollmentState.Enrolled:
                        this.AccessInfoText =
                            "Connected to Google Cloud using " +
                            "BeyondCorp certificate-based access.";
                        this.IsDetailsLinkVisible = true;
                        this.DetailsLinkCaption = "View certificate";
                        break;
                }
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        private void OpenEndpointVerificationHelp()
        {
            Browser.Default.Navigate(
                "https://cloud.google.com/endpoint-verification/docs/overview");
        }

        private void OpenDeviceCertificate(IWin32Window owner)
        {
            //
            // NB. Use parent handle instead of this.View as the flyout
            // window might have already been destroyed.
            //
            X509Certificate2UI.DisplayCertificate(
                this.enrollment.Certificate,
                owner.Handle);
        }

        public void OpenDetails(IWin32Window owner)
        {
            Debug.Assert(owner != null);

            switch (this.enrollment.State)
            {
                case DeviceEnrollmentState.Enrolled:
                    OpenDeviceCertificate(owner);
                    break;

                default:
                    OpenEndpointVerificationHelp();
                    break;
            }
        }
    }
}
