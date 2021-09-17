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

using Google.Solutions.Common.Test;
using Google.Solutions.IapTunneling.Net;
using Google.Solutions.IapTunneling.Socks5;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Socks
{
    [TestFixture]
    public class TestSocks5Stream : IapFixtureBase
    {
        private class StaticStream : INetworkStream
        {
            public Queue<byte[]> ReadData { get; } = new Queue<byte[]>();
            public Queue<byte[]> WriteData { get; } = new Queue<byte[]>();

            public int MaxWriteSize => int.MaxValue;

            public int MinReadSize => 0;

            public Task CloseAsync(CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public void Dispose() => new NotImplementedException();

            public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var data = new byte[count];
                Assert.AreEqual(data.Length, count);
                Array.Copy(buffer, offset, data, 0, count);

                this.WriteData.Enqueue(data);
                return Task.CompletedTask;
            }

            public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var data = this.ReadData.Dequeue();

                var read = Math.Min(data.Length, count);
                Array.Copy(data, 0, buffer, offset, read);
                return Task.FromResult(read);
            }
        }

        //---------------------------------------------------------------------
        // NegotiateMethodRequest
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProtocolInvalid_ThenReadNegotiateMethodRequestSucceeds()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 4, 1 });
            stream.ReadData.Enqueue(new byte[] { 1 });

            var socksStream = new Socks5Stream(stream);

            var request = await socksStream
                .ReadNegotiateMethodRequestAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(4, request.Version);
        }

        [Test]
        public async Task WhenNoMethodSelected_ThenReadNegotiateMethodRequestSucceeds()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 5, 0 });

            var socksStream = new Socks5Stream(stream);

            var request = await socksStream
                .ReadNegotiateMethodRequestAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(5, request.Version);
            Assert.AreEqual(0, request.Methods.Length);
        }

        [Test]
        public async Task WhenMultipleMethodSelected_ThenReadNegotiateMethodRequestSucceeds()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 5, 2 });
            stream.ReadData.Enqueue(new byte[] { 1, 2 });

            var socksStream = new Socks5Stream(stream);

            var request = await socksStream
                .ReadNegotiateMethodRequestAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(5, request.Version);
            Assert.AreEqual(2, request.Methods.Length);
            Assert.AreEqual(AuthenticationMethod.GssApi, request.Methods[0]);
            Assert.AreEqual(AuthenticationMethod.UsernamePassword, request.Methods[1]);
        }

        [Test]
        public async Task WhenReadsSplit_ThenReadNegotiateMethodRequestSucceeds()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 5 });
            stream.ReadData.Enqueue(new byte[] { 2 });
            stream.ReadData.Enqueue(new byte[] { 1 });
            stream.ReadData.Enqueue(new byte[] { 2 });

            var socksStream = new Socks5Stream(stream);

            var request = await socksStream
                .ReadNegotiateMethodRequestAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(5, request.Version);
            Assert.AreEqual(2, request.Methods.Length);
            Assert.AreEqual(AuthenticationMethod.GssApi, request.Methods[0]);
            Assert.AreEqual(AuthenticationMethod.UsernamePassword, request.Methods[1]);
        }

        [Test]
        public async Task WhenMultipleMethodSelected_ThenWriteNegotiateMethodRequestSucceeds()
        {
            var stream = new StaticStream();

            var socksStream = new Socks5Stream(stream);
            await socksStream.WriteNegotiateMethodRequestAsync(
                    new NegotiateMethodRequest(
                        5,
                        new[] {
                            AuthenticationMethod.NoAuthenticationRequired,
                            AuthenticationMethod.GssApi}),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, stream.WriteData.Count);
            
            var data = stream.WriteData.Dequeue();
            CollectionAssert.AreEqual(
                new byte[] { 5, 2, 0, 1 }, 
                data);
        }

        //---------------------------------------------------------------------
        // NegotiateMethodResponse
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenMethodSelected_ThenWriteNegotiateMethodResponseSucceeds()
        {
            var stream = new StaticStream();
            var socksStream = new Socks5Stream(stream);

            await socksStream.WriteNegotiateMethodResponseAsync(
                    new NegotiateMethodResponse(
                        Socks5Stream.ProtocolVersion,
                        AuthenticationMethod.UsernamePassword),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, stream.WriteData.Count);
            CollectionAssert.AreEqual(
                new byte[] { Socks5Stream.ProtocolVersion, (byte)AuthenticationMethod.UsernamePassword },
                stream.WriteData.Peek());
        }

        [Test]
        public async Task WhenMethodSelected_ThenReadNegotiateMethodResponseSucceeds()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 5, 2 });

            var socksStream = new Socks5Stream(stream);

            var response = await socksStream.ReadNegotiateMethodResponseAsync(
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
            Assert.AreEqual(AuthenticationMethod.UsernamePassword, response.Method);
        }

        //---------------------------------------------------------------------
        // ConnectionRequest
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenAddressIsIpv4_ThenReadConnectionRequestSucceeds()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 5, 1, 0, 1, 0x0A });
            stream.ReadData.Enqueue(new byte[] { 
                0x0B, 0x0C, 0x0D, 
                0xFA, 0xFB });

            var socksStream = new Socks5Stream(stream);

            var request = await socksStream
                .ReadConnectionRequestAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(5, request.Version);
            Assert.AreEqual(Command.Connect, request.Command);
            Assert.AreEqual(AddressType.IPv4, request.AddressType);
            CollectionAssert.AreEqual(
                new byte[] { 0x0A, 0x0B, 0x0C, 0x0D },
                request.DestinationAddress);
            Assert.AreEqual(0xFAFB, request.DestinationPort);
        }

        [Test]
        public async Task WhenAddressIsIpv6_ThenReadConnectionRequestSucceeds()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 5, 1, 0, 4, 0x0A });
            stream.ReadData.Enqueue(new byte[] { 
                0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D, 
                0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D,
                0xFA, 0xFB });

            var socksStream = new Socks5Stream(stream);

            var request = await socksStream
                .ReadConnectionRequestAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(5, request.Version);
            Assert.AreEqual(Command.Connect, request.Command);
            Assert.AreEqual(AddressType.IPv6, request.AddressType);
            CollectionAssert.AreEqual(
                new byte[] { 0x0A, 0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D, 
                             0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D },
                request.DestinationAddress);
            Assert.AreEqual(0xFAFB, request.DestinationPort);
        }

        [Test]
        public async Task WhenAddressIsDomainName_ThenReadConnectionRequestSucceeds()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 5, 1, 0, 3, 3 });
            stream.ReadData.Enqueue(new byte[] { 
                (byte)'b', (byte)'a', (byte)'r',
                0xFA, 0xFB });

            var socksStream = new Socks5Stream(stream);

            var request = await socksStream
                .ReadConnectionRequestAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(5, request.Version);
            Assert.AreEqual(Command.Connect, request.Command);
            Assert.AreEqual(AddressType.DomainName, request.AddressType);
            CollectionAssert.AreEqual(
                new byte[] { 3, (byte)'b', (byte)'a', (byte)'r' },
                request.DestinationAddress);
            Assert.AreEqual(0xFAFB, request.DestinationPort);
        }

        [Test]
        public void WhenAddressIsUnknown_ThenReadConnectionRequestThrowsException()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 5, 1, 0, 0, 3 });

            var socksStream = new Socks5Stream(stream);

            AssertEx.ThrowsAggregateException<SocksProtocolException>(
                () => socksStream
                    .ReadConnectionRequestAsync(CancellationToken.None)
                    .Wait());
        }

        [Test]
        public async Task WhenAddressIsIpv4_ThenWriteConnectionRequestSucceeds()
        {
            var stream = new StaticStream();
            var socksStream = new Socks5Stream(stream);

            await socksStream
                .WriteConnectionRequestAsync(
                    new ConnectionRequest(
                        Socks5Stream.ProtocolVersion,
                        Command.Connect,
                        AddressType.IPv4,
                        new byte[] { 0x0A, 0x0B, 0x0C, 0x0D },
                        0xFAFB),
                    CancellationToken.None)
                .ConfigureAwait(false);

            var data = stream.WriteData.Dequeue();
            CollectionAssert.AreEqual(new byte[] {
                5, 1, 0, 1, 
                0x0A, 0x0B, 0x0C, 0x0D, 
                0xFA, 0xFB },
                data);
        }

        [Test]
        public async Task WhenAddressIsIPv6_ThenWriteConnectionRequestSucceeds()
        {
            var stream = new StaticStream();
            var socksStream = new Socks5Stream(stream);

            await socksStream
                .WriteConnectionRequestAsync(
                    new ConnectionRequest(
                        Socks5Stream.ProtocolVersion,
                        Command.Connect,
                        AddressType.IPv6,
                        new byte[] { 0x0A, 0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D,
                                     0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D },
                        0xFAFB),
                    CancellationToken.None)
                .ConfigureAwait(false);

            var data = stream.WriteData.Dequeue();
            CollectionAssert.AreEqual(new byte[] {
                5, 1, 0, 4,
                0x0A, 0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D,
                0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D,
                0xFA, 0xFB },
                data);
        }

        [Test]
        public async Task WhenAddressIsDomainName_ThenWriteConnectionRequestSucceeds()
        {
            var stream = new StaticStream();
            var socksStream = new Socks5Stream(stream);

            await socksStream
                .WriteConnectionRequestAsync(
                    new ConnectionRequest(
                        Socks5Stream.ProtocolVersion,
                        Command.Connect,
                        AddressType.DomainName,
                        new byte[] { 3, (byte)'b', (byte)'a', (byte)'r' },
                        0xFAFB),
                    CancellationToken.None)
                .ConfigureAwait(false);

            var data = stream.WriteData.Dequeue();
            CollectionAssert.AreEqual(new byte[] {
                5, 1, 0, 3, 3,
                (byte)'b', (byte)'a', (byte)'r',
                0xFA, 0xFB },
                data);
        }

        //---------------------------------------------------------------------
        // ConnectionResponse
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenAddressIsIpv4_ThenWriteConnectionResponseSucceeds()
        {
            var stream = new StaticStream();
            var socksStream = new Socks5Stream(stream);

            await socksStream.WriteConnectionResponseAsync(
                    new ConnectionResponse(
                        Socks5Stream.ProtocolVersion,
                        ConnectionReply.Succeeded,
                        AddressType.IPv4,
                        new byte[] { 0xA, 0xB, 0xC, 0xD },
                        0xFAFB),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, stream.WriteData.Count);
            CollectionAssert.AreEqual(
                new byte[] { 
                    Socks5Stream.ProtocolVersion, 
                    (byte)ConnectionReply.Succeeded,
                    0,
                    (byte)AddressType.IPv4,
                    0xA, 0xB, 0xC, 0xD,
                    0xFA, 0xFB
                },
                stream.WriteData.Peek());
        }

        [Test]
        public async Task WhenAddressIsIpv6_ThenWriteConnectionResponseSucceeds()
        {
            var stream = new StaticStream();
            var socksStream = new Socks5Stream(stream);

            await socksStream.WriteConnectionResponseAsync(
                    new ConnectionResponse(
                        Socks5Stream.ProtocolVersion,
                        ConnectionReply.Succeeded,
                        AddressType.IPv6,
                        new byte[] { 0x0A, 0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D, 
                                     0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D },
                        0xFAFB),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, stream.WriteData.Count);
            CollectionAssert.AreEqual(
                new byte[] {
                    Socks5Stream.ProtocolVersion,
                    (byte)ConnectionReply.Succeeded,
                    0,
                    (byte)AddressType.IPv6,
                    0x0A, 0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D, 
                    0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D,
                    0xFA, 0xFB
                },
                stream.WriteData.Peek());
        }

        [Test]
        public async Task WhenAddressIsDomainName_ThenWriteConnectionResponseSucceeds()
        {
            var stream = new StaticStream();
            var socksStream = new Socks5Stream(stream);

            await socksStream.WriteConnectionResponseAsync(
                    new ConnectionResponse(
                        Socks5Stream.ProtocolVersion,
                        ConnectionReply.Succeeded,
                        AddressType.DomainName,
                        new byte[] { 3, (byte)'b', (byte)'a', (byte)'r' },
                        0xFAFB),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, stream.WriteData.Count);
            CollectionAssert.AreEqual(
                new byte[] {
                    Socks5Stream.ProtocolVersion,
                    (byte)ConnectionReply.Succeeded,
                    0,
                    (byte)AddressType.DomainName,
                    3, (byte)'b', (byte)'a', (byte)'r',
                    0xFA, 0xFB
                },
                stream.WriteData.Peek());
        }

        [Test]
        public async Task WhenAddressIsIpv4_ThenReadConnectionResponseSucceeds()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(
                new byte[] {
                    Socks5Stream.ProtocolVersion,
                    (byte)ConnectionReply.Succeeded,
                    0,
                    (byte)AddressType.IPv4,
                    0xA,
                });
            stream.ReadData.Enqueue(
                new byte[] {
                    0xB, 0xC, 0xD,
                    0xFA, 0xFB
                });

            var socksStream = new Socks5Stream(stream);

            var response = await socksStream.ReadConnectionResponseAsync(
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
            Assert.AreEqual(ConnectionReply.Succeeded, response.Reply);
            Assert.AreEqual(AddressType.IPv4, response.AddressType);
            CollectionAssert.AreEqual(new byte[] { 0xA, 0xB, 0xC, 0xD }, response.ServerAddress);
            Assert.AreEqual(0xFAFB, response.ServerPort);
        }

        [Test]
        public async Task WhenAddressIsIpv6_ThenReadConnectionResponseSucceeds()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(
                new byte[] {
                    Socks5Stream.ProtocolVersion,
                    (byte)ConnectionReply.Succeeded,
                    0,
                    (byte)AddressType.IPv6,
                    0x0A
                });
            stream.ReadData.Enqueue(
                new byte[] {
                    0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D,
                    0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D,
                    0xFA, 0xFB
                });
            var socksStream = new Socks5Stream(stream);

            var response = await socksStream.ReadConnectionResponseAsync(
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
            Assert.AreEqual(ConnectionReply.Succeeded, response.Reply);
            Assert.AreEqual(AddressType.IPv6, response.AddressType);
            CollectionAssert.AreEqual(
                new byte[] { 0x0A, 0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D,
                             0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D }, 
                response.ServerAddress);
            Assert.AreEqual(0xFAFB, response.ServerPort);
        }

        [Test]
        public async Task WhenAddressIsDomainName_ThenReadConnectionResponseSucceeds()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(
                new byte[] {
                    Socks5Stream.ProtocolVersion,
                    (byte)ConnectionReply.Succeeded,
                    0,
                    (byte)AddressType.DomainName,
                    3
                });
            stream.ReadData.Enqueue(
                new byte[] {
                    (byte)'b', (byte)'a', (byte)'r',
                    0xFA, 0xFB
                });
            var socksStream = new Socks5Stream(stream);

            var response = await socksStream.ReadConnectionResponseAsync(
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
            Assert.AreEqual(ConnectionReply.Succeeded, response.Reply);
            Assert.AreEqual(AddressType.DomainName, response.AddressType);
            CollectionAssert.AreEqual(
                new byte[] { 3, (byte)'b', (byte)'a', (byte)'r' },
                response.ServerAddress);
            Assert.AreEqual(0xFAFB, response.ServerPort);
        }
    }
}
