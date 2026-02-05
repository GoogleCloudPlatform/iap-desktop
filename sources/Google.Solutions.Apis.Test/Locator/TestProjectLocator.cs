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
    public class TestProjectLocator
        : TestLocatorFixtureBase<ProjectLocator>
    {
        protected override ProjectLocator CreateInstance()
        {
            return new ProjectLocator("project-1");
        }

        [Test]
        public void Project()
        {
            var ref1 = new ProjectLocator("project-1");
            Assert.That(ref1.Project, Is.EqualTo(ref1));
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse_WhenPathIsValid()
        {
            Assert.That(ProjectLocator.TryParse(
                "projects/project-1",
                out var ref1), Is.True);

            Assert.That(ref1, Is.Not.Null);
            Assert.That(ref1!.ResourceType, Is.EqualTo("projects"));
            Assert.That(ref1.Name, Is.EqualTo("project-1"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenQualifiedByComputeGoogleapisHost()
        {
            Assert.That(ProjectLocator.TryParse(
                "https://compute.googleapis.com/compute/v1/projects/project-1",
                out var ref1), Is.True);

            Assert.That(ref1, Is.Not.Null);
            Assert.That(ref1!.ResourceType, Is.EqualTo("projects"));
            Assert.That(ref1.Name, Is.EqualTo("project-1"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenQualifiedByGoogleapisHost()
        {
            Assert.That(ProjectLocator.TryParse(
                "https://www.googleapis.com/compute/v1/projects/project-1",
                out var ref1), Is.True);

            Assert.That(ref1, Is.Not.Null);
            Assert.That(ref1!.ResourceType, Is.EqualTo("projects"));
            Assert.That(ref1.Name, Is.EqualTo("project-1"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenPathLacksProject()
        {
            Assert.That(ProjectLocator.TryParse(
                "/project-1",
                out var _), Is.False);
        }

        [Test]
        public void TryParse_WhenPathInvalid()
        {
            Assert.That(ProjectLocator.TryParse(
                "projects/project-1/zone",
                out var _), Is.False);
            Assert.That(ProjectLocator.TryParse(
                "",
                out var _), Is.False);
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenPathIsValid()
        {
            var ref1 = ProjectLocator.Parse(
                "projects/project-1");

            Assert.That(ref1.ResourceType, Is.EqualTo("projects"));
            Assert.That(ref1.Name, Is.EqualTo("project-1"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenQualifiedByComputeGoogleapisHost()
        {
            var ref1 = ProjectLocator.Parse(
                "https://compute.googleapis.com/compute/v1/projects/project-1");

            Assert.That(ref1.ResourceType, Is.EqualTo("projects"));
            Assert.That(ref1.Name, Is.EqualTo("project-1"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenQualifiedByGoogleapisHost()
        {
            var ref1 = ProjectLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/project-1");

            Assert.That(ref1.ResourceType, Is.EqualTo("projects"));
            Assert.That(ref1.Name, Is.EqualTo("project-1"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenPathLacksProject()
        {
            Assert.Throws<ArgumentException>(() => ProjectLocator.Parse(
                "/project-1"));
        }

        [Test]
        public void Parse_WhenPathInvalid()
        {
            Assert.Throws<ArgumentException>(() => ProjectLocator.Parse(
                "projects/project-1/zone"));
            Assert.Throws<ArgumentException>(() => ProjectLocator.Parse(""));
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        [Test]
        public void Equals_WhenObjectsNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new ProjectLocator("proj-1");
            var ref2 = new ProjectLocator("proj-2");

            Assert.That(ref1.Equals(ref2), Is.False);
            Assert.That(ref1.Equals((object)ref2), Is.False);
            Assert.That(ref1 == ref2, Is.False);
            Assert.That(ref1 != ref2, Is.True);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "projects/project-1";

            Assert.That(
                ProjectLocator.Parse(path).ToString(), Is.EqualTo(path));
        }

        [Test]
        public void ToString_WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1";

            Assert.That(
                ProjectLocator.Parse(
                    "https://www.googleapis.com/compute/v1/" + path).ToString(), Is.EqualTo(path));
        }
    }
}
