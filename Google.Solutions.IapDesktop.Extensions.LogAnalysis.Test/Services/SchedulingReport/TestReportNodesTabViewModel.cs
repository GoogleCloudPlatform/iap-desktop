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
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Events;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Events.System;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Test.Services.SchedulingReport
{
    [TestFixture]
    public class TestReportNodesTabViewModel
    {
        private static readonly DateTime BaselineTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private ulong instanceIdSequence;

        private void AddExistingInstance(
            InstanceSetHistoryBuilder builder,
            int count,
            Tenancies tenancy)
        {
            for (int i = 0; i < count; i++)
            {
                instanceIdSequence++;
                builder.AddExistingInstance(
                    instanceIdSequence,
                    new InstanceLocator("project", "zone", $"instance-{instanceIdSequence}"),
                    new ImageLocator("project", $"image-{instanceIdSequence}"),
                    InstanceState.Running,
                    BaselineTime.AddDays(i),
                    tenancy);

                if (tenancy == Tenancies.SoleTenant)
                {
                    builder.GetInstanceHistoryBuilder(instanceIdSequence)
                        .OnSetPlacement("server-1", BaselineTime);
                }
            }
        }

        private ReportViewModel CreateParentViewModel(
            int fleetInstanceCount,
            int soleTenantInstanceCount)
        {
            this.instanceIdSequence = 0;

            var builder = new InstanceSetHistoryBuilder(
                BaselineTime,
                BaselineTime.AddDays(7));

            AddExistingInstance(builder, fleetInstanceCount, Tenancies.Fleet);
            AddExistingInstance(builder, soleTenantInstanceCount, Tenancies.SoleTenant);

            return new ReportViewModel(new ReportArchive(builder.Build()));
        }

        [Test]
        public void WhenPopulated_ThenNodesDoesNotIncludePseudoNodeForFleet()
        {
            var parentViewModel = CreateParentViewModel(1, 2);
            parentViewModel.SelectNodeTab();
            parentViewModel.Repopulate();

            var viewModel = parentViewModel.NodeReportPane;

            Assert.AreEqual(1, viewModel.Nodes.Count());
            Assert.AreEqual("server-1", viewModel.Nodes.First().ServerId);
        }
    }
}
