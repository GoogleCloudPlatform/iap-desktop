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

using Google.Solutions.Ssh.Native;
using NUnit.Framework;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    public class TestEnvironmentVariable : SshFixtureBase
    {
        //----------------------------------------------------------------------
        // Equals.
        //----------------------------------------------------------------------

        [Test]
        public void Equals_WhenVariablesAreEquivalent_ThenEqualsReturnsTrue()
        {
            var ref1 = new EnvironmentVariable("NAME", "value", false);
            var ref2 = new EnvironmentVariable("NAME", "value", false);

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void Equals_WhenVariablesAreSame_ThenEqualsReturnsTrue()
        {
            var ref1 = new EnvironmentVariable("NAME", "value", true);
            var ref2 = ref1;

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void Equals_WhenVariablesAreNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new EnvironmentVariable("NAME", "", true);
            var ref2 = new EnvironmentVariable("NAME", "", false);

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        [Test]
        public void Equals_estEqualsNull()
        {
            var ref1 = new EnvironmentVariable("NAME", "value", false);

            Assert.IsFalse(ref1.Equals(null!));
            Assert.IsFalse(ref1.Equals((object)null!));
        }
    }
}
