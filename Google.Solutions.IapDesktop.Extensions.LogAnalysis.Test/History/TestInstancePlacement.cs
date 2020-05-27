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

using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Test.History
{
    [TestFixture]
    public class TestInstancePlacement : FixtureBase
    {
        [Test]
        public void WhenTwoPlacementsCloseAndNoneHasServer_ThenPlacementIsMerged()
        {
            var p1 = new InstancePlacement(
                null,
                new DateTime(2020, 1, 1, 10, 0, 0),
                new DateTime(2020, 1, 1, 11, 0, 0));
            var p2 = new InstancePlacement(
                null,
                new DateTime(2020, 1, 1, 11, 0, 50),
                new DateTime(2020, 1, 1, 12, 0, 0));

            Assert.IsTrue(p1.IsAdjacent(p2));

            var merged = p1.Merge(p2);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 10, 0, 0),
                merged.From);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 12, 0, 0),
                merged.To);
            Assert.IsNull(merged.ServerId);
        }

        [Test]
        public void WhenTwoPlacementsCloseAndOneHasServer_ThenPlacementIsMerged()
        {
            var p1 = new InstancePlacement(
                null,
                new DateTime(2020, 1, 1, 10, 0, 0),
                new DateTime(2020, 1, 1, 11, 0, 0));
            var p2 = new InstancePlacement(
                "server1",
                new DateTime(2020, 1, 1, 11, 0, 50),
                new DateTime(2020, 1, 1, 12, 0, 0));

            Assert.IsTrue(p1.IsAdjacent(p2));

            var merged = p1.Merge(p2);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 10, 0, 0),
                merged.From);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 12, 0, 0),
                merged.To);
            Assert.AreEqual("server1", merged.ServerId);
        }

        [Test]
        public void WhenTwoPlacementsCloseAndBothHaveDifferentServers_ThenPlacementIsNotMerged()
        {
            var p1 = new InstancePlacement(
                "server2",
                new DateTime(2020, 1, 1, 10, 0, 0),
                new DateTime(2020, 1, 1, 11, 0, 0));
            var p2 = new InstancePlacement(
                "server1",
                new DateTime(2020, 1, 1, 11, 0, 50),
                new DateTime(2020, 1, 1, 12, 0, 0));

            Assert.IsFalse(p1.IsAdjacent(p2));
        }

        [Test]
        public void WhenTwoPlacementsNotClose_ThenPlacementIsNotMerged()
        {
            var p1 = new InstancePlacement(
                null,
                new DateTime(2020, 1, 1, 10, 0, 0),
                new DateTime(2020, 1, 1, 11, 0, 0));
            var p2 = new InstancePlacement(
                null,
                new DateTime(2020, 1, 1, 11, 2, 0),
                new DateTime(2020, 1, 1, 12, 0, 0));

            Assert.IsFalse(p1.IsAdjacent(p2));
        }
    }
}
