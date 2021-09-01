//
// Copyright 2019 Google LLC
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
using System.Text;

namespace Google.Solutions.IapTunneling.Iap
{
    /// <summary>
    /// Buffer for reading an arbitrary message and then
    /// reinterpreting it as message based on the tag.
    /// </summary>
    internal class MessageBuffer
    {
        public byte[] Buffer { get; }

        public MessageBuffer(byte[] buffer)
        {
            this.Buffer = buffer;
        }

        public MessageTag PeekMessageTag()
        {
            return (MessageTag)BigEndian.DecodeUInt16(this.Buffer, 0);
        }

        public DataMessage AsDataMessage()
        {
            return new DataMessage(this.Buffer);
        }

        public SidMessage AsSidMessage()
        {
            return new SidMessage(this.Buffer);
        }

        public AckMessage AsAckMessage()
        {
            return new AckMessage(this.Buffer);
        }

        public ArraySegment<byte> AsArraySegment()
        {
            return new ArraySegment<byte>(this.Buffer, 0, this.Buffer.Length);
        }
    }

    /// <summary>
    /// Message tags used by SSH Relay v4.
    /// </summary>
    internal enum MessageTag : UInt16
    {
        UNUSED = 0,
        CONNECT_SUCCESS_SID = 1,
        RECONNECT_SUCCESS_ACK = 2,
        DEPRECATED = 3,
        DATA = 4,
        ACK_LATENCY = 5,
        REPLY_LATENCY = 6,
        ACK = 7
    };

    /// <summary>
    /// Connection close codes used by SSH Relay v4.
    /// </summary>
    public enum CloseCode : int
    {
        NORMAL = 1000,
        ERROR_UNKNOWN = 4000,
        SID_UNKNOWN = 4001,
        SID_IN_USE = 4002,
        FAILED_TO_CONNECT_TO_BACKEND = 4003,
        REAUTHENTICATION_REQUIRED = 4004,
        BAD_ACK = 4005,
        INVALID_ACK = 4006,
        INVALID_WEBSOCKET_OPCODE = 4007,
        INVALID_TAG = 4008,
        DESTINATION_WRITE_FAILED = 4009,
        DESTINATION_READ_FAILED = 4010,

        INVALID_DATA = 4013,
        NOT_AUTHORIZED = 4033,
        LOOKUP_FAILED = 4047,
        LOOKUP_FAILED_RECONNECT = 4051
    }

    /// <summary>
    /// Base class for messages.
    /// </summary>
    internal abstract class MessageBase
    {
        public byte[] Buffer { get; }

        public abstract int BufferLength { get; }

        public MessageBase(byte[] buffer)
        {
            this.Buffer = buffer;
        }

        public MessageTag Tag
        {
            get { return (MessageTag)BigEndian.DecodeUInt16(this.Buffer, 0); }
            set { BigEndian.EncodeUInt16((ushort)value, this.Buffer, 0); }
        }
    }

    /// <summary>
    /// Data message. These messages are used for sending or receiving
    /// arbitrary payloads.
    /// </summary>
    internal class DataMessage : MessageBase
    {
        public const uint DataOffset = 6;

        public const uint MaxTotalLength = ushort.MaxValue;

        // Although the length is measured by a UInt16, the protocol defines
        // 16KB as the maximum array length.
        public const uint MaxDataLength = MaxTotalLength - DataOffset;

        public DataMessage(byte[] buffer) : base(buffer)
        {
        }

        public DataMessage(uint dataLength) : base(new byte[dataLength + DataOffset])
        {
        }

        public ulong SequenceNumber { get; set; }

        public ulong ExpectedAck => SequenceNumber + (ulong)DataLength;

        public uint DataLength
        {
            get
            {
                return BigEndian.DecodeUInt32(this.Buffer, 2);
            }
            set
            {
                if (value < 0 || value > MaxDataLength)
                {
                    throw new ArgumentOutOfRangeException("DataLength");
                }

                BigEndian.EncodeUInt32((ushort)value, this.Buffer, 2);
            }
        }

        public override int BufferLength
        {
            get { return (int)(DataOffset + this.DataLength); }
        }

        public override string ToString()
        {
            return $"Data [Seq: {this.SequenceNumber}, Len: {this.DataLength} " +
                   $"ExpAck: {this.ExpectedAck}]";
        }
    }

    /// <summary>
    /// SID message, to be received after establishing a tunnel.
    /// </summary>
    internal class SidMessage : DataMessage
    {
        public const uint MinimumExpectedLength = 7;

        public SidMessage(byte[] buffer) : base(buffer)
        {
        }

        public string Sid
        {
            get
            {
                return new ASCIIEncoding().GetString(
                    this.Buffer,
                    (int)DataOffset,
                    (int)this.DataLength);
            }
        }

        public override int BufferLength
        {
            get { return (int)(DataOffset + this.DataLength); }
        }

        public override string ToString()
        {
            return $"Sid [{this.Sid}]";
        }
    }

    /// <summary>
    /// ACK message, used for acknoledging sent and received data.
    /// </summary>
    internal class AckMessage : MessageBase
    {
        public const uint ExpectedLength = 10;

        public AckMessage(byte[] buffer) : base(buffer)
        {
        }

        public const uint AckOffset = 2;

        public ulong Ack
        {
            get { return BigEndian.DecodeUInt64(this.Buffer, 2); }
            set { BigEndian.EncodeUInt64(value, this.Buffer, 2); }
        }

        public override int BufferLength
        {
            get { return (int)(AckOffset + sizeof(ulong)); }
        }

        public override string ToString()
        {
            return $"Ack [{this.Ack}]";
        }
    }
}

