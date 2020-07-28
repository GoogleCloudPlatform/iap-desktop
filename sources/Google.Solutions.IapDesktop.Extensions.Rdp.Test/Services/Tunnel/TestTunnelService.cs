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
using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Tunnel;
using Google.Solutions.IapTunneling.Iap;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Services.Tunnel
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestTunnelService
    {
        private IAuthorizationAdapter CreateAuthorizationAdapter(ICredential credential)
        {
            var authz = new Mock<IAuthorization>();
            authz.SetupGet(a => a.Credential).Returns(credential);

            var adapter = new Mock<IAuthorizationAdapter>();
            adapter.SetupGet(a => a.Authorization).Returns(authz.Object);

            return adapter.Object;
        }

        [Test]
        public async Task WhenInstanceAvailableAndUserInRole_ThenCreateTunnelAndProbeSucceeds(
            [WindowsInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] CredentialRequest credential)
        {
            await testInstance.AwaitReady();

            var service = new TunnelService(CreateAuthorizationAdapter(
                await credential.GetCredentialAsync()));
            var destination = new TunnelDestination(
                testInstance.Locator,
                3389);

            var tunnel = await service.CreateTunnelAsync(destination);

            Assert.AreEqual(destination, tunnel.Destination);
            await tunnel.Probe(TimeSpan.FromSeconds(20));
            tunnel.Close();
        }

        [Test]
        public async Task WhenInstanceNotAvailable_ThenProbeFails(
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            var service = new TunnelService(CreateAuthorizationAdapter(
                await credential.GetCredentialAsync()));
            var destination = new TunnelDestination(
                new InstanceLocator(
                    TestProject.ProjectId,
                    "us-central1-a",
                    "nonexistinginstance"),
                3389);

            var tunnel = await service.CreateTunnelAsync(destination);

            Assert.AreEqual(destination, tunnel.Destination);

            AssertEx.ThrowsAggregateException<UnauthorizedException>(
                () => tunnel.Probe(TimeSpan.FromSeconds(20)).Wait());

            tunnel.Close();
        }

        [Test]
        public async Task WhenInstanceAvailableButUserNotInRole_ThenProbeFails(
            [WindowsInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            await testInstance.AwaitReady();

            var service = new TunnelService(CreateAuthorizationAdapter(
                await credential.GetCredentialAsync()));
            var destination = new TunnelDestination(
                testInstance.Locator,
                3389);

            var tunnel = await service.CreateTunnelAsync(destination);

            Assert.AreEqual(destination, tunnel.Destination);

            AssertEx.ThrowsAggregateException<UnauthorizedException>(
                () => tunnel.Probe(TimeSpan.FromSeconds(20)).Wait());

            tunnel.Close();
        }
    }
}
