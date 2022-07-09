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
using Google.Solutions.Common.Locator;
using Google.Solutions.Testing.Common.Integration;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using NUnit.Framework;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace Google.Solutions.IapTunneling.Test.Iap
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("IAP")]
    public class TestSshRelayListener : IapFixtureBase
    {
        private static void FillArray(byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (byte)('A' + (i % 26));
            }
        }

        [Test]
        public async Task WhenSendingMessagesToEchoServer_ThenStatisticsAreUpdated(
            [LinuxInstance(InitializeScript = InitializeScripts.InstallEchoServer)] ResourceTask<InstanceLocator> vm,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential,
            [Values(
                1,
                (int)DataMessage.MaxDataLength,
                (int)DataMessage.MaxDataLength * 2)] int length)
        {

            var message = new byte[length];
            FillArray(message);

            var locator = await vm;

            var listener = SshRelayListener.CreateLocalListener(
                new IapTunnelingEndpoint(
                    await credential,
                    await vm,
                    7,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    TestProject.UserAgent),
                new AllowAllRelayPolicy());
            listener.ClientAcceptLimit = 1; // Terminate after first connection.
            listener.ListenAsync(CancellationToken.None).ContinueWith(_ => { });

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, listener.LocalPort));

            var clientStreamStats = new ConnectionStatistics();
            var clientStream = new SocketStream(socket, clientStreamStats);

            using (var tokenSource = new CancellationTokenSource())
            {
                // Write full payload.
                await clientStream
                    .WriteAsync(message, 0, message.Length, tokenSource.Token)
                    .ConfigureAwait(false);
                Assert.AreEqual(length, clientStreamStats.BytesTransmitted);

                // Read entire response.
                var response = new byte[length];
                int totalBytesRead = 0;
                while (true)
                {
                    var bytesRead = await clientStream
                        .ReadAsync(
                            response,
                            totalBytesRead,
                            response.Length - totalBytesRead,
                            tokenSource.Token)
                        .ConfigureAwait(false);
                    totalBytesRead += bytesRead;

                    if (bytesRead == 0 || totalBytesRead >= length)
                    {
                        break;
                    }
                }

                await clientStream
                    .CloseAsync(tokenSource.Token)
                    .ConfigureAwait(false);

                await Task.Delay(50).ConfigureAwait(false);

                Assert.AreEqual(length, totalBytesRead, "bytes read");
                Assert.AreEqual(length, clientStreamStats.BytesReceived, "client received");
                Assert.AreEqual(length, listener.Statistics.BytesReceived, "server received");
                Assert.AreEqual(length, listener.Statistics.BytesTransmitted, "server sent");
            }
        }
    }
}
