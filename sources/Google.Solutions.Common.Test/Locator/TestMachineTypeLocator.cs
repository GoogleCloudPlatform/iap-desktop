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

using Google.Solutions.Common.Locator;
using NUnit.Framework;
using System;

namespace Google.Solutions.Common.Test.Locator
{
    [TestFixture]
    public class TestMachineTypeLocator : FixtureBase
    {

        [Test]
        public void WhenPathIsValid_FromStringReturnsObject()
        {
            var ref1 = MachineTypeLocator.FromString(
                "projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");

            Assert.AreEqual("machineTypes", ref1.ResourceType);
            Assert.AreEqual("n2d-standard-64", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenQualifiedByComputeGoogleapisHost_FromStringReturnsObject()
        {
            var ref1 = MachineTypeLocator.FromString(
                "https://compute.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");

            Assert.AreEqual("machineTypes", ref1.ResourceType);
            Assert.AreEqual("n2d-standard-64", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenQualifiedByGoogleapisHost_FromStringReturnsObject()
        {
            var ref1 = MachineTypeLocator.FromString(
                "https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");

            Assert.AreEqual("machineTypes", ref1.ResourceType);
            Assert.AreEqual("n2d-standard-64", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenUsingBetaApi_FromStringReturnsObject()
        {
            var ref1 = MachineTypeLocator.FromString(
                 "https://compute.googleapis.com/compute/beta/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");
            Assert.AreEqual("machineTypes", ref1.ResourceType);
            Assert.AreEqual("n2d-standard-64", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenPathLacksProject_FromStringThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => MachineTypeLocator.FromString(
                "project-1/zones/us-central1-a/machineTypes/n2d-standard-64"));
        }

        [Test]
        public void WhenPathInvalid_FromStringThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => MachineTypeLocator.FromString(
                "project-1/zones/us-central1-a/machineTypes/"));
            Assert.Throws<ArgumentException>(() => MachineTypeLocator.FromString(
                "project-1/zones/us-central1-a/machineTypes/ "));
            Assert.Throws<ArgumentException>(() => MachineTypeLocator.FromString(
                "/"));
        }

        [Test]
        public void WhenReferencesAreEquivalent_ThenEqualsReturnsTrue()
        {
            var ref1 = new MachineTypeLocator("proj", "zone", "n2d-standard-64");
            var ref2 = new MachineTypeLocator("proj", "zone", "n2d-standard-64");

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreEquivalent_ThenGetHasCodeIsSame()
        {
            var ref1 = new MachineTypeLocator("proj", "zone", "n2d-standard-64");
            var ref2 = new MachineTypeLocator("proj", "zone", "n2d-standard-64");

            Assert.AreEqual(ref1.GetHashCode(), ref2.GetHashCode());
        }

        [Test]
        public void WhenReferencesAreSame_ThenEqualsReturnsTrue()
        {
            var ref1 = new MachineTypeLocator("proj", "zone", "n2d-standard-64");
            var ref2 = ref1;

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new MachineTypeLocator("proj", "zone1", "n2d-standard-64");
            var ref2 = new MachineTypeLocator("proj", "zone2", "n2d-standard-64");

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        [Test]
        public void TestEqualsNull()
        {
            var ref1 = new MachineTypeLocator("proj", "zone", "n2d-standard-64");

            Assert.IsFalse(ref1.Equals(null));
            Assert.IsFalse(ref1.Equals((object)null));
            Assert.IsFalse(ref1 == null);
            Assert.IsFalse(null == ref1);
            Assert.IsTrue(ref1 != null);
            Assert.IsTrue(null != ref1);
        }

        [Test]
        public void WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64";

            Assert.AreEqual(
                path,
                MachineTypeLocator.FromString(path).ToString());
        }

        [Test]
        public void WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64";

            Assert.AreEqual(
                path,
                MachineTypeLocator.FromString(
                    "https://www.googleapis.com/compute/v1/" + path).ToString());
        }
    }
}
