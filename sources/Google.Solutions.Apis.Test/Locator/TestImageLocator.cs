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
    public class TestImageLocator
        : TestLocatorFixtureBase<ImageLocator>
    {
        protected override ImageLocator CreateInstance()
        {
            return new ImageLocator("project-1", "image-1");
        }

        [Test]
        public void Project()
        {
            var ref1 = new ImageLocator("project-1", "image-1");
            Assert.That(ref1.Project.Name, Is.EqualTo(ref1.ProjectId));
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse_WhenPathIsValid()
        {
            Assert.That(ImageLocator.TryParse(
                "projects/project-1/global/images/image-1",
                out var ref1), Is.True);

            Assert.That(ref1, Is.Not.Null);
            Assert.That(ref1!.ResourceType, Is.EqualTo("images"));
            Assert.That(ref1.Name, Is.EqualTo("image-1"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenResourceNameCotainsSlash()
        {
            Assert.That(ImageLocator.TryParse(
                "projects/debian-cloud/global/images/family/debian-9",
                out var ref1), Is.True);

            Assert.That(ref1, Is.Not.Null);
            Assert.That(ref1!.ResourceType, Is.EqualTo("images"));
            Assert.That(ref1.Name, Is.EqualTo("family/debian-9"));
            Assert.That(ref1.ProjectId, Is.EqualTo("debian-cloud"));
        }

        [Test]
        public void TryParse_WhenQualifiedByComputeGoogleapisHost()
        {
            Assert.That(ImageLocator.TryParse(
                "https://compute.googleapis.com/compute/v1/projects/debian-cloud/global/images/family/debian-9",
                out var ref1), Is.True);

            Assert.That(ref1, Is.Not.Null);
            Assert.That(ref1!.ResourceType, Is.EqualTo("images"));
            Assert.That(ref1.Name, Is.EqualTo("family/debian-9"));
            Assert.That(ref1.ProjectId, Is.EqualTo("debian-cloud"));
        }

        [Test]
        public void TryParse_WhenQualifiedByGoogleapisHost()
        {
            Assert.That(ImageLocator.TryParse(
                "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/images/windows-server-core",
                out var ref1), Is.True);

            Assert.That(ref1, Is.Not.Null);
            Assert.That(ref1!.ResourceType, Is.EqualTo("images"));
            Assert.That(ref1.Name, Is.EqualTo("windows-server-core"));
            Assert.That(ref1.ProjectId, Is.EqualTo("windows-cloud"));
        }

        [Test]
        public void TryParse_WhenUsingBetaApi()
        {
            Assert.That(ImageLocator.TryParse(
                "https://compute.googleapis.com/compute/beta/projects/eip-images/global/images/debian-9-drawfork-v20191004",
                out var ref1), Is.True);

            Assert.That(ref1, Is.Not.Null);
            Assert.That(ref1!.ResourceType, Is.EqualTo("images"));
            Assert.That(ref1.Name, Is.EqualTo("debian-9-drawfork-v20191004"));
            Assert.That(ref1.ProjectId, Is.EqualTo("eip-images"));
        }

        [Test]
        public void TryParse_WhenPathLacksProject()
        {
            Assert.That(ImageLocator.TryParse(
                "/project-1/project-1/global/images/image-1",
                out var _), Is.False);
        }

        [Test]
        public void TryParse_WhenPathInvalid()
        {
            Assert.That(ImageLocator.TryParse(
                "projects/project-1/notglobal/images/image-1",
                out var _), Is.False);
            Assert.That(ImageLocator.TryParse(
                "/project-1/global/images/image-1",
                out var _), Is.False);
            Assert.That(ImageLocator.TryParse(
                "/",
                out var _), Is.False);
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenPathIsValid()
        {
            var ref1 = ImageLocator.Parse(
                "projects/project-1/global/images/image-1");

            Assert.That(ref1.ResourceType, Is.EqualTo("images"));
            Assert.That(ref1.Name, Is.EqualTo("image-1"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenResourceNameCotainsSlash()
        {
            var ref1 = ImageLocator.Parse(
                "projects/debian-cloud/global/images/family/debian-9");

            Assert.That(ref1.ResourceType, Is.EqualTo("images"));
            Assert.That(ref1.Name, Is.EqualTo("family/debian-9"));
            Assert.That(ref1.ProjectId, Is.EqualTo("debian-cloud"));
        }

        [Test]
        public void Parse_WhenQualifiedByComputeGoogleapisHost()
        {
            var ref1 = ImageLocator.Parse(
                "https://compute.googleapis.com/compute/v1/projects/debian-cloud/global/images/family/debian-9");

            Assert.That(ref1.ResourceType, Is.EqualTo("images"));
            Assert.That(ref1.Name, Is.EqualTo("family/debian-9"));
            Assert.That(ref1.ProjectId, Is.EqualTo("debian-cloud"));
        }

        [Test]
        public void Parse_WhenQualifiedByGoogleapisHost()
        {
            var ref1 = ImageLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/images/windows-server-core");

            Assert.That(ref1.ResourceType, Is.EqualTo("images"));
            Assert.That(ref1.Name, Is.EqualTo("windows-server-core"));
            Assert.That(ref1.ProjectId, Is.EqualTo("windows-cloud"));
        }

        [Test]
        public void Parse_WhenUsingBetaApi()
        {
            var ref1 = ImageLocator.Parse(
                "https://compute.googleapis.com/compute/beta/projects/eip-images/global/images/debian-9-drawfork-v20191004");

            Assert.That(ref1.ResourceType, Is.EqualTo("images"));
            Assert.That(ref1.Name, Is.EqualTo("debian-9-drawfork-v20191004"));
            Assert.That(ref1.ProjectId, Is.EqualTo("eip-images"));
        }

        [Test]
        public void Parse_WhenPathLacksProject()
        {
            Assert.Throws<ArgumentException>(() => ImageLocator.Parse(
                "/project-1/project-1/global/images/image-1"));
        }

        [Test]
        public void Parse_WhenPathInvalid()
        {
            Assert.Throws<ArgumentException>(() => ImageLocator.Parse(
                "projects/project-1/notglobal/images/image-1"));
            Assert.Throws<ArgumentException>(() => ImageLocator.Parse(
                "/project-1/global/images/image-1"));
            Assert.Throws<ArgumentException>(() => ImageLocator.Parse(
                "/"));
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        [Test]
        public void Equals_WhenObjectsNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new ImageLocator("proj-1", "image-1");
            var ref2 = new ImageLocator("proj-2", "image-1");

            Assert.That(ref1.Equals(ref2), Is.False);
            Assert.That(ref1.Equals((object?)ref2), Is.False);
            Assert.That(ref1 == ref2, Is.False);
            Assert.That(ref1 != ref2, Is.True);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/global/images/image-1";

            Assert.That(
                ImageLocator.Parse(path).ToString(), Is.EqualTo(path));
        }

        [Test]
        public void ToString_WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/global/images/image-1";

            Assert.That(
                ImageLocator.Parse(
                    "https://www.googleapis.com/compute/v1/" + path).ToString(), Is.EqualTo(path));
        }
    }
}
