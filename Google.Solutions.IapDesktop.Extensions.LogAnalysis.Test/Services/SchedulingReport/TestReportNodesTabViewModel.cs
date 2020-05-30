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

                var instanceBuilder = builder.GetInstanceHistoryBuilder(instanceIdSequence);
                if (tenancy == Tenancies.SoleTenant)
                {
                    // Add sole tenant placement.
                    instanceBuilder.OnSetPlacement("server-1", BaselineTime);
                }

                // Add fleet placement.
                instanceBuilder.OnStop(
                    BaselineTime.AddDays(-1),
                    new InstanceLocator("project", "zone", $"instance-{instanceIdSequence}"));
                instanceBuilder.OnStart(
                    BaselineTime.AddDays(-2),
                    new InstanceLocator("project", "zone", $"instance-{instanceIdSequence}"));
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

        [Test]
        public void WhenNodeSelected_ThenNodePlacementsShown()
        {
            var parentViewModel = CreateParentViewModel(0, 3);
            parentViewModel.SelectNodeTab();
            parentViewModel.Repopulate();

            var viewModel = parentViewModel.NodeReportPane;

            Assert.IsFalse(viewModel.NodePlacements.Any());
            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();
            Assert.AreEqual(3, viewModel.NodePlacements.Count());
        }

        [Test]
        public void WhenOsFilterSetInParent_ThenNodePlacementsContainsMatchingInstances()
        {
            var parentViewModel = CreateParentViewModel(0, 3);
            parentViewModel.Model.AddLicenseAnnotation(
                new ImageLocator("project", "image-1"),
                OperatingSystemTypes.Linux,
                LicenseTypes.Unknown);
            parentViewModel.Model.AddLicenseAnnotation(
                new ImageLocator("project", "image-2"),
                OperatingSystemTypes.Windows,
                LicenseTypes.Unknown);
            parentViewModel.Model.AddLicenseAnnotation(
                new ImageLocator("project", "image-3"),
                OperatingSystemTypes.Unknown,
                LicenseTypes.Unknown);

            parentViewModel.SelectNodeTab();
            parentViewModel.Repopulate();

            var viewModel = parentViewModel.NodeReportPane;

            parentViewModel.IncludeWindowsInstances = false;
            parentViewModel.IncludeLinuxInstances = false;
            parentViewModel.IncludeUnknownOsInstances = false;

            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();
            Assert.AreEqual(0, viewModel.NodePlacements.Count());

            parentViewModel.IncludeWindowsInstances = true;
            parentViewModel.IncludeLinuxInstances = false;
            parentViewModel.IncludeUnknownOsInstances = false;

            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();
            Assert.AreEqual(1, viewModel.NodePlacements.Count());

            parentViewModel.IncludeWindowsInstances = true;
            parentViewModel.IncludeLinuxInstances = true;
            parentViewModel.IncludeUnknownOsInstances = false;

            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();
            Assert.AreEqual(2, viewModel.NodePlacements.Count());

            parentViewModel.IncludeWindowsInstances = true;
            parentViewModel.IncludeLinuxInstances = true;
            parentViewModel.IncludeUnknownOsInstances = true;

            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();
            Assert.AreEqual(3, viewModel.NodePlacements.Count());
        }

        [Test]
        public void WhenFilterChanged_ThenSelectionIsCleared()
        {
            var parentViewModel = CreateParentViewModel(0, 3);
            parentViewModel.Model.AddLicenseAnnotation(
                new ImageLocator("project", "image-1"),
                OperatingSystemTypes.Windows,
                LicenseTypes.Spla);
            parentViewModel.Model.AddLicenseAnnotation(
                new ImageLocator("project", "image-2"),
                OperatingSystemTypes.Windows,
                LicenseTypes.Byol);
            parentViewModel.Model.AddLicenseAnnotation(
                new ImageLocator("project", "image-3"),
                OperatingSystemTypes.Windows,
                LicenseTypes.Unknown);

            parentViewModel.SelectNodeTab();
            parentViewModel.Repopulate();

            var viewModel = parentViewModel.NodeReportPane;

            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();
            Assert.IsTrue(viewModel.NodePlacements.Any());

            parentViewModel.IncludeSplaInstances = false;
            Assert.IsNull(viewModel.SelectedNode);
            Assert.IsFalse(viewModel.NodePlacements.Any());
        }

        [Test]
        public void WhenLicenseFilterSetInParent_ThenInstancesContainsMatchingInstances()
        {
            var parentViewModel = CreateParentViewModel(0, 3);
            parentViewModel.Model.AddLicenseAnnotation(
                new ImageLocator("project", "image-1"),
                OperatingSystemTypes.Windows,
                LicenseTypes.Spla);
            parentViewModel.Model.AddLicenseAnnotation(
                new ImageLocator("project", "image-2"),
                OperatingSystemTypes.Windows,
                LicenseTypes.Byol);
            parentViewModel.Model.AddLicenseAnnotation(
                new ImageLocator("project", "image-3"),
                OperatingSystemTypes.Windows,
                LicenseTypes.Unknown);

            parentViewModel.SelectNodeTab();
            parentViewModel.Repopulate();

            var viewModel = parentViewModel.NodeReportPane;

            parentViewModel.IncludeSplaInstances = false;
            parentViewModel.IncludeByolInstances = false;
            parentViewModel.IncludeUnknownLicensedInstances = false;

            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();
            Assert.AreEqual(0, viewModel.NodePlacements.Count());

            parentViewModel.IncludeSplaInstances = true;
            parentViewModel.IncludeByolInstances = false;
            parentViewModel.IncludeUnknownLicensedInstances = false;

            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();
            Assert.AreEqual(1, viewModel.NodePlacements.Count());

            parentViewModel.IncludeSplaInstances = true;
            parentViewModel.IncludeByolInstances = true;
            parentViewModel.IncludeUnknownLicensedInstances = false;

            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();
            Assert.AreEqual(2, viewModel.NodePlacements.Count());

            parentViewModel.IncludeSplaInstances = true;
            parentViewModel.IncludeByolInstances = true;
            parentViewModel.IncludeUnknownLicensedInstances = true;

            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();
            Assert.AreEqual(3, viewModel.NodePlacements.Count());
        }

        [Test]
        public void WhenDateRangeSelected_ThenInstancesContainsMatchingNodePlacements()
        {
            var parentViewModel = CreateParentViewModel(0, 3);
            parentViewModel.SelectNodeTab();
            parentViewModel.Repopulate();

            var viewModel = parentViewModel.NodeReportPane;
            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();

            Assert.AreEqual(3, viewModel.NodePlacements.Count());

            viewModel.Selection = new DateSelection()
            {
                StartDate = BaselineTime.AddDays(1),
                EndDate = BaselineTime.AddDays(2)
            };
            viewModel.SelectedNode = viewModel.Nodes.FirstOrDefault();

            Assert.AreEqual(2, viewModel.NodePlacements.Count());
        }

        [Test]
        public void WhenDateRangeSelected_ThenHistogramIsUnaffected()
        {
            var parentViewModel = CreateParentViewModel(0, 3);
            parentViewModel.SelectNodeTab();
            parentViewModel.Repopulate();

            var viewModel = parentViewModel.NodeReportPane;

            var histogram = viewModel.Histogram;
            Assert.AreEqual(BaselineTime, histogram.First().Timestamp);
            Assert.AreEqual(BaselineTime.AddDays(2), histogram.Last().Timestamp);

            viewModel.Selection = new DateSelection()
            {
                StartDate = BaselineTime,
                EndDate = BaselineTime
            };

            Assert.AreEqual(BaselineTime, histogram.First().Timestamp);
            Assert.AreEqual(BaselineTime.AddDays(2), histogram.Last().Timestamp);
        }
    }
}
