//
// Copyright 2020 Google LLC
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
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.History
{
    [TestFixture]
    public class TestInstanceHistoryBuilder : FixtureBase
    {
        private static readonly InstanceLocator SampleReference = new InstanceLocator("pro", "zone", "name");
        private static readonly ImageLocator SampleImage
            = ImageLocator.FromString("projects/project-1/global/images/image-1");
        private static readonly NodeTypeLocator SampleNodeType
            = NodeTypeLocator.FromString("projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240");

        [Test]
        public void WhenInstanceIsDeletedAndNoEventsRegistered_ThenImageIsNull()
        {
            var b = InstanceHistoryBuilder.ForDeletedInstance(1);
            var i = b.Build(new DateTime(2019, 12, 1));

            Assert.AreEqual(1, i.InstanceId);

            Assert.IsNull(i.Image);
        }

        //---------------------------------------------------------------------
        // Placements for existing instances.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRedundantPlacementsRegistered_ThenSecondPlacementIsIgnored()
        {
            var b = InstanceHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                SampleImage,
                InstanceState.Terminated,
                new DateTime(2019, 12, 31),
                Tenancies.SoleTenant,
                "server-1",
                SampleNodeType);

            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 30));
            b.OnSetPlacement("server-1", SampleNodeType, new DateTime(2019, 12, 29));

            var placements = b.Build(new DateTime(2019, 12, 1)).Placements.ToList();
            Assert.AreEqual(1, placements.Count());
            Assert.AreEqual(new DateTime(2019, 12, 29), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 31), placements[0].To);
            Assert.AreEqual(SampleNodeType, placements[0].NodeType);
        }

        [Test]
        public void WhenPlacementsWithSameServerIdAfterStopRegistered_ThenPlacementsAreKept()
        {
            var b = InstanceHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                SampleImage,
                InstanceState.Running,
                new DateTime(2019, 12, 31),
                Tenancies.SoleTenant,
                "server-1",
                null);

            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 30));
            b.OnStop(new DateTime(2019, 12, 29), SampleReference);
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 28));

            var placements = b.Build(new DateTime(2019, 12, 1)).Placements.ToList();
            Assert.AreEqual(2, placements.Count());
            Assert.AreEqual(new DateTime(2019, 12, 28), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 29), placements[0].To);
            Assert.AreEqual(new DateTime(2019, 12, 30), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31), placements[1].To);
        }

        [Test]
        public void WhenPlacementsWithDifferentServerIdsRegistered_ThenPlacementsAreKept()
        {
            var b = InstanceHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                SampleImage,
                InstanceState.Running,
                new DateTime(2019, 12, 31),
                Tenancies.SoleTenant,
                "server-1",
                SampleNodeType);
            b.OnSetPlacement("server-1", SampleNodeType, new DateTime(2019, 12, 30));
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 29));

            var placements = b.Build(new DateTime(2019, 12, 1)).Placements.ToList();
            Assert.AreEqual(2, placements.Count());
            Assert.AreEqual("server-2", placements[0].ServerId);
            Assert.IsNull(placements[0].NodeType);
            Assert.AreEqual(new DateTime(2019, 12, 29), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 30), placements[0].To);
            Assert.AreEqual("server-1", placements[1].ServerId);
            Assert.AreEqual(SampleNodeType, placements[1].NodeType);
            Assert.AreEqual(new DateTime(2019, 12, 30), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31), placements[1].To);
        }

        [Test]
        public void WhenInstanceRunningAndSinglePlacementRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = InstanceHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                SampleImage,
                InstanceState.Running,
                new DateTime(2019, 12, 31),
                Tenancies.SoleTenant,
                "server-1",
                null);
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 30));

            var i = b.Build(new DateTime(2019, 12, 1));

            Assert.AreEqual(1, i.InstanceId);
            Assert.AreEqual(1, i.Placements.Count());

            var placement = i.Placements.First();
            Assert.AreEqual("server-1", placement.ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 30), placement.From);
            Assert.AreEqual(new DateTime(2019, 12, 31), placement.To);
        }

        [Test]
        public void WhenInstanceRunningAndMultiplePlacementsRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = InstanceHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                SampleImage,
                InstanceState.Running,
                new DateTime(2019, 12, 31),
                Tenancies.SoleTenant,
                "server-2",
                null);
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 30));
            b.OnSetPlacement("server-1", SampleNodeType, new DateTime(2019, 12, 29));

            var i = b.Build(new DateTime(2019, 12, 1));

            var placements = i.Placements.ToList();
            Assert.AreEqual(2, i.Placements.Count());

            Assert.AreEqual("server-1", placements[0].ServerId);
            Assert.AreEqual(SampleNodeType, placements[0].NodeType);
            Assert.AreEqual(new DateTime(2019, 12, 29), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 30), placements[0].To);

            Assert.AreEqual("server-2", placements[1].ServerId);
            Assert.IsNull(placements[1].NodeType);
            Assert.AreEqual(new DateTime(2019, 12, 30), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31), placements[1].To);
        }

        [Test]
        public void WhenInstanceRunningAndMultiplePlacementWithStopsInBetweenRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = InstanceHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                SampleImage,
                InstanceState.Running,
                new DateTime(2019, 12, 31),
                Tenancies.SoleTenant,
                "server-2",
                null);
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 30));
            b.OnStop(new DateTime(2019, 12, 29), SampleReference);
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 28));

            var i = b.Build(new DateTime(2019, 12, 1));

            var placements = i.Placements.ToList();
            Assert.AreEqual(2, i.Placements.Count());

            Assert.AreEqual("server-1", placements[0].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 28), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 29), placements[0].To);

            Assert.AreEqual("server-2", placements[1].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 30), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31), placements[1].To);
        }

        [Test]
        public void WhenInstanceRunningAndNoPlacementRegistered_ThenInstanceHasSyntheticPlacementSpanningEntirePeriod()
        {
            var reportStartDate = new DateTime(2019, 12, 1);
            var lastSeen = new DateTime(2019, 12, 31);
            var b = InstanceHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                SampleImage,
                InstanceState.Running,
                lastSeen,
                Tenancies.SoleTenant,
                "server-2",
                null);

            var i = b.Build(reportStartDate);

            var placements = i.Placements.ToList();
            Assert.AreEqual(1, i.Placements.Count());

            Assert.AreEqual("server-2", placements[0].ServerId);
            Assert.AreEqual(reportStartDate, placements[0].From);
            Assert.AreEqual(lastSeen, placements[0].To);
        }

        //---------------------------------------------------------------------
        // Placement events for deleted instances.
        //---------------------------------------------------------------------


        [Test]
        public void WhenInstanceDeletedAndSinglePlacementRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = InstanceHistoryBuilder.ForDeletedInstance(1);
            b.OnStop(new DateTime(2019, 12, 31), SampleReference);
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 30));

            Assert.AreEqual(Tenancies.SoleTenant, b.Tenancy);
            var i = b.Build(new DateTime(2019, 12, 1));

            Assert.AreEqual(1, i.InstanceId);
            Assert.AreEqual(1, i.Placements.Count());

            var placement = i.Placements.First();
            Assert.AreEqual("server-1", placement.ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 30), placement.From);
            Assert.AreEqual(new DateTime(2019, 12, 31), placement.To);
        }

        [Test]
        public void WhenInstanceDeletedAndMultiplePlacementsRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = InstanceHistoryBuilder.ForDeletedInstance(1);
            b.OnStop(new DateTime(2019, 12, 31), SampleReference);
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 30));
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 29));

            Assert.AreEqual(Tenancies.SoleTenant, b.Tenancy);
            var i = b.Build(new DateTime(2019, 12, 1));

            var placements = i.Placements.ToList();
            Assert.AreEqual(2, i.Placements.Count());

            Assert.AreEqual("server-1", placements[0].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 29), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 30), placements[0].To);

            Assert.AreEqual("server-2", placements[1].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 30), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31), placements[1].To);
        }

        [Test]
        public void WhenInstanceDeletedAndMultiplePlacementWithStopsInBetweenRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = InstanceHistoryBuilder.ForDeletedInstance(1);
            b.OnStop(new DateTime(2019, 12, 31), SampleReference);
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 30));
            b.OnStop(new DateTime(2019, 12, 29), SampleReference);
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 28));

            Assert.AreEqual(Tenancies.SoleTenant, b.Tenancy);
            var i = b.Build(new DateTime(2019, 12, 1));

            var placements = i.Placements.ToList();
            Assert.AreEqual(2, i.Placements.Count());

            Assert.AreEqual("server-1", placements[0].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 28), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 29), placements[0].To);

            Assert.AreEqual("server-2", placements[1].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 30), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31), placements[1].To);
        }


        //---------------------------------------------------------------------
        // More information needed.
        //---------------------------------------------------------------------

        [Test]
        public void WhenInstanceExists_ThenStateIsComplete()
        {
            var b = InstanceHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                SampleImage,
                InstanceState.Running,
                DateTime.Now,
                Tenancies.SoleTenant,
                "server-1",
                null);
            Assert.AreEqual(InstanceHistoryState.Complete, b.State);
        }

        [Test]
        public void WhenOnlyPlacementRegistered_ThenStateIsMissingStopEvent()
        {
            var b = InstanceHistoryBuilder.ForDeletedInstance(1);
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 30));
            Assert.AreEqual(InstanceHistoryState.MissingStopEvent, b.State);
        }

        [Test]
        public void WhenInstancDeletedAndNoPlacementRegistered_ThenStateIsMissingTenancy()
        {
            var b = InstanceHistoryBuilder.ForDeletedInstance(1);
            Assert.AreEqual(InstanceHistoryState.MissingTenancy, b.State);
        }

        [Test]
        public void WhenInstanceDeletedAndPlacementRegisteredButNoInsertRegistered_ThenStateIsMissingImage()
        {
            var b = InstanceHistoryBuilder.ForDeletedInstance(1);
            b.OnStop(new DateTime(2019, 12, 31), SampleReference);
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 30));
            Assert.AreEqual(InstanceHistoryState.MissingImage, b.State);
        }

        [Test]
        public void WhenInstanceDeletedAndPlacementAndInsertRegistered_ThenStateIsComplete()
        {
            var b = InstanceHistoryBuilder.ForDeletedInstance(1);
            b.OnStop(new DateTime(2019, 12, 31), SampleReference);
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 30));
            b.OnInsert(new DateTime(2019, 12, 30), SampleReference, SampleImage);
            Assert.AreEqual(InstanceHistoryState.Complete, b.State);
        }

        [Test]
        public void WhenInstanceDeletedAndInsertRegistered_ThenStateIsComplete()
        {
            var b = InstanceHistoryBuilder.ForDeletedInstance(1);
            b.OnStop(new DateTime(2019, 12, 29), SampleReference);
            b.OnInsert(new DateTime(2019, 12, 29), SampleReference, SampleImage);
            Assert.AreEqual(InstanceHistoryState.Complete, b.State);
        }
    }
}
