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
using Google.Solutions.Common;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using NUnit.Framework;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("IAP")]
    public class TestHttpOverIapDirectTunnel : TestHttpOverIapTunnelBase
    {
        protected override INetworkStream ConnectToWebServer(
            InstanceLocator vmRef,
            ICredential credential)
        {
            return new SshRelayStream(
                new IapTunnelingEndpoint(
                    credential,
                    vmRef,
                    80,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    TestProject.UserAgent));
        }

        [Test]
        public async Task WhenBufferIsTiny_ThenReadingFailsWithIndexOutOfRangeException(
            [LinuxInstance(InitializeScript = InstallApache)] InstanceRequest vm,
            [Credential] ICredential credential)
        {
            await vm.AwaitReady();
            var stream = ConnectToWebServer(vm.Locator, credential);

            byte[] request = new ASCIIEncoding().GetBytes(
                "GET / HTTP/1.0\r\n\r\n");
            await stream.WriteAsync(request, 0, request.Length, CancellationToken.None);

            byte[] buffer = new byte[64];

            AssertEx.ThrowsAggregateException<IndexOutOfRangeException>(() =>
            {
                stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).Wait();
            });
        }

        [Test]
        public async Task WhenConnectingWithInvalidAccessToken_ThenReadingFailsWithUnauthorizedException
            ([LinuxInstance(InitializeScript = InstallApache)] InstanceRequest vm)
        {
            await vm.AwaitReady();

            // NB. Fiddler might cause this test to fail.

            byte[] request = new ASCIIEncoding().GetBytes(
                "GET / HTTP/1.0\r\n\r\n");

            var stream = new SshRelayStream(
                new IapTunnelingEndpoint(
                    GoogleCredential.FromAccessToken("invalid"),
                    vm.Locator,
                    80,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    TestProject.UserAgent));

            await stream.WriteAsync(request, 0, request.Length, CancellationToken.None);

            AssertEx.ThrowsAggregateException<UnauthorizedException>(() =>
            {


                byte[] buffer = new byte[64 * 1024];
                stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).Wait();
            });
        }

        [Test]
        public async Task WhenFirstReadCompleted_ThenSidIsAvailable(
            [LinuxInstance(InitializeScript = InstallApache)] InstanceRequest vm,
            [Credential] ICredential credential)
        {
            await vm.AwaitReady();
            var stream = (SshRelayStream)ConnectToWebServer(vm.Locator, credential);

            byte[] request = new ASCIIEncoding().GetBytes(
                    $"GET / HTTP/1.1\r\nHost:www\r\nConnection: keep-alive\r\n\r\n");

            Assert.IsNull(stream.Sid);
            await stream.WriteAsync(request, 0, request.Length, CancellationToken.None);

            Assert.IsNull(stream.Sid);

            // Read a bit.
            byte[] buffer = new byte[stream.MinReadSize];
            await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

            Assert.IsNotNull(stream.Sid);
        }


        [Test]
        [Ignore("Can also throw an UnauthorizedException")]
        public async Task WhenServerNotListening_ThenReadFails(
            [LinuxInstance] InstanceRequest vm,
            [Credential] ICredential credential)
        {
            await vm.AwaitReady();
            var stream = ConnectToWebServer(vm.Locator, credential);

            byte[] request = new ASCIIEncoding().GetBytes(
                    $"GET / HTTP/1.1\r\nHost:www\r\nConnection: keep-alive\r\n\r\n");
            await stream.WriteAsync(request, 0, request.Length, CancellationToken.None);

            AssertEx.ThrowsAggregateException<WebSocketStreamClosedByServerException>(() =>
            {
                byte[] buffer = new byte[stream.MinReadSize];
                stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).Wait();
            });
        }
    }
}
