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
using Google.Solutions.IapDesktop.Extensions.Os.Inventory;
using Google.Solutions.IapDesktop.Extensions.Os.Views.PackageReport;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Os.Test.Views.PackageReport
{
    [TestFixture]
    public class TestReportViewModel
    {
        private static readonly InstancePackage[] packages = new InstancePackage[]
        {
            // instance-1
            new InstancePackage(
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                new Package("package-1", "arch-1", "ver-1")),
            new InstancePackage(
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                new Package("package-2", "arch-1", "ver-2")),

            // instance-2
            new InstancePackage(
                new InstanceLocator("project-1", "zone-1", "instance-2"),
                new Package("package-1", "arch-2", "ver-3")),
            new InstancePackage(
                new InstanceLocator("project-1", "zone-1", "instance-2"),
                new Package("package-3", null, null))
        };

        [Test]
        public void WhenLoaded_ThenFilteredPackagesContainsAllPackages()
        {
            var model = new ReportViewModel(packages);

            Assert.AreEqual(4, model.FilteredPackages.Count);
            CollectionAssert.Contains(model.FilteredPackages, packages[0]);
            CollectionAssert.Contains(model.FilteredPackages, packages[1]);
            CollectionAssert.Contains(model.FilteredPackages, packages[2]);
            CollectionAssert.Contains(model.FilteredPackages, packages[3]);
        }

        [Test]
        public void WhenFilterHasMultipleTerms_ThenFilteredPackagesContainsPackagesThatMatchAllTerms()
        {
            var model = new ReportViewModel(packages);
            model.Filter = "PACKAGE \t Arch-2   ver-3";
            Assert.AreEqual(1, model.FilteredPackages.Count);
            CollectionAssert.Contains(model.FilteredPackages, packages[2]);
        }

        [Test]
        public void WhenFilterIsReset_ThenFilteredPackagesContainsAllPackages()
        {
            var model = new ReportViewModel(packages);
            model.Filter = "   PACKAGE-3   ";
            Assert.AreEqual(1, model.FilteredPackages.Count);

            model.Filter = null;
            Assert.AreEqual(4, model.FilteredPackages.Count);
        }
    }
}
