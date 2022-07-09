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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Locator;
using Google.Solutions.Testing.Common.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.UsageReport;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.UsageReport
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestReportBuilderLicenseAnnotations : ActivityFixtureBase
    {
        [Test]
        public async Task WhenWindowsInstanceCreated_ThenReportContainsInstanceAndLicenseInfoFromItsDisk(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Roles = new[] {
                PredefinedRole.ComputeViewer,
                PredefinedRole.LogsViewer })] ResourceTask<ICredential> credential)
        {
            await testInstance;
            var instanceRef = await testInstance;

            var startDate = DateTime.UtcNow.AddDays(-1);
            var builder = new ReportBuilder(
                new AuditLogAdapter(await credential),
                new AuditLogStorageSinkAdapter(
                    new StorageAdapter(await credential),
                    new AuditLogAdapter(await credential)),
                new ComputeEngineAdapter(await credential),
                AuditLogSources.Api,
                new[] { TestProject.ProjectId },
                startDate);
            var report = await builder
                .BuildAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var instance = report.History.Instances.First(i => i.Reference == instanceRef);
            Assert.IsTrue(report.IsInstanceAnnotatedAs(
                instance,
                OperatingSystemTypes.Windows,
                LicenseTypes.Spla));
        }

        [Test]
        public async Task WhenLinuxInstanceCreated_ThenReportContainsInstanceAndLicenseInfoFromItsDisk(
            [LinuxInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Roles = new[] {
                PredefinedRole.ComputeViewer,
                PredefinedRole.LogsViewer })] ResourceTask<ICredential> credential)
        {
            await testInstance;
            var instanceRef = await testInstance;

            var startDate = DateTime.UtcNow.AddDays(-1);
            var builder = new ReportBuilder(
                new AuditLogAdapter(await credential),
                 new AuditLogStorageSinkAdapter(
                    new StorageAdapter(await credential),
                    new AuditLogAdapter(await credential)),
                new ComputeEngineAdapter(await credential),
                AuditLogSources.Api,
                new[] { TestProject.ProjectId },
                startDate);
            var report = await builder
                .BuildAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var instance = report.History.Instances.First(i => i.Reference == instanceRef);
            Assert.IsTrue(report.IsInstanceAnnotatedAs(
                instance,
                OperatingSystemTypes.Linux,
                LicenseTypes.Unknown));
        }
    }
}
