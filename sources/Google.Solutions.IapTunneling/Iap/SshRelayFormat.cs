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
        //
        // The protocol defines 16K as the max.
        //
        public const uint MaxArrayLength = 16 * 1024;

        public const uint MaxDataPayloadLength = MaxArrayLength;

        //
        // DATA messages are the largest.
        //
        public const uint MaxMessageSize = 6 + MaxArrayLength;

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

        public static uint DecodeTag(
            byte[] messageBuffer,
            out MessageTag tag)
        {
            //
            // 00-01 (len=2): Tag
            //

            ThrowIfBufferSmallerThan(messageBuffer, sizeof(UInt16));

            tag = (MessageTag)BigEndian.DecodeUInt16(messageBuffer, 0);
            return sizeof(MessageTag);
        }

        public static uint DecodeConnectSuccessSid(
            byte[] messageBuffer,
            out string sid)
        {
            //
            // 00-01 (len=2): Tag
            // 02-05 (len=4): Array length
            // 06-*         : SID
            //

            ThrowIfBufferSmallerThan(messageBuffer, sizeof(UInt16) + sizeof(UInt32));
            Debug.Assert((MessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) == MessageTag.CONNECT_SUCCESS_SID);

            var arrayLength = BigEndian.DecodeUInt32(messageBuffer, 2);
            Debug.Assert(arrayLength <= MaxArrayLength);

            ThrowIfBufferSmallerThan(messageBuffer, sizeof(UInt16) + sizeof(UInt32) + arrayLength);

            sid = new ASCIIEncoding().GetString(
                messageBuffer,
                6,
                (int)arrayLength);

            return sizeof(UInt16) + sizeof(UInt32) + arrayLength;
        }

        public static uint DecodeReconnectAck(
            byte[] messageBuffer,
            out ulong ack)
        {
            //
            // 00-01 (len=2): Tag
            // 02-0A (len=8): ACK
            //

            ThrowIfBufferSmallerThan(messageBuffer, sizeof(UInt16) + sizeof(UInt64));
            Debug.Assert((MessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) == MessageTag.RECONNECT_SUCCESS_ACK);

            ack = BigEndian.DecodeUInt64(messageBuffer, 2);

            return sizeof(UInt16) + sizeof(UInt64);
        }

        public static uint DecodeAck(
            byte[] messageBuffer,
            out ulong ack)
        {
            //
            // 00-01 (len=2): Tag
            // 02-0A (len=8): ACK
            //

            ThrowIfBufferSmallerThan(messageBuffer, sizeof(UInt16) + sizeof(UInt64));
            Debug.Assert((MessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) == MessageTag.ACK);

            ack = BigEndian.DecodeUInt64(messageBuffer, 2);

            return sizeof(UInt16) + sizeof(UInt64);
        }

        public static uint DecodeData(
            byte[] messageBuffer,
            byte[] data,
            uint dataOffset,
            uint dataLength)
        {
            //
            // 00-01 (len=2): Tag
            // 02-05 (len=4): Array length
            // 06-*         : Data
            //

            ThrowIfBufferSmallerThan(messageBuffer, sizeof(UInt16) + sizeof(UInt32) + 1);
            Debug.Assert((MessageTag)BigEndian.DecodeUInt16(messageBuffer, 0) == MessageTag.DATA);

            var arrayLength = BigEndian.DecodeUInt32(messageBuffer, 2);
            Debug.Assert(arrayLength <= MaxArrayLength);
            Debug.Assert(arrayLength > 0);

            ThrowIfBufferSmallerThan(messageBuffer, sizeof(UInt16) + sizeof(UInt32) + arrayLength);

            if (arrayLength > dataLength || dataLength + dataOffset > data.Length)
            {
                throw new IndexOutOfRangeException("Read buffer is too small");
            }

            Array.Copy(
                messageBuffer,
                6,
                data,
                dataOffset,
                (int)arrayLength);

            return sizeof(UInt16) + sizeof(UInt32) + arrayLength;
        }

        //---------------------------------------------------------------------
        // Writing.
        //---------------------------------------------------------------------

        public static uint EncodeAck(
            byte[] messageBuffer,
            ulong ack)
        {
            //
            // 00-01 (len=2): Tag
            // 02-0A (len=8): ACK
            //

            ThrowIfBufferSmallerThan(messageBuffer, sizeof(UInt16) + sizeof(UInt64));

            BigEndian.EncodeUInt16((ushort)MessageTag.ACK, messageBuffer, 0);
            BigEndian.EncodeUInt64(ack, messageBuffer, 2);

            return sizeof(UInt16) + sizeof(UInt64);
        }

        public static uint EncodeData(
            byte[] messageBuffer,
            byte[] data,
            uint dataOffset,
            uint dataLength)
        {
            //
            // 00-01 (len=2): Tag
            // 02-05 (len=4): Array length
            // 06-*         : Data
            //

            if (dataLength > MaxArrayLength)
            {
                throw new ArgumentException($"At most {MaxArrayLength} bytes can be sent at once");
            }
            else if (dataLength == 0)
            {
                throw new ArgumentException($"At least 1 byte must be sent at once");
            }

            ThrowIfBufferSmallerThan(messageBuffer, sizeof(UInt16) + sizeof(UInt32) + dataLength);

            BigEndian.EncodeUInt16((ushort)MessageTag.DATA, messageBuffer, 0);
            BigEndian.EncodeUInt32(dataLength, messageBuffer, 2);
            Array.Copy(
                data,
                (int)dataOffset,
                messageBuffer,
                6,
                dataLength);

            return sizeof(UInt16) + sizeof(UInt32) + dataLength;
        }
    }
}
