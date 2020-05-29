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

using Google.Solutions.Common;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Events;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Test.Services.SchedulingReport
{
    [TestFixture]
    public class TestInstanceReportPaneViewModel : FixtureBase
    {
        private static readonly DateTime BaselineTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void WhenInstanceSetEmpty_ThenEmptyDataIsReported()
        {
            var builder = new InstanceSetHistoryBuilder(
                BaselineTime,
                BaselineTime.AddDays(7));
            var instanceSet = builder.Build();

            var viewModel = new InstanceReportPaneViewModel(instanceSet);

            Assert.AreEqual(instanceSet.StartDate, viewModel.Selection.StartDate);
            Assert.AreEqual(instanceSet.EndDate, viewModel.Selection.EndDate);
            Assert.IsFalse(viewModel.Histogram.Any());
            Assert.IsFalse(viewModel.Instances.Any());
        }

        [Test]
        public void WhenInstanceSetNotEmpty_ThenHistogramBoundsAreCalculatedBasedOnPlacements()
        {
            var builder = new InstanceSetHistoryBuilder(
                BaselineTime,
                BaselineTime.AddDays(7));
            builder.AddExistingInstance(
                1,
                new InstanceLocator("project", "zone", "instance-1"),
                null,
                InstanceState.Running, BaselineTime.AddDays(1), Tenancy.Fleet);
            builder.AddExistingInstance(
                2,
                new InstanceLocator("project", "zone", "instance-2"),
                null,
                InstanceState.Running, BaselineTime.AddDays(5), Tenancy.Fleet);

            var instanceSet = builder.Build();
            var viewModel = new InstanceReportPaneViewModel(instanceSet);

            Assert.AreEqual(2, viewModel.Instances.Count());
            Assert.AreEqual(BaselineTime.AddDays(1), viewModel.Histogram.First().Timestamp);
            Assert.AreEqual(BaselineTime.AddDays(5), viewModel.Histogram.Last().Timestamp);
        }

        [Test]
        public void WhenSelectionChanged_ThenInstancesAreUpdated()
        {
            var builder = new InstanceSetHistoryBuilder(
                BaselineTime,
                BaselineTime.AddDays(7));
            builder.AddExistingInstance(
                1,
                new InstanceLocator("project", "zone", "instance-1"),
                null,
                InstanceState.Running, BaselineTime.AddDays(1), Tenancy.Fleet);
            builder.AddExistingInstance(
                2,
                new InstanceLocator("project", "zone", "instance-2"),
                null,
                InstanceState.Running, BaselineTime.AddDays(5), Tenancy.Fleet);

            var instanceSet = builder.Build();
            var viewModel = new InstanceReportPaneViewModel(instanceSet);

            viewModel.Selection = new DateSelection()
            {
                StartDate = BaselineTime.AddDays(2),
                EndDate = BaselineTime.AddDays(8)
            };

            Assert.AreEqual(1, viewModel.Instances.Count());
        }

        [Test]
        public void WhenSelectionChanged_ThenHistogramStaysUnchanged()
        {
            var builder = new InstanceSetHistoryBuilder(
                BaselineTime,
                BaselineTime.AddDays(7));
            builder.AddExistingInstance(
                1,
                new InstanceLocator("project", "zone", "instance-1"),
                null,
                InstanceState.Running, BaselineTime.AddDays(1), Tenancy.Fleet);
            builder.AddExistingInstance(
                2,
                new InstanceLocator("project", "zone", "instance-2"),
                null,
                InstanceState.Running, BaselineTime.AddDays(5), Tenancy.Fleet);

            var instanceSet = builder.Build();
            var viewModel = new InstanceReportPaneViewModel(instanceSet);

            Assert.AreEqual(BaselineTime.AddDays(1), viewModel.Histogram.First().Timestamp);
            Assert.AreEqual(BaselineTime.AddDays(5), viewModel.Histogram.Last().Timestamp);

            viewModel.Selection = new DateSelection()
            {
                StartDate = BaselineTime.AddDays(2),
                EndDate = BaselineTime.AddDays(8)
            };

            Assert.AreEqual(BaselineTime.AddDays(1), viewModel.Histogram.First().Timestamp);
            Assert.AreEqual(BaselineTime.AddDays(5), viewModel.Histogram.Last().Timestamp);
        }

        [Test]
        public void WhenSelectionReset_ThenFullDateRangeIsUsed()
        {
            var builder = new InstanceSetHistoryBuilder(
                BaselineTime,
                BaselineTime.AddDays(7));
            var instanceSet = builder.Build();
            var viewModel = new InstanceReportPaneViewModel(instanceSet);

            viewModel.Selection = new DateSelection()
            {
                StartDate = BaselineTime.AddDays(8),
                EndDate = BaselineTime.AddDays(8)
            };
            viewModel.Selection = new DateSelection();

            Assert.AreEqual(BaselineTime.AddDays(1), viewModel.Histogram.First().Timestamp);
            Assert.AreEqual(BaselineTime.AddDays(5), viewModel.Histogram.Last().Timestamp);
        }

        [Test]
        public void WhenIncludeSoleTenantInstancesChanged_ThenHistogramAndInstancesAreUpdated()
        {
            Assert.Inconclusive();
        }

        [Test]
        public void WhenIncludeFleetInstancesChanged_ThenHistogramAndInstancesAreUpdated()
        {
            Assert.Inconclusive();
        }
    }
}
