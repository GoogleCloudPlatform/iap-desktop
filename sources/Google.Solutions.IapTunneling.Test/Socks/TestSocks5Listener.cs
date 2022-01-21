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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.Common.Util;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using Google.Solutions.IapTunneling.Socks5;
using Google.Solutions.IapTunneling.Test.Iap;
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

        //---------------------------------------------------------------------
        // Protocol version.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProtocolVersionInvalid_ThenServerSendsNoAcceptableMethods()
        {
            var listener = new Socks5Listener(
                new Mock<ISshRelayEndpointResolver>().Object,
                new Mock<ISshRelayPolicy>().Object,
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

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(AuthenticationMethod.NoAcceptableMethods, response.Method);
            }
        }

        //---------------------------------------------------------------------
        // Authentication method.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenAuthenticationMethodsUnsupported_ThenServerSendsNoAcceptableMethods()
        {
            var listener = new Socks5Listener(
                new Mock<ISshRelayEndpointResolver>().Object,
                new Mock<ISshRelayPolicy>().Object,
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

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(AuthenticationMethod.NoAcceptableMethods, response.Method);
            }
        }

        //---------------------------------------------------------------------
        // Connect with invalid parameters.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnectionCommandUnsupported_ThenServerSendsCommandNotSupported()
        {
            var listener = new Socks5Listener(
                new Mock<ISshRelayEndpointResolver>().Object,
                new Mock<ISshRelayPolicy>().Object,
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

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(ConnectionReply.CommandNotSupported, response.Reply);
            }
        }

        [Test]
        public async Task WhenAddressIsIpv6_ThenServerSendsAddressTypeNotSupported()
        {
            var listener = new Socks5Listener(
                new Mock<ISshRelayEndpointResolver>().Object,
                new Mock<ISshRelayPolicy>().Object,
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
                            AddressType.IPv6,
                            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8 },
                            8080),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var response = await clientStream.ReadConnectionResponseAsync(
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(ConnectionReply.AddressTypeNotSupported, response.Reply);
            }
        }

        [Test]
        public async Task WhenClientNotAllowed_ThenServerSendsConnectionNotAllowed()
        {
            var policy = new Mock<ISshRelayPolicy>();
            policy.Setup(r => r.IsClientAllowed(
                    It.IsAny<IPEndPoint>()))
                .Returns(false);

            var listener = new Socks5Listener(
                new Mock<ISshRelayEndpointResolver>().Object,
                policy.Object,
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

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(ConnectionReply.ConnectionNotAllowed, response.Reply);
            }
        }

        //---------------------------------------------------------------------
        // Connect with invalid destination.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenAddressTypeIsDomainAndDomainNotResolvable_ThenServerSendsNetworkUnreachable()
        {
            var policy = new Mock<ISshRelayPolicy>();
            policy.Setup(r => r.IsClientAllowed(
                    It.IsAny<IPEndPoint>()))
                .Returns(true);

            var resolver = new Mock<ISshRelayEndpointResolver>();
            resolver.Setup(r => r.ResolveEndpointAsync(
                    It.IsAny<string>(),
                    It.IsAny<ushort>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("mock!"));

            var listener = new Socks5Listener(
                resolver.Object,
                policy.Object,
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
                            "unresolvable.example.com",
                            8080),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var response = await clientStream.ReadConnectionResponseAsync(
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(ConnectionReply.NetworkUnreachable, response.Reply);
            }
        }

        [Test]
        public async Task WhenAddressTypeIsIpv4ndAddressNotResolvable_ThenServerSendsNetworkUnreachable()
        {
            var policy = new Mock<ISshRelayPolicy>();
            policy.Setup(r => r.IsClientAllowed(
                    It.IsAny<IPEndPoint>()))
                .Returns(true);

            var resolver = new Mock<ISshRelayEndpointResolver>();
            resolver.Setup(r => r.ResolveEndpointAsync(
                    It.IsAny<IPAddress>(),
                    It.IsAny<ushort>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("mock!"));

            var listener = new Socks5Listener(
                resolver.Object,
                policy.Object,
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

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(ConnectionReply.NetworkUnreachable, response.Reply);
            }
        }

        //---------------------------------------------------------------------
        // Connect with proper destination.
        //---------------------------------------------------------------------

        private const ushort EchoPort = 7;

        [Test]
        public async Task WhenAddressTypeIsDomainAndDomainResolvable_ThenServerSendsAddress(
            [LinuxInstance(InitializeScript = InitializeScripts.InstallEchoServer)] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credentialTask)
        {
            var policy = new Mock<ISshRelayPolicy>();
            policy.Setup(r => r.IsClientAllowed(
                    It.IsAny<IPEndPoint>()))
                .Returns(true);

            var instance = await instanceTask;
            var credential = await credentialTask;
            var resolver = new Mock<ISshRelayEndpointResolver>();
            resolver.Setup(r => r.ResolveEndpointAsync(
                    It.IsAny<string>(),
                    It.Is<ushort>(port => port == EchoPort),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new IapTunnelingEndpoint(
                        credential,
                        instance,
                        EchoPort,
                        IapTunnelingEndpoint.DefaultNetworkInterface,
                        TestProject.UserAgent));

            var listener = new Socks5Listener(
                resolver.Object,
                policy.Object,
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
                            "host.example.com",
                            EchoPort),
                        CancellationToken.None)
                    .ConfigureAwait(false);

                var response = await clientStream.ReadConnectionResponseAsync(
                        CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.IsTrue(await clientStream
                    .ConfirmClosedAsync(CancellationToken.None)
                    .ConfigureAwait(false));

                Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                Assert.AreEqual(ConnectionReply.Succeeded, response.Reply);
                CollectionAssert.AreEqual(new byte[] { 127, 0, 0, 1 }, response.ServerAddress);

                using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
                {
                    socket.Connect(new IPAddress(response.ServerAddress), response.ServerPort);

                    using (var stream = new SocketStream(socket, new ConnectionStatistics()))
                    {
                        var sendData = new byte[] { 1, 2, 3, 4 };
                        await stream
                            .WriteAsync(sendData, 0, sendData.Length, CancellationToken.None)
                            .ConfigureAwait(false);

                        var readData = new byte[4];
                        await stream
                            .ReadAsync(readData, 0, readData.Length, CancellationToken.None)
                            .ConfigureAwait(false);

                        CollectionAssert.AreEqual(sendData, readData);
                    }
                }
            }
        }
    }
}
