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
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.History
{
    [TestFixture]
    public class TestNodeHistory : FixtureBase
    {
        [Test]
        public void WhenNodeHasNoPlacements_ThenZoneAndProjectIdAreNull()
        {
            var node = new NodeHistory(
                "server-1",
                null,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow,
                0,
                Enumerable.Empty<NodePlacement>());

            Assert.IsNull(node.ProjectId);
            Assert.IsNull(node.Zone);
        }

        [Test]
        public void WhenNodeHasPlacementWithoutName_ThenZoneAndProjectIdAreNull()
        {
            var instance = new InstanceHistory(
                1,
                null,
                InstanceHistoryState.MissingName,
                null,
                null);
            var node = new NodeHistory(
                "server-1",
                null,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow,
                0,
                new[]
                {
                    new NodePlacement(
                        DateTime.UtcNow.AddDays(-1),
                        DateTime.UtcNow,
                        instance)
                });

            Assert.IsNull(node.ProjectId);
            Assert.IsNull(node.Zone);
        }

        [Test]
        public void WhenNodeHasPlacementWithName_ThenZoneAndProjectIdAreNotNull()
        {
            var instance = new InstanceHistory(
                1,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                InstanceHistoryState.MissingImage,
                null,
                null);
            var node = new NodeHistory(
                "server-1",
                null,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow,
                0,
                new[]
                {
                    new NodePlacement(
                        DateTime.UtcNow.AddDays(-1),
                        DateTime.UtcNow,
                        instance)
                });

            Assert.AreEqual("project-1", node.ProjectId);
            Assert.AreEqual("zone-1", node.Zone);
        }

        [Test]
        public void WhenNodeHasNoPlacements_ThenMaxInstancePlacementsByDayIsEmpty()
        {
            var node = new NodeHistory(
                "server-1",
                null,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow,
                0,
                Enumerable.Empty<NodePlacement>());

            Assert.IsFalse(node.MaxInstancePlacementsByDay.Any());
        }

        [Test]
        public void WhenNodeHasPlacements_ThenMaxInstancePlacementsByDayUsesFirstAndLastDate()
        {
            var baselineDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var instance = new InstanceHistory(
                1,
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                InstanceHistoryState.MissingImage,
                null,
                null);
            var node = new NodeHistory(
                "server-1",
                null,
                baselineDate,
                baselineDate.AddDays(2),
                0,
                new[]
                {
                    new NodePlacement(
                        baselineDate.AddDays(1),
                        baselineDate.AddDays(2),
                        instance),
                    new NodePlacement(
                        baselineDate,
                        baselineDate.AddDays(1),
                        instance)
                });

            var histogram = node.MaxInstancePlacementsByDay;
            Assert.AreEqual(baselineDate, histogram.First().Timestamp);
            Assert.AreEqual(baselineDate.AddDays(2), histogram.Last().Timestamp);
        }
    }
}
