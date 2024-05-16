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
    public class TestDiskTypeLocator
        : EquatableFixtureBase<DiskTypeLocator, DiskTypeLocator>
    {
        protected override DiskTypeLocator CreateInstance()
        {
            return new DiskTypeLocator("project-1", "zone-1", "type-1");
        }

        [Test]
        public void Project()
        {
            var ref1 = new DiskTypeLocator("project-1", "zone-1", "type-1");
            Assert.AreEqual(ref1.ProjectId, ref1.Project.Name);
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPathIsValid_ParseReturnsObject()
        {
            var ref1 = DiskTypeLocator.Parse(
                "projects/project-1/zones/us-central1-a/diskTypes/pd-standard");

            Assert.AreEqual("diskTypes", ref1.ResourceType);
            Assert.AreEqual("pd-standard", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenQualifiedByComputeGoogleapisHost_ParseReturnsObject()
        {
            var ref1 = DiskTypeLocator.Parse(
                "https://compute.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/diskTypes/pd-standard");

            Assert.AreEqual("diskTypes", ref1.ResourceType);
            Assert.AreEqual("pd-standard", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenQualifiedByGoogleapisHost_ParseReturnsObject()
        {
            var ref1 = DiskTypeLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/diskTypes/pd-standard");

            Assert.AreEqual("diskTypes", ref1.ResourceType);
            Assert.AreEqual("pd-standard", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenUsingBetaApi_ParseReturnsObject()
        {
            var ref1 = DiskTypeLocator.Parse(
                 "https://compute.googleapis.com/compute/beta/projects/project-1/zones/us-central1-a/diskTypes/pd-standard");
            Assert.AreEqual("diskTypes", ref1.ResourceType);
            Assert.AreEqual("pd-standard", ref1.Name);
            Assert.AreEqual("us-central1-a", ref1.Zone);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenPathLacksProject_ParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => DiskTypeLocator.Parse(
                "/project-1/zones/us-central1-a/diskTypes/pd-standard"));
        }

        [Test]
        public void WhenPathInvalid_ParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => DiskTypeLocator.Parse(
                "/project-1/zones/us-central1-a/diskTypes"));
            Assert.Throws<ArgumentException>(() => DiskTypeLocator.Parse(
                "/project-1/zones/us-central1-a/diskTypes/pd-standard"));
            Assert.Throws<ArgumentException>(() => DiskTypeLocator.Parse(
                "/"));
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        [Test]
        public void WhenObjectsNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new DiskTypeLocator("proj", "zone1", "pd-standard");
            var ref2 = new DiskTypeLocator("proj", "zone2", "pd-standard");

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
            var path = "projects/project-1/zones/us-central1-a/diskTypes/pd-standard";

            Assert.AreEqual(
                path,
                DiskTypeLocator.Parse(path).ToString());
        }

        [Test]
        public void WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central1-a/diskTypes/pd-standard";

            Assert.AreEqual(
                path,
                DiskTypeLocator.Parse(
                    "https://www.googleapis.com/compute/v1/" + path).ToString());
        }
    }
}
