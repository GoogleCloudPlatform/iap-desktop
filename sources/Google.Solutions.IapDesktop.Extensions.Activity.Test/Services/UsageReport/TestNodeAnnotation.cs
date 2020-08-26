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
using Google.Solutions.IapDesktop.Extensions.Activity.Services.UsageReport;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.UsageReport
{

    [TestFixture]
    public class TestNodeAnnotation
    {
        [Test]
        public void WhenNodeTypeIsNull_ThenFromNodeTypeReturnsDefault()
        {
            var annotation = NodeAnnotation.FromNodeType(null);
            Assert.AreEqual("n1-node-96-624", annotation.NodeType);
        }

        [Test]
        public void WhenNodeTypeIsUnknown_ThenFromNodeTypeReturnsDefault()
        {
            var annotation = NodeAnnotation.FromNodeType(
                new NodeTypeLocator("project-1", "zone-1", "unknown-type"));
            Assert.AreEqual("n1-node-96-624", annotation.NodeType);
        }

        [Test]
        public void WhenNodeTypeIsKnown_ThenFromNodeTypeReturnsValue()
        {
            var annotation = NodeAnnotation.FromNodeType(
                new NodeTypeLocator("project-1", "zone-1", "n2d-node-224-896"));
            Assert.AreEqual("n2d-node-224-896", annotation.NodeType);
            Assert.AreEqual(128, annotation.PhysicalCores);
        }
    }
}
