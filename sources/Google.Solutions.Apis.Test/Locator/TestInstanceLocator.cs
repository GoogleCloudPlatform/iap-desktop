﻿//
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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Test;
using NUnit.Framework;

namespace Google.Solutions.Apis.Test.Locator
{
    [TestFixture]
    public class TestInstanceLocator : CommonFixtureBase
    {
        [Test]
        public void WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central-1/instances/instance-1";

            Assert.AreEqual(
                path,
                InstanceLocator.FromString(path).ToString());
        }

        [Test]
        public void WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central-1/instances/instance-1";

            Assert.AreEqual(
                path,
                InstanceLocator.FromString(
                    "https://www.googleapis.com/compute/v1/" + path).ToString());
        }

        [Test]
        public void WhenReferencesAreEquivalent_ThenEqualsReturnsTrue()
        {
            var ref1 = new InstanceLocator("proj", "zone", "inst");
            var ref2 = new InstanceLocator("proj", "zone", "inst");

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreSame_ThenEqualsReturnsTrue()
        {
            var ref1 = new InstanceLocator("proj", "zone", "inst");
            var ref2 = ref1;

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new InstanceLocator("proj", "zone", "inst");
            var ref2 = new InstanceLocator("proj", "zone", "other");

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreOfDifferentType_ThenEqualsReturnsFalse()
        {
            var ref1 = new InstanceLocator("proj", "zone", "inst");
            var ref2 = new DiskTypeLocator("proj", "zone", "pd-standard");

            Assert.IsFalse(ref2.Equals(ref1));
            Assert.IsFalse(ref2.Equals((object)ref1));
            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
        }

        [Test]
        public void TestEqualsNull()
        {
            var ref1 = new InstanceLocator("proj", "zone", "inst");

            Assert.IsFalse(ref1.Equals(null));
            Assert.IsFalse(ref1.Equals((object)null));
            Assert.IsFalse(ref1 == null);
            Assert.IsFalse(null == ref1);
            Assert.IsTrue(ref1 != null);
            Assert.IsTrue(null != ref1);
        }
    }
}
