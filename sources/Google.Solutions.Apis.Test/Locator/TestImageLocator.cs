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
            Assert.AreEqual(ref1.ProjectId, ref1.Project.Name);
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse_WhenPathIsValid()
        {
            Assert.IsTrue(ImageLocator.TryParse(
                "projects/project-1/global/images/image-1",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.AreEqual("images", ref1!.ResourceType);
            Assert.AreEqual("image-1", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void TryParse_WhenResourceNameCotainsSlash()
        {
            Assert.IsTrue(ImageLocator.TryParse(
                "projects/debian-cloud/global/images/family/debian-9",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.AreEqual("images", ref1!.ResourceType);
            Assert.AreEqual("family/debian-9", ref1.Name);
            Assert.AreEqual("debian-cloud", ref1.ProjectId);
        }

        [Test]
        public void TryParse_WhenQualifiedByComputeGoogleapisHost()
        {
            Assert.IsTrue(ImageLocator.TryParse(
                "https://compute.googleapis.com/compute/v1/projects/debian-cloud/global/images/family/debian-9",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.AreEqual("images", ref1!.ResourceType);
            Assert.AreEqual("family/debian-9", ref1.Name);
            Assert.AreEqual("debian-cloud", ref1.ProjectId);
        }

        [Test]
        public void TryParse_WhenQualifiedByGoogleapisHost()
        {
            Assert.IsTrue(ImageLocator.TryParse(
                "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/images/windows-server-core",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.AreEqual("images", ref1!.ResourceType);
            Assert.AreEqual("windows-server-core", ref1.Name);
            Assert.AreEqual("windows-cloud", ref1.ProjectId);
        }

        [Test]
        public void TryParse_WhenUsingBetaApi()
        {
            Assert.IsTrue(ImageLocator.TryParse(
                "https://compute.googleapis.com/compute/beta/projects/eip-images/global/images/debian-9-drawfork-v20191004",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.AreEqual("images", ref1!.ResourceType);
            Assert.AreEqual("debian-9-drawfork-v20191004", ref1.Name);
            Assert.AreEqual("eip-images", ref1.ProjectId);
        }

        [Test]
        public void TryParse_WhenPathLacksProject()
        {
            Assert.IsFalse(ImageLocator.TryParse(
                "/project-1/project-1/global/images/image-1",
                out var _));
        }

        [Test]
        public void TryParse_WhenPathInvalid()
        {
            Assert.IsFalse(ImageLocator.TryParse(
                "projects/project-1/notglobal/images/image-1",
                out var _));
            Assert.IsFalse(ImageLocator.TryParse(
                "/project-1/global/images/image-1",
                out var _));
            Assert.IsFalse(ImageLocator.TryParse(
                "/",
                out var _));
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenPathIsValid()
        {
            var ref1 = ImageLocator.Parse(
                "projects/project-1/global/images/image-1");

            Assert.AreEqual("images", ref1.ResourceType);
            Assert.AreEqual("image-1", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void Parse_WhenResourceNameCotainsSlash()
        {
            var ref1 = ImageLocator.Parse(
                "projects/debian-cloud/global/images/family/debian-9");

            Assert.AreEqual("images", ref1.ResourceType);
            Assert.AreEqual("family/debian-9", ref1.Name);
            Assert.AreEqual("debian-cloud", ref1.ProjectId);
        }

        [Test]
        public void Parse_WhenQualifiedByComputeGoogleapisHost()
        {
            var ref1 = ImageLocator.Parse(
                "https://compute.googleapis.com/compute/v1/projects/debian-cloud/global/images/family/debian-9");

            Assert.AreEqual("images", ref1.ResourceType);
            Assert.AreEqual("family/debian-9", ref1.Name);
            Assert.AreEqual("debian-cloud", ref1.ProjectId);
        }

        [Test]
        public void Parse_WhenQualifiedByGoogleapisHost()
        {
            var ref1 = ImageLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/images/windows-server-core");

            Assert.AreEqual("images", ref1.ResourceType);
            Assert.AreEqual("windows-server-core", ref1.Name);
            Assert.AreEqual("windows-cloud", ref1.ProjectId);
        }

        [Test]
        public void Parse_WhenUsingBetaApi()
        {
            var ref1 = ImageLocator.Parse(
                "https://compute.googleapis.com/compute/beta/projects/eip-images/global/images/debian-9-drawfork-v20191004");

            Assert.AreEqual("images", ref1.ResourceType);
            Assert.AreEqual("debian-9-drawfork-v20191004", ref1.Name);
            Assert.AreEqual("eip-images", ref1.ProjectId);
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
            var path = "projects/project-1/global/images/image-1";

            Assert.AreEqual(
                path,
                ImageLocator.Parse(path).ToString());
        }

        [Test]
        public void ToString_WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/global/images/image-1";

            Assert.AreEqual(
                path,
                ImageLocator.Parse(
                    "https://www.googleapis.com/compute/v1/" + path).ToString());
        }
    }
}
