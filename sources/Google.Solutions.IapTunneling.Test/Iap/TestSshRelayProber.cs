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
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("IAP")]
    public class TestSshRelayProber : IapFixtureBase
    {
        [Test]
        public async Task WhenProjectDoesntExist_ThenProbeFailsWithUnauthorizedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            using (var stream = new SshRelayStream(
                new IapTunnelingEndpoint(
                    await credential,
                    new InstanceLocator(
                        "invalid",
                        TestProject.Zone,
                        "invalid"),
                    80,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    TestProject.UserAgent)))
            {
                ExceptionAssert.ThrowsAggregateException<UnauthorizedException>(() =>
                    stream.TestConnectionAsync(TimeSpan.FromSeconds(10)).Wait());
            }
        }

        [Test]
        public async Task WhenZoneDoesntExist_ThenProbeFailsWithUnauthorizedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            using (var stream = new SshRelayStream(
               new IapTunnelingEndpoint(
                    await credential,
                    new InstanceLocator(
                        TestProject.ProjectId,
                        "invalid",
                        "invalid"),
                    80,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    TestProject.UserAgent)))
            {
                ExceptionAssert.ThrowsAggregateException<UnauthorizedException>(() =>
                    stream.TestConnectionAsync(TimeSpan.FromSeconds(10)).Wait());
            }
        }

        [Test]
        public async Task WhenInstanceDoesntExist_ThenProbeFailsWithUnauthorizedException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            using (var stream = new SshRelayStream(
                new IapTunnelingEndpoint(
                    await credential,
                    new InstanceLocator(
                        TestProject.ProjectId,
                        TestProject.Zone,
                        "invalid"),
                    80,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    TestProject.UserAgent)))
            {
                ExceptionAssert.ThrowsAggregateException<UnauthorizedException>(() =>
                    stream.TestConnectionAsync(TimeSpan.FromSeconds(10)).Wait());
            }
        }

        [Test]
        public async Task WhenInstanceExistsAndIsListening_ThenProbeSucceeds(
             [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
             [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            using (var stream = new SshRelayStream(
                new IapTunnelingEndpoint(
                    await credential,
                    await testInstance,
                    3389,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    TestProject.UserAgent)))
            {
                await stream
                    .TestConnectionAsync(TimeSpan.FromSeconds(10))
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public async Task WhenInstanceExistsButNotListening_ThenProbeFailsWithNetworkStreamClosedException(
             [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
             [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            using (var stream = new SshRelayStream(
                new IapTunnelingEndpoint(
                    await credential,
                    await testInstance,
                    22,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    TestProject.UserAgent)))
            {
                ExceptionAssert.ThrowsAggregateException<NetworkStreamClosedException>(() =>
                    stream.TestConnectionAsync(TimeSpan.FromSeconds(5)).Wait());
            }
        }
    }
}
