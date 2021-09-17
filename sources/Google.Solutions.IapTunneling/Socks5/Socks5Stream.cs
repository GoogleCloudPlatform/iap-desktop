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
    internal class Socks5Stream : IDisposable
    {
        public const byte ProtocolVersion = 5;

        /// <summary>
        /// Use a fragmenting stream so that when we read N bytes, we know
        /// that we'll get N bytes, even if it requires multiple reads
        /// to actually get the data.
        /// </summary>
        private readonly BufferedNetworkStream stream;

        public Socks5Stream(INetworkStream stream) : this(new BufferedNetworkStream(stream))
        {
        }

        public Socks5Stream(BufferedNetworkStream stream)
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

        public async Task WriteNegotiateMethodRequestAsync(
            NegotiateMethodRequest request,
            CancellationToken cancellationToken)
        {
            //
            // +----+----------+----------+
            // |VER | NMETHODS | METHODS  |
            // +----+----------+----------+
            // | 1  | 1        | 1 to 255 |
            // +----+----------+----------+
            //
            var buffer = new byte[2 + request.Methods.Length];
            buffer[0] = request.Version;
            buffer[1] = (byte)request.Methods.Length;
            
            for (int i = 0; i < request.Methods.Length; i++)
            {
                buffer[2 + i] = (byte)request.Methods[i];
            }

            await this.stream
                .WriteAsync(buffer, 0, buffer.Length, cancellationToken)
                .ConfigureAwait(false);
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
            var message = new byte[] { (byte)response.Version, (byte)response.Method };
            await this.stream.WriteAsync(
                    message,
                    0,
                    message.Length,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<NegotiateMethodResponse> ReadNegotiateMethodResponseAsync(
            CancellationToken cancellationToken)
        {
            var buffer = new byte[2];
            await this.stream.ReadAsync(
                    buffer,
                    0,
                    buffer.Length,
                    cancellationToken)
                .ConfigureAwait(false);
            return new NegotiateMethodResponse(buffer[0], (AuthenticationMethod)buffer[1]);
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


        public async Task WriteConnectionRequestAsync(
            ConnectionRequest request,
            CancellationToken cancellationToken)
        {
            //
            // +----+-----+-------+------+----------+----------+
            // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
            // +----+-----+-------+------+----------+----------+
            // | 1  |  1  | X'00' |  1   | Variable |    2     |
            // +----+-----+-------+------+----------+----------+
            //

            int addressLength = AddressLengthFromAddressType(
                request.AddressType,
                request.DestinationAddress[0]);

            var buffer = new byte[6 + addressLength];
            buffer[0] = request.Version;
            buffer[1] = (byte)request.Command;
            buffer[2] = 0;
            buffer[3] = (byte)request.AddressType;
            Array.Copy(request.DestinationAddress, 0, buffer, 4, addressLength);
            BigEndian.EncodeUInt16(request.DestinationPort, buffer, buffer.Length - 2);

            await this.stream.WriteAsync(
                    buffer,
                    0,
                    buffer.Length,
                    cancellationToken)
                .ConfigureAwait(false);
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


        public async Task<ConnectionResponse> ReadConnectionResponseAsync(
            CancellationToken cancellationToken)
        {
            //
            // +----+-----+-------+------+----------+----------+
            // |VER | REP |  RSV  | ATYP | BND.ADDR | BND.PORT |
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
                    "Connection closed before completing ConnectionResponse");
            }

            var version = firstPart[0];
            var reply = (ConnectionReply)firstPart[1];
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
                    "Connection closed before completing ConnectionResponse");
            }

            var address = new byte[addressLength];
            Array.Copy(secondPart, 0, address, 0, address.Length);

            return new ConnectionResponse(
                version,
                reply,
                addressType,
                address,
                BigEndian.DecodeUInt16(secondPart, addressLength));
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

        public void Dispose()
        {
            this.stream.Dispose();
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
}
