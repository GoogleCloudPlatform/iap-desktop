﻿//
// Copyright 2020 Google LLC
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

using NUnit.Framework;
using System.Text;

namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    public class TestStreamingDecoder : SshFixtureBase
    {
        private StringBuilder output;
        private StreamingDecoder decoder;

        [SetUp]
        public void SetUp()
        {
            this.output = new StringBuilder();
            this.decoder = new StreamingDecoder(
                Encoding.UTF8,
                s => output.Append(s));
        }

        [Test]
        public void WhenInputContainsOneByteUtf8Sequence_ThenInputIsDecoded()
        {
            this.decoder.Decode(new byte[] { 0x24, 0x24 });
            Assert.AreEqual("$$", this.output.ToString());
        }


        [Test]
        public void WhenInputContainsTwoByteUtf8Sequence_ThenInputIsDecoded()
        {
            this.decoder.Decode(new byte[] { 0xC2, 0xA2 });
            Assert.AreEqual("\u00A2", this.output.ToString());
            this.output.Clear();
        }

        [Test]
        public void WhenInputContainsThreeByteUtf8Sequence_ThenInputIsDecoded()
        {
            this.decoder.Decode(new byte[] { 0xE0, 0xA4, 0xB9 });
            Assert.AreEqual("\u0939", this.output.ToString());
        }

        [Test]
        public void WhenInputContainsPartialTwoByteUtf8Sequence_ThenInputIsHeld()
        {
            this.decoder.Decode(new byte[] { 0xC2 });
            Assert.AreEqual("", this.output.ToString());

            this.decoder.Decode(new byte[] { 0xA2, 0x20 });
            Assert.AreEqual("\u00A2 ", this.output.ToString());
        }

        [Test]
        public void WhenInputContainsPartialThreeByteUtf8Sequence_ThenInputIsHeld()
        {
            this.decoder.Decode(new byte[] { 0xE0, 0xA4 });
            Assert.AreEqual("", this.output.ToString());

            this.decoder.Decode(new byte[] { 0xB9, 0x20 });
            Assert.AreEqual("\u0939 ", this.output.ToString());
        }
    }
}
