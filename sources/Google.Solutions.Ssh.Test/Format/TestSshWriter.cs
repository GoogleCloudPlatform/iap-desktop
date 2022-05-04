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

using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Ssh.Format;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

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
                writer.Write((byte)0x42);
                writer.Flush();

                Assert.AreEqual(
                    new byte[] { 0x42 },
                    buffer.ToArray());
            }
        }

        [Test]
        public void Boolean()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.Write(true);
                writer.Write(false);
                writer.Flush();

                Assert.AreEqual(
                    new byte[] { 1, 0 },
                    buffer.ToArray());
            }
        }

        [Test]
        public void Uint()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.Write(0xAABBCCDD);
                writer.Flush();

                Assert.AreEqual(
                    new byte[] { 0xAA, 0xBB, 0xCC, 0xDD },
                    buffer.ToArray());
            }
        }

        [Test]
        public void Ulong()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.Write(0xAABBCCDD00112233);
                writer.Flush();

                Assert.AreEqual(
                    new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0x00, 0x11, 0x22, 0x33 },
                    buffer.ToArray());
            }
        }

        [Test]
        public void EmptyString()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.Write(string.Empty);
                writer.Flush();

                Assert.AreEqual(
                    new byte[] { 0, 0, 0, 0 },
                    buffer.ToArray());
            }
        }

        [Test]
        public void NonEmptyString()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.Write("a");
                writer.Flush();

                Assert.AreEqual(
                    new byte[] { 0, 0, 0, 1, (byte)'a' },
                    buffer.ToArray());
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

                Assert.AreEqual(
                    new byte[] { 0, 0, 0, 0 },
                    buffer.ToArray());
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

                Assert.AreEqual(
                    new byte[] { 0, 0, 0, 2, 0, 0x80 },
                    buffer.ToArray());
            }
        }
    }
}
