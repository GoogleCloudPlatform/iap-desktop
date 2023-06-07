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
        public void Enpty([Values(1, 16, 32)] int lengthInBits)
        {
            Assert.AreEqual(
                0,
                BsdChecksum.Create(Array.Empty<byte>(), (ushort)lengthInBits));
        }

        [Test]
        public void Zeros([Values(1, 16, 32)] int lengthInBits)
        {
            Assert.AreEqual(
                0, 
                BsdChecksum.Create(new byte[] { 0, 0, 0, 0 }, (ushort)lengthInBits));
        }

        [Test]
        public void NonZero()
        {
            Assert.AreEqual(
                41076,
                BsdChecksum.Create(Encoding.ASCII.GetBytes("test\n"), 16));
            Assert.AreEqual(
                27248,
                BsdChecksum.Create(Encoding.ASCII.GetBytes("Testdata\n"), 16));
        }
    }
}
