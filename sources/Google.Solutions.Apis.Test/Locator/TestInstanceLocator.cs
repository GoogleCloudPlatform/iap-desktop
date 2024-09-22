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

using Google.Solutions.Apis.Locator;
using NUnit.Framework;

namespace Google.Solutions.Apis.Test.Locator
{
    [TestFixture]
    public class TestInstanceLocator
        : TestLocatorFixtureBase<InstanceLocator>
    {
        protected override InstanceLocator CreateInstance()
        {
            return new InstanceLocator("project-1", "zone-1", "instance-1");
        }

        [Test]
        public void Project()
        {
            var ref1 = new InstanceLocator("project-1", "zone-1", "instance-1");
            Assert.AreEqual(ref1.ProjectId, ref1.Project.Name);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central-1/instances/instance-1";

            Assert.AreEqual(
                path,
                InstanceLocator.Parse(path).ToString());
        }

        [Test]
        public void ToString_WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central-1/instances/instance-1";

            Assert.AreEqual(
                path,
                InstanceLocator.Parse(
                    "https://www.googleapis.com/compute/v1/" + path).ToString());
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        [Test]
        public void Equals_WhenObjectsNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new InstanceLocator("proj", "zone", "inst");
            var ref2 = new InstanceLocator("proj", "zone", "other");

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object?)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        [Test]
        public void Equals_WhenReferencesAreOfDifferentType_ThenEqualsReturnsFalse()
        {
            var ref1 = new InstanceLocator("proj", "zone", "inst");
            var ref2 = new DiskTypeLocator("proj", "zone", "pd-standard");

            Assert.IsFalse(ref2.Equals(ref1));
            Assert.IsFalse(ref2.Equals((object?)ref1));
            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object?)ref2));
        }
    }
}
