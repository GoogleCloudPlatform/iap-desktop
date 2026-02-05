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
            public SamplePublicKey(string type, byte[] value)
            {
                this.Type = type;
                this.WireFormatValue = value;
            }

            public override string Type { get; }

            public override byte[] WireFormatValue { get; }
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_ReturnsOpenSshFormat()
        {
            using (var key = new SamplePublicKey("sample", Convert.FromBase64String("ABCD")))
            {
                Assert.That(key.ToString(), Is.EqualTo("sample ABCD"));
            }
        }

        [Test]
        public void ToString_WhenFormatIsOpenSsh()
        {
            using (var key = new SamplePublicKey("sample", Convert.FromBase64String("ABCD")))
            {
                Assert.That(key.ToString(PublicKey.Format.OpenSsh), Is.EqualTo("sample ABCD"));
            }
        }

        [Test]
        public void ToString_WhenFormatIsSsh2()
        {
            using (var key = new SamplePublicKey("sample", Convert.FromBase64String("ABCD")))
            {
                Assert.That(
                    key.ToString(PublicKey.Format.Ssh2), Is.EqualTo("---- BEGIN SSH2 PUBLIC KEY ----\r\n" +
                    "ABCD\r\n" +
                    "---- END SSH2 PUBLIC KEY ----\r\n"));
            }
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void Equals_WhenTypesDifferent_ThenEqualsReturnsFalse()
        {
            using (var lhs = new SamplePublicKey("type1", Convert.FromBase64String("ABCD")))
            using (var rhs = new SamplePublicKey("type2", Convert.FromBase64String("ABCD")))
            {
                Assert.That(lhs.Equals(rhs), Is.False);
                Assert.That(lhs.Equals((object)rhs), Is.False);
            }
        }

        [Test]
        public void Equals_WhenWireFormatDifferent_ThenEqualsReturnsFalse()
        {
            using (var lhs = new SamplePublicKey("type", Convert.FromBase64String("ABCD")))
            using (var rhs = new SamplePublicKey("type", Convert.FromBase64String("ABCE")))
            {
                Assert.That(lhs.Equals(rhs), Is.False);
                Assert.That(lhs.Equals((object)rhs), Is.False);
            }
        }

        [Test]
        public void Equals_WhenTypesAndWireFormatEqual_ThenEqualsReturnsTrue()
        {
            using (var lhs = new SamplePublicKey("type", Convert.FromBase64String("ABCD")))
            using (var rhs = new SamplePublicKey("type", Convert.FromBase64String("ABCD")))
            {
                Assert.IsTrue(lhs.Equals(rhs));
                Assert.IsTrue(lhs.Equals((object)rhs));
            }
        }

        //---------------------------------------------------------------------
        // GetHashCode.
        //---------------------------------------------------------------------

        [Test]
        public void GetHashCode_WhenWireFormatDifferent_ThenHashCodeIsDifferent()
        {
            using (var lhs = new SamplePublicKey("type", Convert.FromBase64String("ABCD")))
            using (var rhs = new SamplePublicKey("type", Convert.FromBase64String("ABCE")))
            {
                Assert.That(rhs.GetHashCode(), Is.Not.EqualTo(lhs.GetHashCode()));
            }
        }

        [Test]
        public void GetHashCode_WhenTypesAndWireFormatEqual_ThenHashCodeIsEqual()
        {
            using (var lhs = new SamplePublicKey("type", Convert.FromBase64String("ABCD")))
            using (var rhs = new SamplePublicKey("type", Convert.FromBase64String("ABCD")))
            {
                Assert.That(rhs.GetHashCode(), Is.EqualTo(lhs.GetHashCode()));
            }
        }
    }
}
