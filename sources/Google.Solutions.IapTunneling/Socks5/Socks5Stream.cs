using Google.Solutions.IapTunneling.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Socks5
{
    /// <summary>
    /// Stream for reading and writing SOCKS5 protocol messages.
    /// The class only peforms minimal validation.
    /// </summary>
    internal class Socks5Stream
    {
        public const byte ProtocolVersion = 5;

        /// <summary>
        /// Use a fragmenting stream so that when we read N bytes, we know
        /// that we'll get N bytes, even if it requires multiple reads
        /// to actually get the data.
        /// </summary>
        private readonly FragmentingStream stream;

        public Socks5Stream(INetworkStream stream) : this(new FragmentingStream(stream))
        {
        }

        public Socks5Stream(FragmentingStream stream)
        {
            this.stream = stream;
        }

        public async Task<NegotiateMethodRequest> ReadNegotiateMethodRequestAsync(
            CancellationToken cancellationToken)
        {
            //
            // +----+----------+----------+
            // |VER | NMETHODS | METHODS  |
            // +----+----------+----------+
            // | 1  | 1        | 1 to 255 |
            // +----+----------+----------+
            //

            var buffer = new byte[NegotiateMethodRequest.MaxSize];
            var bytesRead = await this.stream.ReadAsync(
                    buffer,
                    0,
                    2,
                    cancellationToken)
                .ConfigureAwait(false);
            if (bytesRead != 2)
            {
                throw new SocksProtocolException(
                    "Connection closed before completing NegotiateMethodRequest");
            }

            var version = buffer[0];
            if (version != ProtocolVersion)
            {
                throw new UnsupportedSocksVersionException("Unsupported SOCKS version " + version);
            }

            var nMethods = buffer[1];
            if (nMethods != 0)
            {
                //
                // Read methods.
                //
                await this.stream.ReadAsync(
                        buffer,
                        2,
                        nMethods,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            var methods = new AuthenticationMethod[nMethods];
            for (int i = 0; i < nMethods; i++)
            {
                methods[i] = (AuthenticationMethod)buffer[i + 2];
            }

            return new NegotiateMethodRequest(
                version,
                methods);
        }

        public async Task WriteNegotiateMethodResponseAsync(
            NegotiateMethodResponse response,
            CancellationToken cancellationToken)
        {
            //
            //  +----+--------+
            //  |VER | METHOD |
            //  +----+--------+
            //  | 1  |   1    |
            //  +----+--------+
            //
            var message = new byte[] { ProtocolVersion, (byte)response.Method };
            await this.stream.WriteAsync(
                    message,
                    0,
                    message.Length,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<ConnectionRequest> ReadConnectionRequestAsync(
            CancellationToken cancellationToken)
        {
            //
            // +----+-----+-------+------+----------+----------+
            // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
            // +----+-----+-------+------+----------+----------+
            // | 1  |  1  | X'00' |  1   | Variable |    2     |
            // +----+-----+-------+------+----------+----------+
            //

            //
            // Read first part.
            //
            var firstPart = new byte[5];
            if (await this.stream.ReadAsync(
                    firstPart,
                    0,
                    firstPart.Length,
                    cancellationToken)
                .ConfigureAwait(false) != firstPart.Length)
            {
                throw new SocksProtocolException(
                    "Connection closed before completing ConnectionRequest");
            }

            var version = firstPart[0];
            if (version != ProtocolVersion)
            {
                throw new UnsupportedSocksVersionException("Unsupported SOCKS version " + version);
            }

            var command = (Command)firstPart[1];
            var addressType = (AddressType)firstPart[3];

            int addressLength = AddressLengthFromAddressType(
                addressType,
                firstPart[4]);

            //
            // Read the second (dynamic-length) part.
            //
            var secondPart = new byte[addressLength + 2];
            secondPart[0] = firstPart[4];
            if (await this.stream.ReadAsync(
                    secondPart,
                    1,
                    secondPart.Length - 1,
                    cancellationToken)
                .ConfigureAwait(false) != secondPart.Length - 1)
            {
                throw new SocksProtocolException(
                    "Connection closed before completing ConnectionRequest");
            }

            var address = new byte[addressLength];
            Array.Copy(secondPart, 0, address, 0, address.Length);

            return new ConnectionRequest(
                version,
                command,
                addressType,
                address,
                BigEndian.DecodeUInt16(secondPart, addressLength));
        }

        public async Task WriteConnectionResponseAsync(
            ConnectionResponse response,
            CancellationToken cancellationToken)
        {
            Debug.Assert(
                AddressLengthFromAddressType(response.AddressType, response.ServerAddress[0]) ==
                response.ServerAddress.Length);
            
            //
            // +----+-----+-------+------+----------+----------+
            // |VER | REP |  RSV  | ATYP | BND.ADDR | BND.PORT |
            // +----+-----+-------+------+----------+----------+
            // | 1  |  1  | X'00' |  1   | Variable |    2     |
            // +----+-----+-------+------+----------+----------+
            //

            var buffer = new byte[6 + AddressLengthFromAddressType(
                response.AddressType,
                response.ServerAddress[0])];

            buffer[0] = ProtocolVersion;
            buffer[1] = (byte)response.Reply;
            buffer[2] = 0x00; // Reserved
            buffer[3] = (byte)response.AddressType;
            Array.Copy(response.ServerAddress, 0, buffer, 4, response.ServerAddress.Length);
            
            BigEndian.EncodeUInt16(response.ServerPort, buffer, buffer.Length - 2);

            await this.stream.WriteAsync(
                    buffer,
                    0,
                    buffer.Length,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private static byte AddressLengthFromAddressType(
            AddressType addressType,
            byte firstByteOfAddress)
        {
            switch (addressType)
            {
                case AddressType.IPv4:
                    return 4;

                case AddressType.IPv6:
                    return 16;

                case AddressType.DomainName:
                    return (byte)(firstByteOfAddress + 1);

                default:
                    throw new SocksProtocolException("Unknown address type " + addressType);
            }
        }
    }

    public class SocksException : Exception
    {
        public SocksException(string message) : base(message)
        {
        }
    }

    public class SocksProtocolException : SocksException
    {
        public SocksProtocolException(string message) : base(message)
        {
        }
    }

    public class UnsupportedSocksVersionException : SocksProtocolException
    {
        public UnsupportedSocksVersionException(string message) : base(message)
        {
        }
    }
}
