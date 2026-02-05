//
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

using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Text;

namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    [UsesCloudResources]
    public class TestStreamingDecoder : SshFixtureBase
    {
        [Test]
        public void Decode_WhenInputContainsOneByteUtf8Sequence_ThenInputIsDecoded()
        {
            var decoder = new StreamingDecoder(Encoding.UTF8);
            var s = decoder.Decode(new byte[] { 0x24, 0x24 });
            Assert.That(s, Is.EqualTo("$$"));
        }


        [Test]
        public void Decode_WhenInputContainsTwoByteUtf8Sequence_ThenInputIsDecoded()
        {
            var decoder = new StreamingDecoder(Encoding.UTF8);
            var s = decoder.Decode(new byte[] { 0xC2, 0xA2 });
            Assert.That(s, Is.EqualTo("\u00A2"));
        }

        [Test]
        public void Decode_WhenInputContainsThreeByteUtf8Sequence_ThenInputIsDecoded()
        {
            var decoder = new StreamingDecoder(Encoding.UTF8);
            var s = decoder.Decode(new byte[] { 0xE0, 0xA4, 0xB9 });
            Assert.That(s, Is.EqualTo("\u0939"));
        }

        [Test]
        public void Decode_WhenInputContainsPartialTwoByteUtf8Sequence_ThenInputIsHeld()
        {
            var decoder = new StreamingDecoder(Encoding.UTF8);

            var s = decoder.Decode(new byte[] { 0xC2 });
            Assert.That(s, Is.EqualTo(""));

            s = decoder.Decode(new byte[] { 0xA2, 0x20 });
            Assert.That(s, Is.EqualTo("\u00A2 "));
        }

        [Test]
        public void Decode_WhenInputContainsPartialThreeByteUtf8Sequence_ThenInputIsHeld()
        {
            var decoder = new StreamingDecoder(Encoding.UTF8);

            var s = decoder.Decode(new byte[] { 0xE0, 0xA4 });
            Assert.That(s, Is.EqualTo(""));

            s = decoder.Decode(new byte[] { 0xB9, 0x20 });
            Assert.That(s, Is.EqualTo("\u0939 "));
        }
    }
}
