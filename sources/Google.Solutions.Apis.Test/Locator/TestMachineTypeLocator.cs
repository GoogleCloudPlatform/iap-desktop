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
using System;

namespace Google.Solutions.Apis.Test.Locator
{
    [TestFixture]
    public class TestMachineTypeLocator
        : TestLocatorFixtureBase<MachineTypeLocator>
    {
        protected override MachineTypeLocator CreateInstance()
        {
            return new MachineTypeLocator("project-1", "zone-1", "type-1");
        }

        [Test]
        public void Project()
        {
            var ref1 = new MachineTypeLocator("project-1", "zone-1", "type-1");
            Assert.That(ref1.Project.Name, Is.EqualTo(ref1.ProjectId));
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse_WhenPathIsValid()
        {
            Assert.IsTrue(MachineTypeLocator.TryParse(
                "projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.That(ref1!.ResourceType, Is.EqualTo("machineTypes"));
            Assert.That(ref1.Name, Is.EqualTo("n2d-standard-64"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenQualifiedByComputeGoogleapisHost()
        {
            Assert.IsTrue(MachineTypeLocator.TryParse(
                "https://compute.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.That(ref1!.ResourceType, Is.EqualTo("machineTypes"));
            Assert.That(ref1.Name, Is.EqualTo("n2d-standard-64"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenQualifiedByGoogleapisHost()
        {
            Assert.IsTrue(MachineTypeLocator.TryParse(
                "https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.That(ref1!.ResourceType, Is.EqualTo("machineTypes"));
            Assert.That(ref1.Name, Is.EqualTo("n2d-standard-64"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenUsingBetaApi()
        {
            Assert.IsTrue(MachineTypeLocator.TryParse(
                 "https://compute.googleapis.com/compute/beta/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.That(ref1!.ResourceType, Is.EqualTo("machineTypes"));
            Assert.That(ref1.Name, Is.EqualTo("n2d-standard-64"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenPathLacksProject()
        {
            Assert.IsFalse(MachineTypeLocator.TryParse(
                "project-1/zones/us-central1-a/machineTypes/n2d-standard-64",
                out var _));
        }

        [Test]
        public void TryParse_WhenPathInvalid()
        {
            Assert.IsFalse(MachineTypeLocator.TryParse(
                "project-1/zones/us-central1-a/machineTypes/",
                out var _));
            Assert.IsFalse(MachineTypeLocator.TryParse(
                "project-1/zones/us-central1-a/machineTypes/ ",
                out var _));
            Assert.IsFalse(MachineTypeLocator.TryParse(
                "/",
                out var _));
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenPathIsValid()
        {
            var ref1 = MachineTypeLocator.Parse(
                "projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");

            Assert.That(ref1.ResourceType, Is.EqualTo("machineTypes"));
            Assert.That(ref1.Name, Is.EqualTo("n2d-standard-64"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenQualifiedByComputeGoogleapisHost()
        {
            var ref1 = MachineTypeLocator.Parse(
                "https://compute.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");

            Assert.That(ref1.ResourceType, Is.EqualTo("machineTypes"));
            Assert.That(ref1.Name, Is.EqualTo("n2d-standard-64"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenQualifiedByGoogleapisHost()
        {
            var ref1 = MachineTypeLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");

            Assert.That(ref1.ResourceType, Is.EqualTo("machineTypes"));
            Assert.That(ref1.Name, Is.EqualTo("n2d-standard-64"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenUsingBetaApi()
        {
            var ref1 = MachineTypeLocator.Parse(
                 "https://compute.googleapis.com/compute/beta/projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64");
            Assert.That(ref1.ResourceType, Is.EqualTo("machineTypes"));
            Assert.That(ref1.Name, Is.EqualTo("n2d-standard-64"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenPathLacksProject()
        {
            Assert.Throws<ArgumentException>(() => MachineTypeLocator.Parse(
                "project-1/zones/us-central1-a/machineTypes/n2d-standard-64"));
        }

        [Test]
        public void Parse_WhenPathInvalid()
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
        public void Equals_WhenObjectsNotEquivalent_ThenEqualsReturnsFalse()
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
        public void ToString_WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64";

            Assert.That(
                MachineTypeLocator.Parse(path).ToString(), Is.EqualTo(path));
        }

        [Test]
        public void ToString_WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central1-a/machineTypes/n2d-standard-64";

            Assert.That(
                MachineTypeLocator.Parse(
                    "https://www.googleapis.com/compute/v1/" + path).ToString(), Is.EqualTo(path));
        }
    }
}
