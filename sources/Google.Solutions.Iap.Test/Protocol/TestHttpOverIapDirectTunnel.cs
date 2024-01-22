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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Test.Protocol
{
    [TestFixture]
    [UsesCloudResources]
    public class TestHttpOverIapDirectTunnel : TestHttpOverIapTunnelBase
    {
        protected override INetworkStream ConnectToWebServer(
            InstanceLocator vmRef,
            IAuthorization authorization)
        {
            var client = new IapClient(
                IapClient.CreateEndpoint(),
                authorization,
                TestProject.UserAgent);

            return new SshRelayStream(
                client.GetTarget(vmRef, 80, IapClient.DefaultNetworkInterface));
        }

        [Test]
        public async Task WhenBufferIsTiny_ThenReadingFailsWithIndexOutOfRangeException(
            [LinuxInstance(InitializeScript = InstallApache)] ResourceTask<InstanceLocator> vm,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var locator = await vm;
            var stream = ConnectToWebServer(locator, await auth);

            var request = new ASCIIEncoding().GetBytes(
                "GET / HTTP/1.0\r\n\r\n");
            await stream
                .WriteAsync(request, 0, request.Length, CancellationToken.None)
                .ConfigureAwait(false);

            var buffer = new byte[64];

            ExceptionAssert.ThrowsAggregateException<IndexOutOfRangeException>(() =>
            {
                stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).Wait();
            });
        }

        [Test]
        public async Task WhenConnectingWithInvalidAccessToken_ThenReadingFailsWithUnauthorizedException
            ([LinuxInstance(InitializeScript = InstallApache)] ResourceTask<InstanceLocator> vm)
        {
            // NB. Fiddler might cause this test to fail.

            var request = new ASCIIEncoding().GetBytes(
                "GET / HTTP/1.0\r\n\r\n");

            var client = new IapClient(
                IapClient.CreateEndpoint(),
                TestProject.InvalidAuthorization,
                TestProject.UserAgent);

            var stream = new SshRelayStream(
                client.GetTarget(
                    await vm,
                    80,
                    IapClient.DefaultNetworkInterface));

            ExceptionAssert.ThrowsAggregateException<SshRelayDeniedException>(() =>
            {
                stream.WriteAsync(request, 0, request.Length, CancellationToken.None).Wait();
            });
        }

        [Test]
        public async Task WhenFirstWriteCompleted_ThenSidIsAvailable(
            [LinuxInstance(InitializeScript = InstallApache)] ResourceTask<InstanceLocator> vm,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var locator = await vm;
            var stream = (SshRelayStream)ConnectToWebServer(
                locator,
                await auth);

            var request = new ASCIIEncoding().GetBytes(
                    $"GET / HTTP/1.1\r\nHost:www\r\nConnection: keep-alive\r\n\r\n");

            Assert.IsNull(stream.Sid);
            await stream
                .WriteAsync(request, 0, request.Length, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(stream.Sid);
        }

        [Test]
        public async Task WhenServerNotListening_ThenWriteFails(
            [LinuxInstance] ResourceTask<InstanceLocator> vm,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
        {
            var stream = ConnectToWebServer(
                await vm,
                await auth);

            var request = new ASCIIEncoding().GetBytes(
                    $"GET / HTTP/1.1\r\nHost:www\r\nConnection: keep-alive\r\n\r\n");

            ExceptionAssert.ThrowsAggregateException<SshRelayConnectException>(() =>
            {
                var buffer = new byte[SshRelayStream.MinReadSize];
                stream.WriteAsync(request, 0, request.Length, CancellationToken.None).Wait();
            });
        }
    }
}
