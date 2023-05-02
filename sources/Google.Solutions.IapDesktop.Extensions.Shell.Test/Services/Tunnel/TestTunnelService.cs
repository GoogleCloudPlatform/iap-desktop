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
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Google.Solutions.Testing.Common;
using Google.Solutions.Testing.Common.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Tunnel
{
    [TestFixture]
    [UsesCloudResources]
    public class TestTunnelService : ShellFixtureBase
    {
        private IAuthorization CreateAuthorizationMock(
            ICredential credential,
            IDeviceEnrollment enrollment)
        {
            var authz = new Mock<IAuthorization>();
            authz.SetupGet(a => a.Credential).Returns(credential);
            authz.SetupGet(a => a.DeviceEnrollment).Returns(enrollment);

            return authz.Object;
        }

        [Test]
        public async Task WhenInstanceAvailableAndUserInRole_ThenCreateTunnelAndProbeSucceeds(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var service = new TunnelService(CreateAuthorizationMock(
                await credential,
                null));
            var destination = new TunnelDestination(
                await testInstance,
                3389);

            using (var tunnel = await service
                .CreateTunnelAsync(
                    destination,
                    new SameProcessRelayPolicy())
                .ConfigureAwait(false))
            {

                Assert.AreEqual(destination, tunnel.Destination);
                Assert.IsFalse(tunnel.IsMutualTlsEnabled);

                await tunnel
                    .Probe(TimeSpan.FromSeconds(20))
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public async Task WhenInstanceNotAvailable_ThenProbeFails(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var service = new TunnelService(CreateAuthorizationMock(
                await credential,
                null));
            var destination = new TunnelDestination(
                new InstanceLocator(
                    TestProject.ProjectId,
                    "us-central1-a",
                    "nonexistinginstance"),
                3389);

            using (var tunnel = await service
                .CreateTunnelAsync(
                    destination,
                    new SameProcessRelayPolicy())
                .ConfigureAwait(false))
            {
                Assert.AreEqual(destination, tunnel.Destination);

                ExceptionAssert.ThrowsAggregateException<SshRelayBackendNotFoundException>(
                    () => tunnel.Probe(TimeSpan.FromSeconds(20)).Wait());
            }
        }

        [Test]
        public async Task WhenInstanceAvailableButUserNotInRole_ThenProbeFails(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<ICredential> credential)
        {
            var service = new TunnelService(CreateAuthorizationMock(
                await credential,
                null));
            var destination = new TunnelDestination(
                await testInstance,
                3389);

            using (var tunnel = await service
                .CreateTunnelAsync(
                    destination,
                    new SameProcessRelayPolicy())
                .ConfigureAwait(false))
            {
                Assert.AreEqual(destination, tunnel.Destination);

                ExceptionAssert.ThrowsAggregateException<SshRelayDeniedException>(
                    () => tunnel.Probe(TimeSpan.FromSeconds(20)).Wait());
            }
        }

        [Test]
        public async Task WhenInstanceAvailableButRelayPolicyFails_ThenProbeThrowsUnauthorizedException(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var service = new TunnelService(CreateAuthorizationMock(
                await credential,
                null));
            var destination = new TunnelDestination(
                await testInstance,
                3389);

            using (var tunnel = await service
                .CreateTunnelAsync(
                    destination,
                    new DenyAllPolicy())
                .ConfigureAwait(false))
            {
                // The Probe should still succeed.
                await tunnel
                    .Probe(TimeSpan.FromSeconds(20))
                    .ConfigureAwait(false);

                // Trying to send ot receive anything should cause a connection reset.
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(new IPEndPoint(IPAddress.Loopback, tunnel.LocalPort));
                socket.ReceiveTimeout = 100;
                Assert.AreEqual(0, socket.Receive(new byte[1]));
            }
        }

        private class DenyAllPolicy : ISshRelayPolicy
        {
            public bool IsClientAllowed(IPEndPoint remote) => false;
        }
    }
}
