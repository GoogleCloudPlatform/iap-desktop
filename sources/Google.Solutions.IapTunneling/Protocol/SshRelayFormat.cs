//
// Copyright 2022 Google LLC
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

using Google.Solutions.Common.Format;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Text;

namespace Google.Solutions.Iap.Protocol
{
    /// <summary>
    /// Encode/decode SSH Relay messages.
    /// </summary>
    internal static class SshRelayFormat
    {
        /// <summary>
        /// Maximum size of arrays. Defined as 16K by the protocol
        /// specification.
        /// </summary>
        private const uint MaxArrayLength = 16 * 1024;

        //
        // DATA messages are the largest.
        //
        public const uint MaxMessageSize = 6 + MaxArrayLength;
        public const uint MinMessageSize = 7;

        private static void ThrowIfBufferSmallerThan(
            byte[] messageBuffer,
            uint minSize)
        {
            if (messageBuffer == null || messageBuffer.Length < minSize)
            {
                throw new ArgumentException("Message is truncated");
            }
        }

        //---------------------------------------------------------------------
        // Reading.
        //---------------------------------------------------------------------

        public static class Tag
        {
            public static uint Length = sizeof(ushort);

            //
            // 00-01 (len=2): Tag
            //

            public static uint Encode(
                byte[] messageBuffer,
                SshRelayMessageTag tag)
            {
                ThrowIfBufferSmallerThan(messageBuffer, sizeof(ushort));

                BigEndian.EncodeUInt16((ushort)tag, messageBuffer, 0);
                return sizeof(SshRelayMessageTag);
            }

            public static uint Decode(
                byte[] messageBuffer,
                out SshRelayMessageTag tag)
            {
                ThrowIfBufferSmallerThan(messageBuffer, sizeof(ushort));

                tag = (SshRelayMessageTag)BigEndian.DecodeUInt16(messageBuffer, 0);
                return sizeof(SshRelayMessageTag);
            }
        }

        public static class ConnectSuccessSid
        {
            //
            // 00-01 (len=2): Tag
            // 02-05 (len=4): Array length
            // 06-*         : SID
            //

            public static uint Encode(
                byte[] messageBuffer,
                string sid)
            {
                sid.ExpectNotEmpty(nameof(sid));

                var sidBytes = Encoding.ASCII.GetBytes(sid);
                var requiredSize = sizeof(ushort) + sizeof(uint) + (uint)sidBytes.Length;
                ThrowIfBufferSmallerThan(messageBuffer, requiredSize);

                BigEndian.EncodeUInt16((ushort)SshRelayMessageTag.CONNECT_SUCCESS_SID, messageBuffer, 0);
                BigEndian.EncodeUInt32((uint)sidBytes.Length, messageBuffer, 2);
                Array.Copy(sidBytes, 0, messageBuffer, 6, sidBytes.Length);

                return requiredSize;
            }

            public static uint Decode(
                byte[] messageBuffer,
                out string sid)
            {
                ThrowIfBufferSmallerThan(messageBuffer, sizeof(ushort) + sizeof(uint));
                Debug.Assert((SshRelayMessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) ==
                    SshRelayMessageTag.CONNECT_SUCCESS_SID);

                var arrayLength = BigEndian.DecodeUInt32(messageBuffer, 2);
                Debug.Assert(arrayLength <= MaxArrayLength);

                ThrowIfBufferSmallerThan(messageBuffer, sizeof(ushort) + sizeof(uint) + arrayLength);

                sid = Encoding.ASCII.GetString(
                    messageBuffer,
                    6,
                    (int)arrayLength);

                return sizeof(ushort) + sizeof(uint) + arrayLength;
            }
        }

        public static class ReconnectAck
        {
            //
            // 00-01 (len=2): Tag
            // 02-0A (len=8): ACK
            //

            public const uint MessageLength = sizeof(ushort) + sizeof(ulong);

            public static uint Encode(
                byte[] messageBuffer,
                ulong ack)
            {
                ThrowIfBufferSmallerThan(messageBuffer, MessageLength);

                BigEndian.EncodeUInt16((ushort)SshRelayMessageTag.RECONNECT_SUCCESS_ACK, messageBuffer, 0);
                BigEndian.EncodeUInt64((uint)ack, messageBuffer, 2);

                return MessageLength;
            }

            public static uint Decode(
                byte[] messageBuffer,
                out ulong ack)
            {
                ThrowIfBufferSmallerThan(messageBuffer, MessageLength);
                Debug.Assert((SshRelayMessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) ==
                    SshRelayMessageTag.RECONNECT_SUCCESS_ACK);

                ack = BigEndian.DecodeUInt64(messageBuffer, 2);

                return MessageLength;
            }
        }

        public static class Ack
        {
            //
            // 00-01 (len=2): Tag
            // 02-0A (len=8): ACK
            //

            public const uint MessageLength = sizeof(ushort) + sizeof(ulong);

            public static uint Encode(
                byte[] messageBuffer,
                ulong ack)
            {
                ThrowIfBufferSmallerThan(messageBuffer, MessageLength);

                BigEndian.EncodeUInt16((ushort)SshRelayMessageTag.ACK, messageBuffer, 0);
                BigEndian.EncodeUInt64(ack, messageBuffer, 2);

                return MessageLength;
            }

