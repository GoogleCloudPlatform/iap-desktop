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
    public class TestSshRelayFormat
    {
        //---------------------------------------------------------------------
        // Tag.
        //---------------------------------------------------------------------

        [TestFixture]
        public class Tag : IapFixtureBase
        {
            [Test]
            public void WhenBufferSufficient_ThenEncodeSucceeds()
            {
                var message = new byte[2];

                var bytesWritten = SshRelayFormat.Tag.Encode(message, MessageTag.ACK);

                Assert.AreEqual(2, bytesWritten);
                CollectionAssert.AreEquivalent(
                    new byte[]
                    {
                    0, 7
                    },
                    message);
            }

            [Test]
            public void WhenBufferTooSmall_ThenEncodeThrowsException()
            {
                var message = new byte[1];

                Assert.Throws<ArgumentException>(
                    () => SshRelayFormat.Tag.Encode(message, MessageTag.ACK));
            }

            [Test]
            public void WhenMessageComplete_ThenDecodeSucceeds()
            {
                var message = new byte[] { 0, 4, 99, 99, 99, 99, 99 };

                var bytesRead = SshRelayFormat.Tag.Decode(message, out var tag);

                Assert.AreEqual(2, bytesRead);
                Assert.AreEqual(MessageTag.DATA, tag);
            }

            [Test]
            public void WhenMessageTruncated_ThenDecodeThrowsException()
            {
                var message = new byte[] { 0 };

                Assert.Throws<ArgumentException>(
                    () => SshRelayFormat.Tag.Decode(message, out var tag));
            }
        }

        //---------------------------------------------------------------------
        // ConnectSuccessSid.
        //---------------------------------------------------------------------

        [TestFixture]
        public class ConnectSuccessSid : IapFixtureBase
        {
            [Test]
            public void WhenMessageComplete_ThenDecodeReturnsSid()
            {
                var message = new byte[] {
                    0, 1,
                    0, 0, 0, 3,
                    (byte)'S', (byte)'i', (byte)'d'
                };

                var bytesRead = SshRelayFormat.ConnectSuccessSid.Decode(message, out var sid);

                Assert.AreEqual(9, bytesRead);
                Assert.AreEqual("Sid", sid);
            }

            [Test]
            public void WhenMessageTruncated_ThenDecodeThrowsException()
            {
                var message = new byte[] {
                    0, 1,
                    0, 0, 0, 4,
                    (byte)'S', (byte)'i', (byte)'d'
                };

                Assert.Throws<ArgumentException>(
                    () => SshRelayFormat.ConnectSuccessSid.Decode(message, out var sid));
            }
        }

        //---------------------------------------------------------------------
        // ReconnectAck.
        //---------------------------------------------------------------------

        [TestFixture]
        public class ReconnectAck : IapFixtureBase
        {
            [Test]
            public void WhenMessageComplete_ThenDecodeReturnsAck()
            {
                var message = new byte[] {
                    0, 2,
                    0, 0, 0, 0, 0, 0, 0, 3
                };

                var bytesRead = SshRelayFormat.ReconnectAck.Decode(message, out var ack);

                Assert.AreEqual(10, bytesRead);
                Assert.AreEqual(3, ack);
            }

            [Test]
            public void WhenMessageTruncated_ThenDecodeThrowsException()
            {
                var message = new byte[] {
                    0, 1,
                    1, 2, 3, 4, 5, 6, 7
                };

                Assert.Throws<ArgumentException>(
                    () => SshRelayFormat.ReconnectAck.Decode(message, out var ack));
            }
        }

        //---------------------------------------------------------------------
        // Ack.
        //---------------------------------------------------------------------

        [TestFixture]
        public class Ack : IapFixtureBase
        {
            [Test]
            public void WhenBufferSufficient_ThenEncodeSucceeds()
            {
                var message = new byte[10];

                var bytesWritten = SshRelayFormat.Ack.Encode(message, 77);

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
            public void WhenBufferTooSmall_ThenEncodeThrowsException()
            {
                var message = new byte[9];

                Assert.Throws<ArgumentException>(
                    () => SshRelayFormat.Ack.Encode(message, 77));
            }

            [Test]
            public void WhenMessageComplete_ThenDecodeReturnsAck()
            {
                var message = new byte[] {
                    0, 7,
                    0, 0, 0, 0, 0, 0, 0, 3
                };

                var bytesRead = SshRelayFormat.Ack.Decode(message, out var ack);

                Assert.AreEqual(10, bytesRead);
                Assert.AreEqual(3, ack);
            }

            [Test]
            public void WhenMessageTruncated_ThenDecodeThrowsException()
            {
                var message = new byte[] {
                    0, 7,
                    1, 2, 3, 4, 5, 6, 7
                };

                Assert.Throws<ArgumentException>(
                    () => SshRelayFormat.Ack.Decode(message, out var ack));
            }
        }

        //---------------------------------------------------------------------
        // Data.
        //---------------------------------------------------------------------

        [TestFixture]
        public class Data : IapFixtureBase
        {

            [Test]
            public void WhenDataIsEmpty_ThenEncodeThrowsException()
            {
                var message = new byte[SshRelayFormat.MaxDataPayloadLength];
                var data = new byte[0];

                Assert.Throws<ArgumentException>(
                    () => SshRelayFormat.Data.Encode(message, data, 0, (uint)data.Length));
            }

            [Test]
            public void WhenDataTooLarge_ThenEncodeThrowsException()
            {
                var message = new byte[SshRelayFormat.MaxDataPayloadLength + 1];
                var data = new byte[SshRelayFormat.MaxDataPayloadLength + 1];

                Assert.Throws<ArgumentException>(
                    () => SshRelayFormat.Data.Encode(message, data, 0, (uint)data.Length));
            }

            [Test]
            public void WhenBufferTooSmall_ThenEncodeThrowsException()
            {
                var message = new byte[SshRelayFormat.MaxMessageSize - 1];
                var data = new byte[SshRelayFormat.MaxDataPayloadLength];

                Assert.Throws<ArgumentException>(
                    () => SshRelayFormat.Data.Encode(message, data, 0, (uint)data.Length));
            }

            [Test]
            public void WhenDataIsMaxSize_ThenEncodeSucceeds()
            {
                var message = new byte[SshRelayFormat.MaxMessageSize];
                var data = new byte[SshRelayFormat.MaxDataPayloadLength];
                data[0] = (byte)'A';
                data[SshRelayFormat.MaxDataPayloadLength - 1] = (byte)'Z';

                var bytesWritten = SshRelayFormat.Data.Encode(
                    message,
                    data,
                    0,
                    SshRelayFormat.MaxDataPayloadLength);

                Assert.AreEqual(SshRelayFormat.MaxMessageSize, bytesWritten);
                Assert.AreEqual(SshRelayFormat.MaxMessageSize, bytesWritten);
                Assert.AreEqual((byte)'Z', message[SshRelayFormat.MaxMessageSize - 1]);
            }

            [Test]
            public void WhenIndexNotZero_ThenEncodeSucceeds()
            {
                var message = new byte[7];
                var data = new byte[SshRelayFormat.MaxDataPayloadLength + 1];
                data[SshRelayFormat.MaxDataPayloadLength] = (byte)'D';

                var bytesWritten = SshRelayFormat.Data.Encode(
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

            [Test]
            public void WhenMessageComplete_ThenDecodeReturnsData()
            {
                var message = new byte[] {
                    0, 4,
                    0, 0, 0, 1,
                    (byte)'D'
                };

                var data = new byte[1];
                var bytesRead = SshRelayFormat.Data.Decode(message, data, 0, (uint)data.Length);

                Assert.AreEqual(7, bytesRead);
                Assert.AreEqual('D', data[0]);
            }

            [Test]
            public void WhenTargetIndexNotNull_ThenDecodeReturnsData()
            {
                var message = new byte[] {
                    0, 4,
                    0, 0, 0, 1,
                    (byte)'D'
                };

                var data = new byte[2];
                var bytesRead = SshRelayFormat.Data.Decode(message, data, 1, 1);

                Assert.AreEqual(7, bytesRead);
                Assert.AreEqual('D', data[1]);
            }

            [Test]
            public void WhenBufferTooSmall_ThenDecodeThrowsException()
            {
                var message = new byte[] {
                    0, 4,
                    0, 0, 0, 2,
                    (byte)'D'
                };

                var data = new byte[1];

                Assert.Throws<ArgumentException>(
                    () => SshRelayFormat.Data.Decode(message, data, 1, (uint)data.Length));
            }

            [Test]
            public void WhenBufferTooSmallForData_ThenDecodeThrowsException()
            {
                var message = new byte[] {
                    0, 4,
                    0, 0, 0, 2,
                    (byte)'D', (byte) 'a'
                };

                var data = new byte[1];

                Assert.Throws<IndexOutOfRangeException>(
                    () => SshRelayFormat.Data.Decode(message, data, 0, (uint)data.Length));
            }
        }
    }
}
