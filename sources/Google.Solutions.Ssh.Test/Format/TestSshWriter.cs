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

namespace Google.Solutions.Ssh.Test.Format
{
    [TestFixture]
    public class TestSshWriter
    {
        [Test]
        public void Byte()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.WriteByte((byte)0x42);
                writer.Flush();

                Assert.That(
                    buffer.ToArray(), Is.EqualTo(new byte[] { 0x42 }));
            }
        }

        [Test]
        public void Boolean()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.WriteBoolean(true);
                writer.WriteBoolean(false);
                writer.Flush();

                Assert.That(
                    buffer.ToArray(), Is.EqualTo(new byte[] { 1, 0 }));
            }
        }

        [Test]
        public void Uint()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.WriteUint32(0xAABBCCDD);
                writer.Flush();

                Assert.That(
                    buffer.ToArray(), Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }));
            }
        }

        [Test]
        public void Ulong()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.WriteUint64(0xAABBCCDD00112233);
                writer.Flush();

                Assert.That(
                    buffer.ToArray(), Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0x00, 0x11, 0x22, 0x33 }));
            }
        }

        [Test]
        public void EmptyString()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.WriteString(string.Empty);
                writer.Flush();

                Assert.That(
                    buffer.ToArray(), Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
            }
        }

        [Test]
        public void NonEmptyString()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.WriteString("a");
                writer.Flush();

                Assert.That(
                    buffer.ToArray(), Is.EqualTo(new byte[] { 0, 0, 0, 1, (byte)'a' }));
            }
        }

        [Test]
        public void ZeroMultiPrecisionInteger()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.WriteMultiPrecisionInteger(new byte[] { 0 });
                writer.Flush();

                Assert.That(
                    buffer.ToArray(), Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
            }
        }

        [Test]
        public void MultiPrecisionIntegerWithLeadingZeroes()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.WriteMultiPrecisionInteger(new byte[] { 0, 0, 0xA, 0xB, 0xC });
                writer.Flush();

                Assert.That(
                    buffer.ToArray(), Is.EqualTo(new byte[] { 0, 0, 0, 3, 0xA, 0xB, 0xC }));
            }
        }

        [Test]
        public void NegativeMultiPrecisionInteger()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.WriteMultiPrecisionInteger(new[] { (byte)0x80 });
                writer.Flush();

                Assert.That(
                    buffer.ToArray(), Is.EqualTo(new byte[] { 0, 0, 0, 2, 0, 0x80 }));
            }
        }
    }
}
