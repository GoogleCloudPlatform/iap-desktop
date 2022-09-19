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

using Google.Apis.Util;
using System;
using System.Diagnostics;
using System.Text;

namespace Google.Solutions.IapTunneling.Iap
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
            //
            // 00-01 (len=2): Tag
            //

            public static uint Encode(
                byte[] messageBuffer,
                MessageTag tag)
            {
                ThrowIfBufferSmallerThan(messageBuffer, sizeof(ushort));

                BigEndian.EncodeUInt16((ushort)tag, messageBuffer, 0);
                return sizeof(MessageTag);
            }

            public static uint Decode(
                byte[] messageBuffer,
                out MessageTag tag)
            {
                ThrowIfBufferSmallerThan(messageBuffer, sizeof(ushort));

                tag = (MessageTag)BigEndian.DecodeUInt16(messageBuffer, 0);
                return sizeof(MessageTag);
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
                sid.ThrowIfNullOrEmpty(nameof(sid));

                var sidBytes = Encoding.ASCII.GetBytes(sid);
                var requiredSize = sizeof(ushort) + sizeof(uint) + (uint)sidBytes.Length;
                ThrowIfBufferSmallerThan(messageBuffer, requiredSize);

                BigEndian.EncodeUInt16((ushort)MessageTag.CONNECT_SUCCESS_SID, messageBuffer, 0);
                BigEndian.EncodeUInt32((uint)sidBytes.Length, messageBuffer, 2);
                Array.Copy(sidBytes, 0, messageBuffer, 6, sidBytes.Length);

                return requiredSize;
            }

            public static uint Decode(
                byte[] messageBuffer,
                out string sid)
            {
                ThrowIfBufferSmallerThan(messageBuffer, sizeof(ushort) + sizeof(uint));
                Debug.Assert((MessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) == MessageTag.CONNECT_SUCCESS_SID);

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

        public class ReconnectAck
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

                BigEndian.EncodeUInt16((ushort)MessageTag.RECONNECT_SUCCESS_ACK, messageBuffer, 0);
                BigEndian.EncodeUInt64((uint)ack, messageBuffer, 2);

                return MessageLength;
            }

            public static uint Decode(
                byte[] messageBuffer,
                out ulong ack)
            {
                ThrowIfBufferSmallerThan(messageBuffer, MessageLength);
                Debug.Assert((MessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) == MessageTag.RECONNECT_SUCCESS_ACK);

                ack = BigEndian.DecodeUInt64(messageBuffer, 2);

                return MessageLength;
            }
        }

        public class Ack
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

                BigEndian.EncodeUInt16((ushort)MessageTag.ACK, messageBuffer, 0);
                BigEndian.EncodeUInt64(ack, messageBuffer, 2);

                return MessageLength;
            }

            public static uint Decode(
                byte[] messageBuffer,
                out ulong ack)
            {
                ThrowIfBufferSmallerThan(messageBuffer, MessageLength);
                Debug.Assert((MessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) == MessageTag.ACK);

                ack = BigEndian.DecodeUInt64(messageBuffer, 2);

                return MessageLength;
            }
        }

        public class Data
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

                BigEndian.EncodeUInt16((ushort)MessageTag.DATA, messageBuffer, 0);
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
                Debug.Assert((MessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) == MessageTag.DATA);

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
    }
}
