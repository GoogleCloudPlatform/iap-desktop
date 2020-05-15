//
// Copyright 2019 Google LLC
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

namespace Google.Solutions.Compute.Test
{
    [TestFixture]
    public class TestVmInstanceReference : FixtureBase
    {
        [Test]
        public void ToStringReturnsName()
        {
            var ref1 = new VmInstanceReference("proj", "zone", "inst");
            Assert.AreEqual("inst", ref1.ToString());
        }

        [Test]
        public void WhenReferencesAreEquivalent_ThenEqualsReturnsTrue()
        {
            var ref1 = new VmInstanceReference("proj", "zone", "inst");
            var ref2 = new VmInstanceReference("proj", "zone", "inst");

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreSame_ThenEqualsReturnsTrue()
        {
            var ref1 = new VmInstanceReference("proj", "zone", "inst");
            var ref2 = ref1;

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new VmInstanceReference("proj", "zone", "inst");
            var ref2 = new VmInstanceReference("proj", "zone", "other");

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreOfDifferentType_ThenEqualsReturnsFalse()
        {
            var ref1 = new VmInstanceReference("proj", "zone", "inst");
            var ref2 = new ResourceReference("proj", "zone", "instances", "inst");

            Assert.IsFalse(ref2.Equals(ref1));
            Assert.IsFalse(ref2.Equals((object)ref1));
            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
        }

        [Test]
        public void TestEqualsNull()
        {
            var ref1 = new VmInstanceReference("proj", "zone", "inst");

            Assert.IsFalse(ref1.Equals(null));
            Assert.IsFalse(ref1.Equals((object)null));
            Assert.IsFalse(ref1 == null);
            Assert.IsFalse(null == ref1);
            Assert.IsTrue(ref1 != null);
            Assert.IsTrue(null != ref1);
        }
    }
}
