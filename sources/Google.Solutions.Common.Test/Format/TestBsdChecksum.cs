//
// Copyright 2023 Google LLC
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
using NUnit.Framework;
using System;
using System.Text;

namespace Google.Solutions.Common.Test.Format
{
    [TestFixture]
    public class TestBsdChecksum
    {
        [Test]
        public void Value_WhenEnpty([Values(1, 16, 32)] int lengthInBits)
        {
            var checksum = new BsdChecksum((ushort)lengthInBits);
            checksum.Add(Array.Empty<byte>());
            Assert.That(checksum.Value, Is.EqualTo(0));
        }

        [Test]
        public void Value_WhenZeros([Values(1, 16, 32)] int lengthInBits)
        {
            var checksum = new BsdChecksum((ushort)lengthInBits);
            checksum.Add(new byte[] { 0, 0, 0, 0 });
            Assert.That(checksum.Value, Is.EqualTo(0));
        }

        [Test]
        public void Value_WhenSingleChunk()
        {
            var checksum = new BsdChecksum(16);
            checksum.Add(Encoding.ASCII.GetBytes("test\n"));
            Assert.That(checksum.Value, Is.EqualTo(41076));
        }

        [Test]
        public void Value_WhenMultipleChunks()
        {
            var checksum = new BsdChecksum(16);
            checksum.Add(Encoding.ASCII.GetBytes("Test"));
            checksum.Add(Encoding.ASCII.GetBytes("data\n"));
            Assert.That(checksum.Value, Is.EqualTo(27248));
        }
    }
}
