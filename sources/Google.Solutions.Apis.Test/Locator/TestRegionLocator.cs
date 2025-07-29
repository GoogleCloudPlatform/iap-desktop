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
    public class TestRegionLocator
        : TestLocatorFixtureBase<RegionLocator>
    {
        protected override RegionLocator CreateInstance()
        {
            return new RegionLocator("project-1", "region-1");
        }

        [Test]
        public void Project()
        {
            var ref1 = new RegionLocator("project-1", "region-1");
            Assert.AreEqual(ref1.ProjectId, ref1.Project.Name);
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse_WhenPathIsValid()
        {
            Assert.IsTrue(RegionLocator.TryParse(
                "projects/project-1/regions/us-central1",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.AreEqual("regions", ref1!.ResourceType);
            Assert.AreEqual("us-central1", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void TryParse_WhenQualifiedByComputeGoogleapisHost()
        {
            Assert.IsTrue(RegionLocator.TryParse(
                "https://compute.googleapis.com/compute/v1/projects/project-1/regions/us-central1",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.AreEqual("regions", ref1!.ResourceType);
            Assert.AreEqual("us-central1", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void TryParse_WhenQualifiedByGoogleapisHost()
        {
            Assert.IsTrue(RegionLocator.TryParse(
                "https://www.googleapis.com/compute/v1/projects/project-1/regions/us-central1",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.AreEqual("regions", ref1!.ResourceType);
            Assert.AreEqual("us-central1", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void TryParse_WhenPathLacksProject()
        {
            Assert.IsFalse(RegionLocator.TryParse(
                "/project-1/project-1/regions/us-central1",
                out var _));
        }

        [Test]
        public void TryParse_WhenPathInvalid()
        {
            Assert.IsFalse(RegionLocator.TryParse(
                "projects/project-1/region/us-central1",
                out var _));
            Assert.IsFalse(RegionLocator.TryParse(
                "projects/project-1/regions",
                out var _));
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenPathIsValid()
        {
            var ref1 = RegionLocator.Parse(
                "projects/project-1/regions/us-central1");

            Assert.AreEqual("regions", ref1.ResourceType);
            Assert.AreEqual("us-central1", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void Parse_WhenQualifiedByComputeGoogleapisHost()
        {
            var ref1 = RegionLocator.Parse(
                "https://compute.googleapis.com/compute/v1/projects/project-1/regions/us-central1");

            Assert.AreEqual("regions", ref1.ResourceType);
            Assert.AreEqual("us-central1", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void Parse_WhenQualifiedByGoogleapisHost()
        {
            var ref1 = RegionLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/project-1/regions/us-central1");

            Assert.AreEqual("regions", ref1.ResourceType);
            Assert.AreEqual("us-central1", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void Parse_WhenPathLacksProject()
        {
            Assert.Throws<ArgumentException>(() => RegionLocator.Parse(
                "/project-1/project-1/regions/us-central1"));
        }

        [Test]
        public void Parse_WhenPathInvalid()
        {
            Assert.Throws<ArgumentException>(() => RegionLocator.Parse(
                "projects/project-1/region/us-central1"));
            Assert.Throws<ArgumentException>(() => RegionLocator.Parse(
                "projects/project-1/regions"));
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        [Test]
        public void Equals_WhenObjectsNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new RegionLocator("proj-1", "us-central1");
            var ref2 = new RegionLocator("proj-2", "us-central1");

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
            var path = "projects/project-1/regions/us-central1";

            Assert.AreEqual(
                path,
                RegionLocator.Parse(path).ToString());
        }

        [Test]
        public void ToString_WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/regions/us-central1";

            Assert.AreEqual(
                path,
                RegionLocator.Parse(
                    "https://www.googleapis.com/compute/v1/" + path).ToString());
        }
    }
}
