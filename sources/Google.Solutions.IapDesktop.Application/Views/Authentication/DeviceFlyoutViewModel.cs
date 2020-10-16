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
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Authentication
{
    public class DeviceFlyoutViewModel : ViewModelBase
    {
        private const string ProductName = "Secure Connect Endpoint Verification";
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
                case DeviceEnrollmentState.NotInstalled:
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

                case DeviceEnrollmentState.EnrolledWithoutCertificate:
                    this.EnrollmentStateDescription = 
                        $"This computer is enrolled in {ProductName}, but lacks a device certificate";
                    this.IsDeviceEnrolledIconVisible = false;
                    this.IsDeviceNotEnrolledIconVisible = true;
                    this.IsDetailsLinkVisible = false;
                    this.DetailsLinkCaption = string.Empty;
                    break;

                case DeviceEnrollmentState.Enrolled:
                    this.EnrollmentStateDescription = 
                        $"This computer is enrolled in {ProductName}";
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
