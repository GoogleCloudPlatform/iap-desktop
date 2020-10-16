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

namespace Google.Solutions.IapDesktop.Application.Views.Authentication
{
    public class DeviceFlyoutViewModel : ViewModelBase
    {
        private readonly IDeviceEnrollment enrollment;

        public bool IsDeviceEnrolledIconVisible { get; }
        public bool IsDeviceNotEnrolledIconVisible { get; }
        public string EnrollmentStateDescription { get; }
        public string DetailsLinkCaption { get; }
        public bool IsDetailsLinkVisible { get; }

        public DeviceFlyoutViewModel(IDeviceEnrollment enrollment)
        {
            this.enrollment = enrollment;

            switch (enrollment.State)
            {
                case DeviceEnrollmentState.NotInstalled:
                    this.EnrollmentStateDescription = "Endpoint verification not installed";
                    this.IsDeviceEnrolledIconVisible = false;
                    this.IsDeviceNotEnrolledIconVisible = true;
                    this.IsDetailsLinkVisible = true;
                    this.DetailsLinkCaption = "More information";
                    break;

                case DeviceEnrollmentState.NotEnrolled:
                    this.EnrollmentStateDescription = "Not enrolled in endpoint verification";
                    this.IsDeviceEnrolledIconVisible = false;
                    this.IsDeviceNotEnrolledIconVisible = true;
                    this.IsDetailsLinkVisible = false;
                    this.DetailsLinkCaption = string.Empty;
                    break;

                case DeviceEnrollmentState.EnrolledWithoutCertificate:
                    this.EnrollmentStateDescription = "Device certificate missing";
                    this.IsDeviceEnrolledIconVisible = false;
                    this.IsDeviceNotEnrolledIconVisible = true;
                    this.IsDetailsLinkVisible = false;
                    this.DetailsLinkCaption = string.Empty;
                    break;

                case DeviceEnrollmentState.Enrolled:
                    this.EnrollmentStateDescription = "Enrolled in endpoint verification";
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
                FileName = "https://support.google.com/a/answer/9007320#install-helper"
            }))
            { };

        }

        private void OpenDeviceCertificate()
        {
            X509Certificate2UI.DisplayCertificate(this.enrollment.Certificate);
        }

        public void OpenDetails()
        {
            switch (this.enrollment.State)
            {
                case DeviceEnrollmentState.NotInstalled:
                    OpenEndpointVerificationHelp();
                    break;

                case DeviceEnrollmentState.Enrolled:
                    OpenDeviceCertificate();
                    break;
            }
        }
    }
}
