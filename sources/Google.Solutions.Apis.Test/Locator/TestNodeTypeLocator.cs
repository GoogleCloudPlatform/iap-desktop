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
    public class TestNodeTypeLocator
        : TestLocatorFixtureBase<NodeTypeLocator>
    {
        protected override NodeTypeLocator CreateInstance()
        {
            return new NodeTypeLocator("project-1", "zone-1", "type-1");
        }

        [Test]
        public void Project()
        {
            var ref1 = new NodeTypeLocator("project-1", "zone-1", "type-1");
            Assert.That(ref1.Project.Name, Is.EqualTo(ref1.ProjectId));
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse_WhenPathIsValid()
        {
            Assert.IsTrue(NodeTypeLocator.TryParse(
                "projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.That(ref1!.ResourceType, Is.EqualTo("nodeTypes"));
            Assert.That(ref1.Name, Is.EqualTo("c2-node-60-240"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenQualifiedByComputeGoogleapisHost()
        {
            Assert.IsTrue(NodeTypeLocator.TryParse(
                "https://compute.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.That(ref1!.ResourceType, Is.EqualTo("nodeTypes"));
            Assert.That(ref1.Name, Is.EqualTo("c2-node-60-240"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenQualifiedByGoogleapisHost()
        {
            Assert.IsTrue(NodeTypeLocator.TryParse(
                "https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.That(ref1!.ResourceType, Is.EqualTo("nodeTypes"));
            Assert.That(ref1.Name, Is.EqualTo("c2-node-60-240"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenUsingBetaApi()
        {
            Assert.IsTrue(NodeTypeLocator.TryParse(
                 "https://compute.googleapis.com/compute/beta/projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240",
                out var ref1));

            Assert.IsNotNull(ref1);
            Assert.That(ref1!.ResourceType, Is.EqualTo("nodeTypes"));
            Assert.That(ref1.Name, Is.EqualTo("c2-node-60-240"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void TryParse_WhenPathLacksProject()
        {
            Assert.That(NodeTypeLocator.TryParse(
                "project-1/zones/us-central1-a/nodeTypes/c2-node-60-240",
                out var _), Is.False);
        }

        [Test]
        public void TryParse_WhenPathInvalid()
        {
            Assert.That(NodeTypeLocator.TryParse(
                "projects/project-1/zones/us-central1-a/nodeTypes/",
                out var _), Is.False);
            Assert.That(NodeTypeLocator.TryParse(
                "/zones/us-central1-a/nodeTypes/c2-node-60-240 ",
                out var _), Is.False);
            Assert.That(NodeTypeLocator.TryParse(
                "/",
                out var _), Is.False);
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenPathIsValid()
        {
            var ref1 = NodeTypeLocator.Parse(
                "projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240");

            Assert.That(ref1.ResourceType, Is.EqualTo("nodeTypes"));
            Assert.That(ref1.Name, Is.EqualTo("c2-node-60-240"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenQualifiedByComputeGoogleapisHost()
        {
            var ref1 = NodeTypeLocator.Parse(
                "https://compute.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240");

            Assert.That(ref1.ResourceType, Is.EqualTo("nodeTypes"));
            Assert.That(ref1.Name, Is.EqualTo("c2-node-60-240"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenQualifiedByGoogleapisHost()
        {
            var ref1 = NodeTypeLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240");

            Assert.That(ref1.ResourceType, Is.EqualTo("nodeTypes"));
            Assert.That(ref1.Name, Is.EqualTo("c2-node-60-240"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenUsingBetaApi()
        {
            var ref1 = NodeTypeLocator.Parse(
                 "https://compute.googleapis.com/compute/beta/projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240");
            Assert.That(ref1.ResourceType, Is.EqualTo("nodeTypes"));
            Assert.That(ref1.Name, Is.EqualTo("c2-node-60-240"));
            Assert.That(ref1.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(ref1.ProjectId, Is.EqualTo("project-1"));
        }

        [Test]
        public void Parse_WhenPathLacksProject()
        {
            Assert.Throws<ArgumentException>(() => NodeTypeLocator.Parse(
                "project-1/zones/us-central1-a/nodeTypes/c2-node-60-240"));
        }

        [Test]
        public void Parse_WhenPathInvalid()
        {
            Assert.Throws<ArgumentException>(() => NodeTypeLocator.Parse(
                "projects/project-1/zones/us-central1-a/nodeTypes/"));
            Assert.Throws<ArgumentException>(() => NodeTypeLocator.Parse(
                "/zones/us-central1-a/nodeTypes/c2-node-60-240 "));
            Assert.Throws<ArgumentException>(() => NodeTypeLocator.Parse(
                "/"));
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        [Test]
        public void Equals_WhenObjectsNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new NodeTypeLocator("proj", "zone1", "c2-node-60-240");
            var ref2 = new NodeTypeLocator("proj", "zone2", "c2-node-60-240");

            Assert.That(ref1.Equals(ref2), Is.False);
            Assert.That(ref1.Equals((object?)ref2), Is.False);
            Assert.That(ref1 == ref2, Is.False);
            Assert.IsTrue(ref1 != ref2);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240";

            Assert.That(
                NodeTypeLocator.Parse(path).ToString(), Is.EqualTo(path));
        }

        [Test]
        public void ToString_WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240";

            Assert.That(
                NodeTypeLocator.Parse(
                    "https://www.googleapis.com/compute/v1/" + path).ToString(), Is.EqualTo(path));
        }
    }
}