            public static uint Decode(
                byte[] messageBuffer,
                out ulong ack)
            {
                ThrowIfBufferSmallerThan(messageBuffer, MessageLength);
                Debug.Assert((SshRelayMessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) ==
                    SshRelayMessageTag.ACK);

                ack = BigEndian.DecodeUInt64(messageBuffer, 2);

                return MessageLength;
            }
        }

        public static class Data
        {
            //
            // 00-01 (len=2): Tag
            // 02-05 (len=4): Array length
            // 06-*         : Data
            //

            public const uint HeaderLength = sizeof(ushort) + sizeof(uint);

            public const uint MaxPayloadLength = MaxArrayLength;

            public const uint MaxMessageLength = MaxPayloadLength + HeaderLength;

            public static uint Encode(
                byte[] messageBuffer,
                byte[] data,
                uint dataOffset,
                uint dataLength)
            {
                if (dataLength > MaxArrayLength)
                {
                    throw new ArgumentException($"At most {MaxArrayLength} bytes can be sent at once");
                }
                else if (dataLength == 0)
                {
                    throw new ArgumentException($"At least 1 byte must be sent at once");
                }

                ThrowIfBufferSmallerThan(messageBuffer, sizeof(ushort) + sizeof(uint) + dataLength);

                BigEndian.EncodeUInt16((ushort)SshRelayMessageTag.DATA, messageBuffer, 0);
                BigEndian.EncodeUInt32(dataLength, messageBuffer, 2);
                Array.Copy(
                    data,
                    (int)dataOffset,
                    messageBuffer,
                    6,
                    dataLength);

                return HeaderLength + dataLength;
            }

            public static uint Decode(
                byte[] messageBuffer,
                byte[] targetBuffer,
                uint targetBufferOffset,
                uint targetBufferLength,
                out uint dataLength)
            {
                ThrowIfBufferSmallerThan(messageBuffer, HeaderLength + 1);
                Debug.Assert((SshRelayMessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) ==
                    SshRelayMessageTag.DATA);

                dataLength = BigEndian.DecodeUInt32(messageBuffer, 2);
                Debug.Assert(dataLength <= MaxArrayLength);
                Debug.Assert(dataLength > 0);

                ThrowIfBufferSmallerThan(messageBuffer, sizeof(ushort) + sizeof(uint) + dataLength);

                if (dataLength > targetBufferLength ||
                    targetBufferLength + targetBufferOffset > targetBuffer.Length)
                {
                    throw new IndexOutOfRangeException("Read buffer is too small");
                }

                Array.Copy(
                    messageBuffer,
                    6,
                    targetBuffer,
                    targetBufferOffset,
                    (int)dataLength);

                return HeaderLength + dataLength;
            }
        }

        public static class LongClose
        {
            //
            // 00-01 (len=2): Tag
            // 02-05 (len=4): Long close code
            // 06-0a (len=4): Long reason length
            // 0b-*         : Long reason string
            //

            public const uint MinMessageLength = sizeof(ushort) + sizeof(uint) + sizeof(uint);

            public static uint Encode(
                byte[] messageBuffer,
                SshRelayCloseCode closeCode,
                string closeReason)
            {
                byte[] closeReasonBytes = closeReason == null
                    ? Array.Empty<byte>()
                    : Encoding.UTF8.GetBytes(closeReason);

                ThrowIfBufferSmallerThan(messageBuffer, MinMessageLength + (uint)closeReasonBytes.Length);

                if (closeReason.Length > MaxArrayLength)
                {
                    throw new ArgumentException($"Reason must not exceed {MaxArrayLength} bytes");
                }

                BigEndian.EncodeUInt16((ushort)SshRelayMessageTag.LONG_CLOSE, messageBuffer, 0);
                BigEndian.EncodeUInt32((uint)closeCode, messageBuffer, 2);
                BigEndian.EncodeUInt32((uint)closeReasonBytes.Length, messageBuffer, 6);
                Array.Copy(
                    closeReasonBytes,
                    0,
                    messageBuffer,
                    10,
                    closeReasonBytes.Length);

                return MinMessageLength + (uint)closeReasonBytes.Length;
            }

            public static uint Decode(
                byte[] messageBuffer,
                out SshRelayCloseCode closeCode,
                out string closeReason)
            {
                ThrowIfBufferSmallerThan(messageBuffer, MinMessageLength);
                Debug.Assert((SshRelayMessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) ==
                    SshRelayMessageTag.LONG_CLOSE);

                closeCode = (SshRelayCloseCode)BigEndian.DecodeUInt32(messageBuffer, 2);
                var reasonLength = BigEndian.DecodeUInt32(messageBuffer, 6);

                ThrowIfBufferSmallerThan(messageBuffer, MinMessageLength + reasonLength);
                closeReason = Encoding.UTF8.GetString(messageBuffer, 10, (int)reasonLength);

                return MinMessageLength + reasonLength;
            }
        }
    }
}
