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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Test.Protocol
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSshRelayStreamProbing : IapFixtureBase
    {
        [Test]
        public async Task WhenProjectDoesntExist_ThenProbeThrowsException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new IapClient(
                IapClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            using (var stream = new SshRelayStream(
                client.GetTarget(
                    new InstanceLocator("invalid", TestProject.Zone, "invalid"),
                    80,
                    IapClient.DefaultNetworkInterface)))
            {
                ExceptionAssert.ThrowsAggregateException<SshRelayDeniedException>(() =>
                    stream.ProbeConnectionAsync(TimeSpan.FromSeconds(10)).Wait());
            }
        }

        [Test]
        public async Task WhenZoneDoesntExist_ThenProbeThrowsException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new IapClient(
                IapClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            using (var stream = new SshRelayStream(
               client.GetTarget(
                    new InstanceLocator(
                        TestProject.ProjectId,
                        "invalid",
                        "invalid"),
                    80,
                    IapClient.DefaultNetworkInterface)))
            {
                ExceptionAssert.ThrowsAggregateException<SshRelayBackendNotFoundException>(() =>
                    stream.ProbeConnectionAsync(TimeSpan.FromSeconds(10)).Wait());
            }
        }

        [Test]
        public async Task WhenInstanceDoesntExist_ThenProbeThrowsException(
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new IapClient(
                IapClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            using (var stream = new SshRelayStream(
                client.GetTarget(
                    new InstanceLocator(
                        TestProject.ProjectId,
                        TestProject.Zone,
                        "invalid"),
                    80,
                    IapClient.DefaultNetworkInterface)))
            {
                ExceptionAssert.ThrowsAggregateException<SshRelayBackendNotFoundException>(() =>
                    stream.ProbeConnectionAsync(TimeSpan.FromSeconds(10)).Wait());
            }
        }

        [Test]
        public async Task WhenInstanceExistsAndIsListening_ThenProbeSucceeds(
             [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
             [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new IapClient(
                IapClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            using (var stream = new SshRelayStream(
                client.GetTarget(
                    await testInstance,
                    3389,
                    IapClient.DefaultNetworkInterface)))
            {
                await stream
                    .ProbeConnectionAsync(TimeSpan.FromSeconds(10))
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public async Task WhenInstanceExistsButNotListening_ThenProbeThrowsException(
             [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
             [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var client = new IapClient(
                IapClient.CreateEndpoint(),
                await auth,
                TestProject.UserAgent);

            using (var stream = new SshRelayStream(
               client.GetTarget(
                    await testInstance,
                    22,
                    IapClient.DefaultNetworkInterface)))
            {
                ExceptionAssert.ThrowsAggregateException<NetworkStreamClosedException>(() =>
                    stream.ProbeConnectionAsync(TimeSpan.FromSeconds(5)).Wait());
            }
        }
    }
}
