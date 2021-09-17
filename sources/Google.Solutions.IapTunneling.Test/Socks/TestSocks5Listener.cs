//
// Copyright 2021 Google LLC
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

using Google.Solutions.Common.Util;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using Google.Solutions.IapTunneling.Socks5;
using Google.Solutions.IapTunneling.Test.Net;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Socks
{
    [TestFixture]
    public class TestSocks5Listener : IapFixtureBase
    {
        private static Socks5Stream ConnectToListener(Socks5Listener listener)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, listener.ListenPort));

            return new Socks5Stream(
                new SocketStream(socket, new ConnectionStatistics()));
        }

        private IDisposable RunListener(Socks5Listener listener)
        {
            var cts = new CancellationTokenSource();
            var listenTask = listener.ListenAsync(cts.Token);

            return Disposable.For(() =>
            {
                cts.Cancel();
                listenTask.Wait();
            });
        }

        [Test]
        public async Task WhenProtocolVersionInvalid_ThenServerSendsNoAcceptableMethods()
        {
            var relay = new Mock<ISocks5Relay>();
            var listener = new Socks5Listener(
                relay.Object,
                PortFinder.FindFreeLocalPort());

            using (RunListener(listener))
            using (var clientStream = ConnectToListener(listener))
            {
                await clientStream.WriteNegotiateMethodRequestAsync(
                        new NegotiateMethodRequest(
                            1,
                            new [] { AuthenticationMethod.NoAuthenticationRequired }),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var response = await clientStream.ReadNegotiateMethodResponseAsync(
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(AuthenticationMethod.NoAcceptableMethods, response.Method);
            }
        }

        [Test]
        public async Task WhenAuthenticationMethodsUnsupported_ThenServerSendsNoAcceptableMethods()
        {
            var relay = new Mock<ISocks5Relay>();
            var listener = new Socks5Listener(
                relay.Object,
                PortFinder.FindFreeLocalPort());

            using (RunListener(listener))
            using (var clientStream = ConnectToListener(listener))
            {
                await clientStream.WriteNegotiateMethodRequestAsync(
                        new NegotiateMethodRequest(
                            Socks5Stream.ProtocolVersion,
                            new [] { AuthenticationMethod.GssApi }),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var response = await clientStream.ReadNegotiateMethodResponseAsync(
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(AuthenticationMethod.NoAcceptableMethods, response.Method);
            }
        }

        [Test]
        public async Task WhenConnectionCommandUnsupported_ThenServerSendsCommandNotSupported()
        {
            var relay = new Mock<ISocks5Relay>();
            var listener = new Socks5Listener(
                relay.Object,
                PortFinder.FindFreeLocalPort());

            using (RunListener(listener))
            using (var clientStream = ConnectToListener(listener))
            {
                await clientStream.WriteNegotiateMethodRequestAsync(
                        new NegotiateMethodRequest(
                            Socks5Stream.ProtocolVersion,
                            new [] { AuthenticationMethod.NoAuthenticationRequired }),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                await clientStream.ReadNegotiateMethodResponseAsync(
                        CancellationToken.None)
                    .ConfigureAwait(false);

                await clientStream.WriteConnectionRequestAsync(
                        new ConnectionRequest(
                            Socks5Stream.ProtocolVersion,
                            Command.Bind,
                            AddressType.IPv4,
                            new byte[] { 1, 2, 3, 4 },
                            8080),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var response = await clientStream.ReadConnectionResponseAsync(
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(ConnectionReply.CommandNotSupported, response.Reply);
            }
        }

        [Test]
        public async Task WhenAddressIsIpv4_ThenServerSendsAddressTypeNotSupported()
        {
            var relay = new Mock<ISocks5Relay>();
            relay.Setup(r => r.CreateRelayPortAsync(
                    It.IsAny<IPEndPoint>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("mock"));

            var listener = new Socks5Listener(
                relay.Object,
                PortFinder.FindFreeLocalPort());

            using (RunListener(listener))
            using (var clientStream = ConnectToListener(listener))
            {
                await clientStream.WriteNegotiateMethodRequestAsync(
                        new NegotiateMethodRequest(
                            Socks5Stream.ProtocolVersion,
                            new[] { AuthenticationMethod.NoAuthenticationRequired }),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                await clientStream.ReadNegotiateMethodResponseAsync(
                        CancellationToken.None)
                    .ConfigureAwait(false);

                await clientStream.WriteConnectionRequestAsync(
                        new ConnectionRequest(
                            Socks5Stream.ProtocolVersion,
                            Command.Connect,
                            AddressType.IPv4,
                            new byte[] { 1, 2, 3, 4 },
                            8080),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var response = await clientStream.ReadConnectionResponseAsync(
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(ConnectionReply.AddressTypeNotSupported, response.Reply);
            }
        }

        [Test]
        public async Task WhenClientUnauthorized_ThenServerSendsConnectionNotAllowed()
        {
            var relay = new Mock<ISocks5Relay>();
            relay.Setup(r => r.CreateRelayPortAsync(
                    It.IsAny<IPEndPoint>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("mock"));

            var listener = new Socks5Listener(
                relay.Object,
                PortFinder.FindFreeLocalPort());

            using (RunListener(listener))
            using (var clientStream = ConnectToListener(listener))
            {
                await clientStream.WriteNegotiateMethodRequestAsync(
                        new NegotiateMethodRequest(
                            Socks5Stream.ProtocolVersion,
                            new[] { AuthenticationMethod.NoAuthenticationRequired }),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                await clientStream.ReadNegotiateMethodResponseAsync(
                        CancellationToken.None)
                    .ConfigureAwait(false);

                await clientStream.WriteConnectionRequestAsync(
                        new ConnectionRequest(
                            Socks5Stream.ProtocolVersion,
                            Command.Connect,
                            "denied.example.com",
                            8080),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var response = await clientStream.ReadConnectionResponseAsync(
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(ConnectionReply.ConnectionNotAllowed, response.Reply);
            }
        }
    }
}
