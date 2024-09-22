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
    public class TestZoneLocator
        : TestLocatorFixtureBase<ZoneLocator>
    {
        protected override ZoneLocator CreateInstance()
        {
            return new ZoneLocator("project-1", "zone-1");
        }

        [Test]
        public void Project()
        {
            var ref1 = new ZoneLocator("project-1", "zone-1");
            Assert.AreEqual(ref1.ProjectId, ref1.Project.Name);
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenPathIsValid_ParseReturnsObject()
        {
            var ref1 = ZoneLocator.Parse(
                "projects/project-1/zones/us-central1-a");

            Assert.AreEqual("zones", ref1.ResourceType);
            Assert.AreEqual("us-central1-a", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void Parse_WhenQualifiedByComputeGoogleapisHost_ParseReturnsObject()
        {
            var ref1 = ZoneLocator.Parse(
                "https://compute.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a");

            Assert.AreEqual("zones", ref1.ResourceType);
            Assert.AreEqual("us-central1-a", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void Parse_WhenQualifiedByGoogleapisHost_ParseReturnsObject()
        {
            var ref1 = ZoneLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a");

            Assert.AreEqual("zones", ref1.ResourceType);
            Assert.AreEqual("us-central1-a", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void Parse_WhenPathLacksProject_ParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ZoneLocator.Parse(
                "/project-1/project-1/zones/us-central1-a"));
        }

        [Test]
        public void Parse_WhenPathInvalid_ParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ZoneLocator.Parse(
                "projects/project-1/zone/us-central1-a"));
            Assert.Throws<ArgumentException>(() => ZoneLocator.Parse(
                "projects/project-1/zones"));
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        [Test]
        public void Equals_WhenObjectsNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new ZoneLocator("proj-1", "us-central1-a");
            var ref2 = new ZoneLocator("proj-2", "us-central1-a");

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central1-a";

            Assert.AreEqual(
                path,
                ZoneLocator.Parse(path).ToString());
        }

        [Test]
        public void ToString_WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central1-a";

            Assert.AreEqual(
                path,
                ZoneLocator.Parse(
                    "https://www.googleapis.com/compute/v1/" + path).ToString());
        }
    }
}
