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

using Google.Solutions.IapTunneling.Iap;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    [TestFixture]
    public class TestSshRelayFormat : IapFixtureBase
    {
        //---------------------------------------------------------------------
        // DecodeTag.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMessageComplete_ThenDecodeTagReturnsTag()
        {
            var message = new byte[] { 0, 4, 99, 99, 99, 99, 99 };

            var bytesRead = SshRelayFormat.DecodeTag(message, out var tag);

            Assert.AreEqual(2, bytesRead);
            Assert.AreEqual(MessageTag.DATA, tag);
        }

        [Test]
        public void WhenMessageTruncated_ThenDecodeTagThrowsException()
        {
            var message = new byte[] { 0 };

            Assert.Throws<ArgumentException>(
                () => SshRelayFormat.DecodeTag(message, out var tag));
        }

        //---------------------------------------------------------------------
        // DecodeConnectSuccessSid.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMessageComplete_ThenDecodeConnectSuccessSidReturnsSid()
        {
            var message = new byte[] {
                0, 1, 
                0, 0, 0, 3,
                (byte)'S', (byte)'i', (byte)'d'
            };

            var bytesRead = SshRelayFormat.DecodeConnectSuccessSid(message, out var sid);

            Assert.AreEqual(9, bytesRead);
            Assert.AreEqual("Sid", sid);
        }

        [Test]
        public void WhenMessageTruncated_ThenDecodeConnectSuccessSidThrowsException()
        {
            var message = new byte[] {
                0, 1,
                0, 0, 0, 4,
                (byte)'S', (byte)'i', (byte)'d'
            };

            Assert.Throws<ArgumentException>(
                () => SshRelayFormat.DecodeConnectSuccessSid(message, out var sid));
        }

        //---------------------------------------------------------------------
        // DecodeReconnectAck.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMessageComplete_ThenDecodeReconnectAckReturnsAck()
        {
            var message = new byte[] {
                0, 2,
                0, 0, 0, 0, 0, 0, 0, 3
            };

            var bytesRead = SshRelayFormat.DecodeReconnectAck(message, out var ack);

            Assert.AreEqual(10, bytesRead);
            Assert.AreEqual(3, ack);
        }

        [Test]
        public void WhenMessageTruncated_ThenDecodeReconnectAckThrowsException()
        {
            var message = new byte[] {
                0, 1,
                1, 2, 3, 4, 5, 6, 7
            };

            Assert.Throws<ArgumentException>(
                () => SshRelayFormat.DecodeReconnectAck(message, out var ack));
        }

        //---------------------------------------------------------------------
        // DecodeReconnectAck.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMessageComplete_ThenDecodeAckReturnsAck()
        {
            var message = new byte[] {
                0, 7,
                0, 0, 0, 0, 0, 0, 0, 3
            };

            var bytesRead = SshRelayFormat.DecodeAck(message, out var ack);

            Assert.AreEqual(10, bytesRead);
            Assert.AreEqual(3, ack);
        }

        [Test]
        public void WhenMessageTruncated_ThenDecodeAckThrowsException()
        {
            var message = new byte[] {
                0, 7,
                1, 2, 3, 4, 5, 6, 7
            };

            Assert.Throws<ArgumentException>(
                () => SshRelayFormat.DecodeAck(message, out var ack));
        }

        //---------------------------------------------------------------------
        // DecodeData.
        //---------------------------------------------------------------------

        [Test]
        public void WhenMessageComplete_ThenDecodeDataReturnsData()
        {
            var message = new byte[] {
                0, 4,
                0, 0, 0, 1, 
                (byte)'D'
            };

            var data = new byte[1];
            var bytesRead = SshRelayFormat.DecodeData(message, data, 0, (uint)data.Length);

            Assert.AreEqual(7, bytesRead);
            Assert.AreEqual('D', data[0]);
        }

        [Test]
        public void WhenTargetIndexNotNull_ThenDecodeDataReturnsData()
        {
            var message = new byte[] {
                0, 4,
                0, 0, 0, 1,
                (byte)'D'
            };

            var data = new byte[2];
            var bytesRead = SshRelayFormat.DecodeData(message, data, 1, 1);

            Assert.AreEqual(7, bytesRead);
            Assert.AreEqual('D', data[1]);
        }

        [Test]
        public void WhenBufferTooSmall_ThenDecodeDataThrowsException()
        {
            var message = new byte[] {
                0, 4,
                0, 0, 0, 2,
                (byte)'D'
            };

            var data = new byte[1];

            Assert.Throws<ArgumentException>(
                () => SshRelayFormat.DecodeData(message, data, 1, (uint)data.Length));
        }

        [Test]
        public void WhenBufferTooSmallForData_ThenDecodeDataThrowsException()
        {
            var message = new byte[] {
                0, 4,
                0, 0, 0, 2,
                (byte)'D', (byte) 'a'
            };

            var data = new byte[1];

            Assert.Throws<IndexOutOfRangeException>(
                () => SshRelayFormat.DecodeData(message, data, 0, (uint)data.Length));
        }

        //---------------------------------------------------------------------
        // EncodeAck.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBufferSufficient_ThenEncodeAckSucceeds()
        {
            var message = new byte[10];

            var bytesWritten = SshRelayFormat.EncodeAck(message, 77);

            Assert.AreEqual(10, bytesWritten);
            CollectionAssert.AreEquivalent(
                new byte[]
                {
                    0, 7,
                    0, 0, 0, 0, 0, 0, 0, 77
                },
                message);
        }

        [Test]
        public void WhenBufferTooSmall_ThenEncodeAckThrowsException()
        {
            var message = new byte[9];

            Assert.Throws<ArgumentException>(
                () => SshRelayFormat.EncodeAck(message, 77));
        }

        //---------------------------------------------------------------------
        // EncodeData.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDataIsEmpty_ThenEncodeDataThrowsException()
        {
            var message = new byte[SshRelayFormat.MaxDataPayloadLength];
            var data = new byte[0];

            Assert.Throws<ArgumentException>(
                () => SshRelayFormat.EncodeData(message, data, 0, (uint)data.Length));
        }

        [Test]
        public void WhenDataTooLarge_ThenEncodeDataThrowsException()
        {
            var message = new byte[SshRelayFormat.MaxDataPayloadLength + 1];
            var data = new byte[SshRelayFormat.MaxDataPayloadLength + 1];

            Assert.Throws<ArgumentException>(
                () => SshRelayFormat.EncodeData(message, data, 0, (uint)data.Length));
        }

        [Test]
        public void WhenBufferTooSmall_ThenEncodeDataThrowsException()
        {
            var message = new byte[SshRelayFormat.MaxMessageSize - 1];
            var data = new byte[SshRelayFormat.MaxDataPayloadLength];

            Assert.Throws<ArgumentException>(
                () => SshRelayFormat.EncodeData(message, data, 0, (uint)data.Length));
        }

        [Test]
        public void WhenDataIsMaxSize_ThenEncodeDataSucceeds()
        {
            var message = new byte[SshRelayFormat.MaxMessageSize];
            var data = new byte[SshRelayFormat.MaxDataPayloadLength];
            data[0] = (byte)'A';
            data[SshRelayFormat.MaxDataPayloadLength - 1] = (byte)'Z';

            var bytesWritten = SshRelayFormat.EncodeData(
                message, 
                data,
                0,
                SshRelayFormat.MaxDataPayloadLength);

            Assert.AreEqual(SshRelayFormat.MaxMessageSize, bytesWritten);
            Assert.AreEqual(SshRelayFormat.MaxMessageSize, bytesWritten);
            Assert.AreEqual((byte)'Z', message[SshRelayFormat.MaxMessageSize - 1]);
        }

        [Test]
        public void WhenIndexNotZero_ThenEncodeDataSucceeds()
        {
            var message = new byte[7];
            var data = new byte[SshRelayFormat.MaxDataPayloadLength + 1];
            data[SshRelayFormat.MaxDataPayloadLength] = (byte)'D';

            var bytesWritten = SshRelayFormat.EncodeData(
                message,
                data,
                SshRelayFormat.MaxDataPayloadLength,
                1);

            Assert.AreEqual(7, bytesWritten);
            CollectionAssert.AreEquivalent(
                new byte[]
                {
                    0, 4,
                    0, 0, 0, 1,
                    (byte)'D'
                },
                message);
        }
    }
}
