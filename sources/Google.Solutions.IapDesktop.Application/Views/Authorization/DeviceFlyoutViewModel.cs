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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

#pragma warning disable CA1822 // Mark members as static

namespace Google.Solutions.IapDesktop.Application.Views.Authorization
{
    public class DeviceFlyoutViewModel : ViewModelBase
    {
        private const string ProductName = "Endpoint Verification";
        private readonly IDeviceEnrollment enrollment;

        public bool IsDeviceEnrolledIconVisible { get; }
        public bool IsDeviceNotEnrolledIconVisible { get; }
        public string EnrollmentStateDescription { get; }
        public string DetailsLinkCaption { get; }
        public bool IsDetailsLinkVisible { get; }

        public DeviceFlyoutViewModel(
            IWin32Window window,
            IDeviceEnrollment enrollment)
        {
            this.View = window;
            this.enrollment = enrollment;

            switch (enrollment.State)
            {
                case DeviceEnrollmentState.Disabled:
                    this.EnrollmentStateDescription =
                        $"{ProductName} is not available on this computer";
                    this.IsDeviceEnrolledIconVisible = false;
                    this.IsDeviceNotEnrolledIconVisible = true;
                    this.IsDetailsLinkVisible = true;
                    this.DetailsLinkCaption = "More information";
                    break;

                case DeviceEnrollmentState.NotEnrolled:
                    this.EnrollmentStateDescription =
                        $"This computer is currently not enrolled in {ProductName}";
                    this.IsDeviceEnrolledIconVisible = false;
                    this.IsDeviceNotEnrolledIconVisible = true;
                    this.IsDetailsLinkVisible = true;
                    this.DetailsLinkCaption = "More information";
                    break;

                case DeviceEnrollmentState.Enrolled:
                    this.EnrollmentStateDescription =
                        $"Computer is enrolled in {ProductName} and uses " +
                        "device certificate authentication";
                    this.IsDeviceEnrolledIconVisible = true;
                    this.IsDeviceNotEnrolledIconVisible = false;
                    this.IsDetailsLinkVisible = true;
                    this.DetailsLinkCaption = "View device certificate";
                    break;
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        private void OpenEndpointVerificationHelp()
        {
            using (Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                Verb = "open",
                FileName = "https://cloud.google.com/endpoint-verification/docs/overview"
            }))
            { };

        }

        private void OpenDeviceCertificate()
        {
            X509Certificate2UI.DisplayCertificate(
                this.enrollment.Certificate,
                this.View.Handle);
        }

        public void OpenDetails()
        {
            switch (this.enrollment.State)
            {
                case DeviceEnrollmentState.Enrolled:
                    OpenDeviceCertificate();
                    break;

                default:
                    OpenEndpointVerificationHelp();
                    break;
            }
        }
    }
}
