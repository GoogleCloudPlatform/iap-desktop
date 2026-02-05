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
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Iap.Net;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Iap.Test.Protocol
{
    public abstract class TestHttpOverIapTunnelBase : IapFixtureBase
    {
        protected const string InstallApache = "sudo apt-get install -y apache2";

        private const int RepeatCount = 5;
        private readonly CancellationTokenSource tokenSource 
            = new CancellationTokenSource();

        protected abstract INetworkStream ConnectToWebServer(
            InstanceLocator vmRef,
            IAuthorization authorization);

        private class HttpResponseAccumulator
        {
            public int ExpectedBytes { get; private set; } = int.MaxValue;
            public int TotalBytesRead { get; private set; } = 0;

            private readonly StringBuilder response = new StringBuilder();

            public void Accumulate(byte[] buffer, int offset, int count)
            {
                this.response.Append(
                    new ASCIIEncoding().GetString(buffer, offset, count));
                this.TotalBytesRead += count;

                if (this.response.ToString().IndexOf("\r\n\r\n") > 0)
                {
                    // Full HTTP header read.

                    var contentLengthMatch = new Regex("Content-Length: (\\d+)")
                        .Match(this.response.ToString());

                    if (this.ExpectedBytes == int.MaxValue && 
                        contentLengthMatch.Success)
                    {
                        this.ExpectedBytes = int.Parse(
                            contentLengthMatch.Groups[1].Value);

                        // Subtract header from bytes read.
                        var headerLength = this.response
                            .ToString()
                            .IndexOf("\r\n\r\n");

                        this.TotalBytesRead -= headerLength + 4;
                    }
                }
            }

            public bool IsComplete => this.ExpectedBytes == this.TotalBytesRead;
        }

        [Test, Repeat(RepeatCount)]
        public async Task Read_WhenServerClosesConnectionAfterSingleHttpRequest(
            [LinuxInstance(InitializeScript = InstallApache)] 
            ResourceTask<InstanceLocator> vm,

            [Credential(Role = PredefinedRole.IapTunnelUser)] 
            ResourceTask<IAuthorization> auth)
        {
            var stream = ConnectToWebServer(
                await vm,
                await auth);

            var request = new ASCIIEncoding().GetBytes(
                "GET / HTTP/1.0\r\n\r\n");
            await stream
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            var buffer = new byte[SshRelayStream.MinReadSize];

            var response = new HttpResponseAccumulator();
            int bytesRead;
            while ((bytesRead = await stream
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false)) > 0)
            {
                response.Accumulate(buffer, 0, bytesRead);
            }

            await stream
                .CloseAsync(this.tokenSource.Token)
                .ConfigureAwait(false);

            Assert.That(response.TotalBytesRead, Is.EqualTo(response.ExpectedBytes));
        }

        [Test, Repeat(RepeatCount)]
        public async Task Read_WhenServerClosesConnectionMultipleHttpRequests(
            [LinuxInstance(InitializeScript = InstallApache)]
            ResourceTask<InstanceLocator> vm,

            [Credential(Role = PredefinedRole.IapTunnelUser)]
            ResourceTask<IAuthorization> auth)
        {
            var locator = await vm;
            var stream = ConnectToWebServer(
                locator,
                await auth);

            for (var i = 0; i < 3; i++)
            {
                var request = new ASCIIEncoding().GetBytes(
                    $"GET /?_={i} HTTP/1.1\r\nHost:www\r\nConnection: keep-alive\r\n\r\n");
                await stream
                    .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                    .ConfigureAwait(false);

                var buffer = new byte[SshRelayStream.MinReadSize];

                var response = new HttpResponseAccumulator();
                int bytesRead;
                while ((bytesRead = await stream
                    .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                    .ConfigureAwait(false)) > 0)
                {
                    response.Accumulate(buffer, 0, bytesRead);

                    if (response.IsComplete)
                    {
                        IapTraceSource.Log.TraceVerbose("Got full response");
                        break;
                    }
                }

                Assert.That(response.TotalBytesRead, Is.EqualTo(response.ExpectedBytes));
            }

            await stream
                .CloseAsync(this.tokenSource.Token)
                .ConfigureAwait(false);
        }

        [Test, Repeat(RepeatCount)]
        public async Task Read_WhenClientClosesConnectionAfterSingleHttpRequest(
            [LinuxInstance(InitializeScript = InstallApache)] 
            ResourceTask<InstanceLocator> vm,

            [Credential(Role = PredefinedRole.IapTunnelUser)]
            ResourceTask<IAuthorization> auth)
        {
            var locator = await vm;
            var stream = ConnectToWebServer(
                locator,
                await auth);

            var request = new ASCIIEncoding().GetBytes(
                    $"GET / HTTP/1.1\r\nHost:www\r\nConnection: keep-alive\r\n\r\n");
            await stream
                .WriteAsync(request, 0, request.Length, this.tokenSource.Token)
                .ConfigureAwait(false);

            var buffer = new byte[SshRelayStream.MinReadSize];

            // Read a bit.
            var response = new HttpResponseAccumulator();
            var bytesRead = await stream
                .ReadAsync(buffer, 0, buffer.Length, this.tokenSource.Token)
                .ConfigureAwait(false);
            response.Accumulate(buffer, 0, bytesRead);

            await stream
                .CloseAsync(this.tokenSource.Token)
                .ConfigureAwait(false);
        }
    }
}
