//
// Copyright 2023 Google LLC
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
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.Platform.Net;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Client
{
    [TestFixture]
    public class TestCloudConsoleClient
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private static TSession CreateSession<TSession>() where TSession : class, IOidcSession
        {
            var session = new Mock<TSession>();
            session.Setup(s => s.CreateDomainSpecificServiceUri(It.IsAny<Uri>()))
                .Returns((Uri u) => u);
            return session.Object;
        }

        private static IAuthorization CreateGaiaAuthorization(DeviceEnrollmentState state)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(state);

            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.DeviceEnrollment).Returns(enrollment.Object);
            authorization.SetupGet(a => a.Session).Returns(CreateSession<IGaiaOidcSession>());

            return authorization.Object;
        }

        private static IAuthorization CreateWorkfoceIdentityAuthorization()
        {
            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.Session).Returns(CreateSession<IOidcSession>());
            return authorization.Object;
        }

        //---------------------------------------------------------------------
        // OpenInstanceDetails.
        //---------------------------------------------------------------------

        [Test]
        public void OpenInstanceDetails_WhenAuthenticatedWithGaiaAndEnrolled_ThenOpenInstanceDetailsUsesSecureConsole()
        {
            var browser = new Mock<IBrowser>();
            var cloudConsole = new CloudConsoleClient(
                CreateGaiaAuthorization(DeviceEnrollmentState.Enrolled),
                browser.Object);

            cloudConsole.OpenInstanceDetails(SampleLocator);

            browser.Verify(b => b.Navigate(new Uri(
                "https://console-secure.cloud.google.com" +
                "/compute/instancesDetail/zones/zone-1/instances/instance-1?project=project-1")));
        }

        [Test]
        public void OpenInstanceDetails_WhenAuthenticatedWithGaiaAndNotEnrolled_ThenOpenInstanceDetailsUsesSecureConsole()
        {
            var browser = new Mock<IBrowser>();
            var cloudConsole = new CloudConsoleClient(
                CreateGaiaAuthorization(DeviceEnrollmentState.Disabled),
                browser.Object);

            cloudConsole.OpenInstanceDetails(SampleLocator);

            browser.Verify(b => b.Navigate(new Uri(
                "https://console.cloud.google.com" +
                "/compute/instancesDetail/zones/zone-1/instances/instance-1?project=project-1")));
        }

        [Test]
        public void OpenInstanceDetails_WhenAuthenticatedWithWorkforceIdentity_ThenOpenInstanceDetailsUsesByoidConsole()
        {
            var browser = new Mock<IBrowser>();
            var cloudConsole = new CloudConsoleClient(
                CreateWorkfoceIdentityAuthorization(),
                browser.Object);

            cloudConsole.OpenInstanceDetails(SampleLocator);

            browser.Verify(b => b.Navigate(new Uri(
                "https://console.cloud.google" +
                "/compute/instancesDetail/zones/zone-1/instances/instance-1?project=project-1")));
        }

        //---------------------------------------------------------------------
        // OpenIapSecurity.
        //---------------------------------------------------------------------

        [Test]
        public void OpenIapSecurity_WhenAuthenticatedWithGaiaAndEnrolled_ThenOpenIapSecurityUsesSecureConsole()
        {
            var browser = new Mock<IBrowser>();
            var cloudConsole = new CloudConsoleClient(
                CreateGaiaAuthorization(DeviceEnrollmentState.Enrolled),
                browser.Object);

            cloudConsole.OpenIapSecurity("project-1");

            browser.Verify(b => b.Navigate(new Uri(
                "https://console-secure.cloud.google.com" +
                "/security/iap?tab=ssh-tcp-resources&project=project-1")));
        }

        [Test]
        public void OpenIapSecurity_WhenAuthenticatedWithGaiaAndNotEnrolled_ThenOpenIapSecurityUsesSecureConsole()
        {
            var browser = new Mock<IBrowser>();
            var cloudConsole = new CloudConsoleClient(
                CreateGaiaAuthorization(DeviceEnrollmentState.Disabled),
                browser.Object);

            cloudConsole.OpenIapSecurity("project-1");

            browser.Verify(b => b.Navigate(new Uri(
                "https://console.cloud.google.com" +
                "/security/iap?tab=ssh-tcp-resources&project=project-1")));
        }

        [Test]
        public void OpenIapSecurity_WhenAuthenticatedWithWorkforceIdentity_ThenOpenIapSecurityUsesByoidConsole()
        {
            var browser = new Mock<IBrowser>();
            var cloudConsole = new CloudConsoleClient(
                CreateWorkfoceIdentityAuthorization(),
                browser.Object);

            cloudConsole.OpenIapSecurity("project-1");

            browser.Verify(b => b.Navigate(new Uri(
                "https://console.cloud.google" +
                "/security/iap?tab=ssh-tcp-resources&project=project-1")));

        }
    }
}
