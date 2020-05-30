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

using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Test.Services.SchedulingReport
{
    [TestFixture]
    public class TestReportViewModel : FixtureBase
    {
        private static readonly DateTime BaselineTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static ReportArchive CreateReportArchive()
        {
            var builder = new InstanceSetHistoryBuilder(
                BaselineTime,
                BaselineTime.AddDays(7));
            return new ReportArchive(builder.Build());
        }

        [Test]
        public void WhenIncludeXxSet_ThenGetIncludeXxReflectsThat()
        {
            var viewModel = new ReportViewModel(CreateReportArchive());

            viewModel.IncludeByolInstances =
                viewModel.IncludeSplaInstances =
                viewModel.IncludeUnknownLicensedInstances =

                viewModel.IncludeFleetInstances =
                viewModel.IncludeSoleTenantInstances =

                viewModel.IncludeLinuxInstances =
                viewModel.IncludeWindowsInstances =
                viewModel.IncludeUnknownOsInstances = false;

            Assert.IsFalse(viewModel.IncludeByolInstances);
            Assert.IsFalse(viewModel.IncludeSplaInstances);
            Assert.IsFalse(viewModel.IncludeUnknownLicensedInstances);
            
            Assert.IsFalse(viewModel.IncludeFleetInstances);
            Assert.IsFalse(viewModel.IncludeSoleTenantInstances);
            
            Assert.IsFalse(viewModel.IncludeLinuxInstances);
            Assert.IsFalse(viewModel.IncludeWindowsInstances);
            Assert.IsFalse(viewModel.IncludeUnknownOsInstances);


            viewModel.IncludeByolInstances =
                viewModel.IncludeSplaInstances =
                viewModel.IncludeUnknownLicensedInstances =

                viewModel.IncludeFleetInstances =
                viewModel.IncludeSoleTenantInstances =

                viewModel.IncludeLinuxInstances =
                viewModel.IncludeWindowsInstances =
                viewModel.IncludeUnknownOsInstances = true;

            Assert.IsTrue(viewModel.IncludeByolInstances);
            Assert.IsTrue(viewModel.IncludeSplaInstances);
            Assert.IsTrue(viewModel.IncludeUnknownLicensedInstances);

            Assert.IsTrue(viewModel.IncludeFleetInstances);
            Assert.IsTrue(viewModel.IncludeSoleTenantInstances);

            Assert.IsTrue(viewModel.IncludeLinuxInstances);
            Assert.IsTrue(viewModel.IncludeWindowsInstances);
            Assert.IsTrue(viewModel.IncludeUnknownOsInstances);
        }

        [Test]
        public void WhenInstancesTabSelected_ThenRightMenusAreEnabled()
        {
            var viewModel = new ReportViewModel(CreateReportArchive());
            viewModel.SelectedTabIndex = 1;
            viewModel.SelectedTabIndex = 0;

            Assert.IsTrue(viewModel.IsTenancyMenuEnabled);
            Assert.IsTrue(viewModel.IsOsMenuEnabled);
            Assert.IsTrue(viewModel.IsLicenseMenuEnabled);
        }

        [Test]
        public void WhenNodesTabSelected_ThenRightMenusAreEnabled()
        {
            var viewModel = new ReportViewModel(CreateReportArchive());
            viewModel.SelectedTabIndex = 1;

            Assert.IsFalse(viewModel.IsTenancyMenuEnabled);
            Assert.IsTrue(viewModel.IsOsMenuEnabled);
            Assert.IsTrue(viewModel.IsLicenseMenuEnabled);
        }
    }
}
