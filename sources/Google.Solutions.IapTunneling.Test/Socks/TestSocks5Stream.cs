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
        public void WhenProtocolInvalid_ThenReadNegotiateMethodRequestThrowsException()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 4, 1 });
            stream.ReadData.Enqueue(new byte[] { 1 });

            var socksStream = new Socks5Stream(stream);

            AssertEx.ThrowsAggregateException<UnsupportedSocksVersionException>(
                () => socksStream
                    .ReadNegotiateMethodRequestAsync(CancellationToken.None)
                    .Wait());
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

        //---------------------------------------------------------------------
        // NegotiateMethodResponse
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenMethodSelected_ThenWriteNegotiateMethodResponseSucceeds()
        {
            var stream = new StaticStream();
            var socksStream = new Socks5Stream(stream);

            await socksStream.WriteNegotiateMethodResponseAsync(
                    new NegotiateMethodResponse(AuthenticationMethod.UsernamePassword),
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, stream.WriteData.Count);
            CollectionAssert.AreEqual(
                new byte[] { Socks5Stream.ProtocolVersion, (byte)AuthenticationMethod.UsernamePassword },
                stream.WriteData.Peek());
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
                0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D, 0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D,
                0xFA, 0xFB });

            var socksStream = new Socks5Stream(stream);

            var request = await socksStream
                .ReadConnectionRequestAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(5, request.Version);
            Assert.AreEqual(Command.Connect, request.Command);
            Assert.AreEqual(AddressType.IPv6, request.AddressType);
            CollectionAssert.AreEqual(
                new byte[] { 0x0A, 0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D, 0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D },
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

        //---------------------------------------------------------------------
        // ConnectionResponse
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenAddressIsIpv4_ThenConnectionResponseSucceeds()
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
        public async Task WhenAddressIsIpv6_ThenConnectionResponseSucceeds()
        {
            var stream = new StaticStream();
            var socksStream = new Socks5Stream(stream);

            await socksStream.WriteConnectionResponseAsync(
                    new ConnectionResponse(
                        Socks5Stream.ProtocolVersion,
                        ConnectionReply.Succeeded,
                        AddressType.IPv6,
                        new byte[] { 0x0A, 0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D, 0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D },
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
                    0x0A, 0x0B, 0x0C, 0x0D, 0x1A, 0x1B, 0x1C, 0x1D, 0x2A, 0x2B, 0x2C, 0x2D, 0x3A, 0x3B, 0x3C, 0x3D,
                    0xFA, 0xFB
                },
                stream.WriteData.Peek());
        }

        [Test]
        public async Task WhenAddressIsDomainName_ThenConnectionResponseSucceeds()
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
    }
}
