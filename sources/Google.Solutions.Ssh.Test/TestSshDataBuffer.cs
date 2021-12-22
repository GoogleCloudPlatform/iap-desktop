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
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    public class TestSshDataBuffer
    {
        [Test]
        public void EmptyString()
        {
            using (var buffer = new SshDataBuffer())
            {
                buffer.WriteString(string.Empty);

                Assert.AreEqual(
                    new byte[] { 0, 0, 0, 0 },
                    buffer.ToArray());
            }
        }

        [Test]
        public void NonEmptyString()
        {
            using (var buffer = new SshDataBuffer())
            {
                buffer.WriteString("a");

                Assert.AreEqual(
                    new byte[] { 0, 0, 0, 1, (byte)'a' },
                    buffer.ToArray());
            }
        }

        [Test]
        public void MpintZero()
        {
            using (var buffer = new SshDataBuffer())
            {
                buffer.WriteMpint(new byte[] { 0 });

                Assert.AreEqual(
                    new byte[] { 0, 0, 0, 0 },
                    buffer.ToArray());
            }
        }

        [Test]
        public void MpintNegative()
        {
            using (var buffer = new SshDataBuffer())
            {
                buffer.WriteMpint(new[] { (byte)0x80 });

                Assert.AreEqual(
                    new byte[] { 0, 0, 0, 2, 0, 0x80 },
                    buffer.ToArray());
            }
        }
    }
}
