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

using Google.Solutions.Common.Test.Integration;
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
    public class TestReportBuilder : FixtureBase
    {
        [Test]
        public async Task WhenWindowsInstanceCreated_ThenReportContainsInstanceAndLicenseInfoFromItsDisk(
            [WindowsInstance] InstanceRequest testInstance,
            [Credential(Roles = new[] {
                PredefinedRole.ComputeViewer,
                PredefinedRole.LogsViewer })] CredentialRequest credential)
        {
            await testInstance.AwaitReady();
            var instanceRef = await testInstance.GetInstanceAsync();

            var startDate = DateTime.UtcNow.AddDays(-1);
            var builder = new ReportBuilder(
                new AuditLogAdapter(await credential.GetCredentialAsync()),
                new ComputeEngineAdapter(await credential.GetCredentialAsync()),
                new[] { TestProject.ProjectId },
                startDate);
            var report = await builder.BuildAsync(AuditLogSources.Api, CancellationToken.None);

            var instance = report.History.Instances.First(i => i.Reference == instanceRef);
            Assert.IsTrue(report.IsInstanceAnnotatedAs(
                instance,
                OperatingSystemTypes.Windows,
                LicenseTypes.Spla));
        }

        [Test]
        public async Task WhenLinuxInstanceCreated_ThenReportContainsInstanceAndLicenseInfoFromItsDisk(
            [LinuxInstance] InstanceRequest testInstance,
            [Credential(Roles = new[] {
                PredefinedRole.ComputeViewer,
                PredefinedRole.LogsViewer })] CredentialRequest credential)
        {
            await testInstance.AwaitReady();
            var instanceRef = await testInstance.GetInstanceAsync();

            var startDate = DateTime.UtcNow.AddDays(-1);
            var builder = new ReportBuilder(
                new AuditLogAdapter(await credential.GetCredentialAsync()),
                new ComputeEngineAdapter(await credential.GetCredentialAsync()),
                new[] { TestProject.ProjectId },
                startDate);
            var report = await builder.BuildAsync(AuditLogSources.Api, CancellationToken.None);

            var instance = report.History.Instances.First(i => i.Reference == instanceRef);
            Assert.IsTrue(report.IsInstanceAnnotatedAs(
                instance,
                OperatingSystemTypes.Linux,
                LicenseTypes.Unknown));
        }
    }
}
