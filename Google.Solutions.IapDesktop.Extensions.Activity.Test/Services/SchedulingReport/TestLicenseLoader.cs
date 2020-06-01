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

using Google.Apis.Compute.v1;
using Google.Apis.Services;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Testbed;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.SchedulingReport;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.SchedulingReport
{

    [TestFixture]
    [Category("IntegrationTest")]
    public class TestLicenseLoader : FixtureBase
    {
        private ReportArchive CreateSet(ImageLocator image)
        {
            return new ReportArchive(
                new InstanceSetHistory(
                    new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new[]
                    {
                        new InstanceHistory(
                            188550847350222232,
                            new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                            InstanceHistoryState.Complete,
                            image,
                            Enumerable.Empty<InstancePlacement>())
                    }));
        }

        [Test]
        public async Task WhenImageFound_ThenAnnotationIsAdded()
        {
            var annotatedSet = CreateSet(
                new ImageLocator("windows-cloud", "family/windows-2019"));

            Assert.AreEqual(0, annotatedSet.LicenseAnnotations.Count());

            var computeEngineAdapter = new ComputeEngineAdapter(Defaults.GetCredential());
            await LicenseLoader.LoadLicenseAnnotationsAsync(
                annotatedSet,
                computeEngineAdapter,
                CancellationToken.None);

            Assert.AreEqual(1, annotatedSet.LicenseAnnotations.Count());

            var annotation = annotatedSet.LicenseAnnotations.Values.First();
            Assert.AreEqual(OperatingSystemTypes.Windows, annotation.OperatingSystem);
            Assert.AreEqual(LicenseTypes.Spla, annotation.LicenseType);
        }

        [Test]
        public async Task WhenImageNotFoundButFromWindowsProject_ThenAnnotationIsAdded()
        {
            var annotatedSet = CreateSet(
                new ImageLocator("windows-cloud", "windows-95"));

            Assert.AreEqual(0, annotatedSet.LicenseAnnotations.Count());

            var computeEngineAdapter = new ComputeEngineAdapter(Defaults.GetCredential());
            await LicenseLoader.LoadLicenseAnnotationsAsync(
                annotatedSet,
                computeEngineAdapter,
                CancellationToken.None);

            Assert.AreEqual(1, annotatedSet.LicenseAnnotations.Count());

            var annotation = annotatedSet.LicenseAnnotations.Values.First();
            Assert.AreEqual(OperatingSystemTypes.Windows, annotation.OperatingSystem);
            Assert.AreEqual(LicenseTypes.Spla, annotation.LicenseType);
        }

        [Test]
        public async Task WhenImageNotFound_ThenAnnotationNotAdded()
        {
            var annotatedSet = CreateSet(
                new ImageLocator("unknown", "beos"));

            Assert.AreEqual(0, annotatedSet.LicenseAnnotations.Count());

            var computeEngineAdapter = new ComputeEngineAdapter(Defaults.GetCredential());
            await LicenseLoader.LoadLicenseAnnotationsAsync(
                annotatedSet,
                computeEngineAdapter,
                CancellationToken.None);

            Assert.AreEqual(0, annotatedSet.LicenseAnnotations.Count());
        }
    }
}
