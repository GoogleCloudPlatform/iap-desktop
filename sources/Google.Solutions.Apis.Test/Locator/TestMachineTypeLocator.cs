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
using Google.Solutions.Common.Test;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System;

namespace Google.Solutions.Apis.Test.Locator
{
    [TestFixture]
    public class TestMachineTypeLocator
        : EquatableFixtureBase<MachineTypeLocator, MachineTypeLocator>
    {
        protected override MachineTypeLocator CreateInstance()
        {
            return new MachineTypeLocator("project-1", "zone-1", "type-1");
        }

        [Test]
        public void Project()
        {
            var ref1 = new MachineTypeLocator("project-1", "zone-1", "type-1");
            Assert.AreEqual(ref1.ProjectId, ref1.Project.Name);
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPathIsValid_ParseReturnsObject()
        {
            var ref1 = MachineTypeLocator.Parse(
                "projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");

            Assert.AreEqual("machineTypes", ref1.ResourceType);
            Assert.AreEqual("n2d-standard-64", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenQualifiedByComputeGoogleapisHost_ParseReturnsObject()
        {
            var ref1 = MachineTypeLocator.Parse(
                "https://compute.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");

            Assert.AreEqual("machineTypes", ref1.ResourceType);
            Assert.AreEqual("n2d-standard-64", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenQualifiedByGoogleapisHost_ParseReturnsObject()
        {
            var ref1 = MachineTypeLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");

            Assert.AreEqual("machineTypes", ref1.ResourceType);
            Assert.AreEqual("n2d-standard-64", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenUsingBetaApi_ParseReturnsObject()
        {
            var ref1 = MachineTypeLocator.Parse(
                 "https://compute.googleapis.com/compute/beta/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");
            Assert.AreEqual("machineTypes", ref1.ResourceType);
            Assert.AreEqual("n2d-standard-64", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenPathLacksProject_ParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => MachineTypeLocator.Parse(
                "project-1/zones/us-central1-a/machineTypes/n2d-standard-64"));
        }

        [Test]
        public void WhenPathInvalid_ParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => MachineTypeLocator.Parse(
                "project-1/zones/us-central1-a/machineTypes/"));
            Assert.Throws<ArgumentException>(() => MachineTypeLocator.Parse(
                "project-1/zones/us-central1-a/machineTypes/ "));
            Assert.Throws<ArgumentException>(() => MachineTypeLocator.Parse(
                "/"));
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        [Test]
        public void WhenObjectsNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new MachineTypeLocator("proj", "zone1", "n2d-standard-64");
            var ref2 = new MachineTypeLocator("proj", "zone2", "n2d-standard-64");

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object?)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64";

            Assert.AreEqual(
                path,
                MachineTypeLocator.Parse(path).ToString());
        }

        [Test]
        public void WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64";

            Assert.AreEqual(
                path,
                MachineTypeLocator.Parse(
                    "https://www.googleapis.com/compute/v1/" + path).ToString());
        }
    }
}
