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

using Google.Solutions.Ssh.Cryptography;
using NUnit.Framework;
using System;

namespace Google.Solutions.Ssh.Test.Cryptography
{
    [TestFixture]
    public class TestPublicKey
    {
        private class SamplePublicKey : PublicKey
        {
            public SamplePublicKey(byte[] value)
            {
                this.WireFormatValue = value;
            }

            public override string Type => "sample";

            public override byte[] WireFormatValue { get; }
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToStringReturnsOpenSshFormat()
        {
            var key = new SamplePublicKey(Convert.FromBase64String("ABCD"));

            Assert.AreEqual("sample ABCD", key.ToString());
        }

        [Test]
        public void ToOpenSshString()
        {
            var key = new SamplePublicKey(Convert.FromBase64String("ABCD"));

            Assert.AreEqual("sample ABCD", key.ToString(PublicKey.Format.OpenSsh));
        }

        [Test]
        public void ToSsh2String()
        {
            var key = new SamplePublicKey(Convert.FromBase64String("ABCD"));

            Assert.AreEqual(
                "---- BEGIN SSH2 PUBLIC KEY ----\r\n" +
                "ABCD\r\n" +
                "---- END SSH2 PUBLIC KEY ----\r\n",
                key.ToString(PublicKey.Format.Ssh2));
        }
    }
}
