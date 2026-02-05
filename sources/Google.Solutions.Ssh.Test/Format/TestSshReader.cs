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

using Google.Solutions.Ssh.Format;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace Google.Solutions.Ssh.Test.Format
{
    [TestFixture]
    public class TestSshReader
    {
        [Test]
        public void Byte()
        {
            var data = new byte[] { 0xA, 0xB };
            using (var stream = new MemoryStream(data))
            {
                var reader = new SshReader(stream);

                Assert.That(reader.ReadByte(), Is.EqualTo((byte)0xA));
                Assert.That(reader.ReadByte(), Is.EqualTo((byte)0xB));
            }
        }

        [Test]
        public void Boolean()
        {
            var data = new byte[] { 1, 0 };
            using (var stream = new MemoryStream(data))
            {
                var reader = new SshReader(stream);

                Assert.That(reader.ReadBoolean(), Is.True);
                Assert.That(reader.ReadBoolean(), Is.False);
            }
        }

        [Test]
        public void Uint()
        {
            var data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
            using (var stream = new MemoryStream(data))
            {
                var reader = new SshReader(stream);

                Assert.That(reader.ReadUint32(), Is.EqualTo(0xAABBCCDD));
            }
        }

        [Test]
        public void Ulong()
        {
            var data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0x00, 0x11, 0x22, 0x33 };
            using (var stream = new MemoryStream(data))
            {
                var reader = new SshReader(stream);

                Assert.That(reader.ReadUint64(), Is.EqualTo(0xAABBCCDD00112233));
            }
        }

        [Test]
        public void EmptyString()
        {
            var data = new byte[] { 0, 0, 0, 0 };
            using (var stream = new MemoryStream(data))
            {
                var reader = new SshReader(stream);

                Assert.That(reader.ReadString(), Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public void NonEmptyString()
        {
            var data = new byte[] { 0, 0, 0, 1, (byte)'a' };
            using (var stream = new MemoryStream(data))
            {
                var reader = new SshReader(stream);

                Assert.That(reader.ReadString(), Is.EqualTo("a"));
            }
        }

        [Test]
        public void ZeroMultiPrecisionInteger()
        {
            var data = new byte[] { 0, 0, 0, 0 };
            using (var stream = new MemoryStream(data))
            {
                var reader = new SshReader(stream);

                Assert.That(
                    reader.ReadMultiPrecisionInteger().ToArray(), Is.EqualTo(new byte[] { 0 }));
            }
        }

        [Test]
        public void NegativeMultiPrecisionInteger()
        {
            var data = new byte[] { 0, 0, 0, 2, 0, 0x80 };
            using (var stream = new MemoryStream(data))
            {
                var reader = new SshReader(stream);

                Assert.That(
                    reader.ReadMultiPrecisionInteger().ToArray(), Is.EqualTo(new byte[] { (byte)0x80 }));
            }
        }
    }
}
