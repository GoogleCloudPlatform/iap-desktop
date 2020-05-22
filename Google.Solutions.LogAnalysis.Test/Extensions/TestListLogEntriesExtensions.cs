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
using Google.Apis.Logging.v2;
using Google.Apis.Logging.v2.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Testbed;
using Google.Solutions.LogAnalysis.Events;
using Google.Solutions.LogAnalysis.Events.Lifecycle;
using Google.Solutions.LogAnalysis.Extensions;
using Google.Solutions.LogAnalysis.History;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.LogAnalysis.Test.Extensions
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestListLogEntriesExtensions : FixtureBase
    {
        [Test]
        public async Task WhenInstanceCreated_ThenListLogEntriesReturnsInsertEvent(
            [LinuxInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();
            var instanceRef = await testInstance.GetInstanceAsync();

            var loggingService = new LoggingService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Defaults.GetCredential()
            });

            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            var request = new ListLogEntriesRequest()
            {
                ResourceNames = new[]
                {
                    "projects/" + Defaults.ProjectId
                },
                Filter = $"resource.type=\"gce_instance\" " +
                    $"AND protoPayload.methodName:{InsertInstanceEvent.Method} " +
                    $"AND timestamp > {startDate:yyyy-MM-dd}",
                PageSize = 1000,
                OrderBy = "timestamp desc"
            };

            var events = new List<EventBase>();
            var instanceBuilder = new InstanceSetHistoryBuilder(startDate, endDate);

            // Creating the VM might be quicker than the logs become available.
            for (int retry = 0; retry < 4 && !events.Any(); retry++)
            {
                await loggingService.Entries.ListEventsAsync(
                    request,
                    events.Add,
                    new Apis.Util.ExponentialBackOff(),
                    CancellationToken.None);

                if (!events.Any())
                {
                    await Task.Delay(20 * 1000);
                }
            }

            var insertEvent = events.OfType<InsertInstanceEvent>()
                .First(e => e.InstanceReference == instanceRef);
            Assert.IsNotNull(insertEvent);
        }

        [Test]
        public async Task WhenInstanceCreated_ThenListInstanceEventsAsyncCanFeedHistorySetBuilder(
            [LinuxInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();
            var instanceRef = await testInstance.GetInstanceAsync();

            var loggingService = new LoggingService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Defaults.GetCredential()
            });

            var computeService = new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Defaults.GetCredential()
            });

            var instanceBuilder = new InstanceSetHistoryBuilder(
                DateTime.UtcNow.AddDays(-7),
                DateTime.UtcNow);
            await instanceBuilder.AddExistingInstances(
                computeService.Instances,
                computeService.Disks,
                Defaults.ProjectId);

            await loggingService.Entries.ListInstanceEventsAsync(
                new[] { Defaults.ProjectId },
                instanceBuilder.StartDate,
                instanceBuilder,
                CancellationToken.None);

            var set = instanceBuilder.Build();
            var testInstanceHistory = set.Instances.FirstOrDefault(i => i.Reference == instanceRef);

            Assert.IsNotNull(testInstanceHistory, "Instance found in history");
        }


        [Test]
        public void WhenUsingInvalidProjectId_ThenListEventsAsyncThrowsException()
        {
            var loggingService = new LoggingService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Defaults.GetCredential()
            });

            var startDate = DateTime.UtcNow.AddDays(-30);
            var request = new ListLogEntriesRequest()
            {
                ResourceNames = new[]
                {
                    "projects/invalid"
                },
                Filter = $"resource.type=\"gce_instance\" " +
                    $"AND protoPayload.methodName:{InsertInstanceEvent.Method} " +
                    $"AND timestamp > {startDate:yyyy-MM-dd}",
                PageSize = 1000,
                OrderBy = "timestamp desc"
            };

            AssertEx.ThrowsAggregateException<GoogleApiException>(
                () => loggingService.Entries.ListEventsAsync(
                    request,
                    _ => { },
                    new Apis.Util.ExponentialBackOff(),
                    CancellationToken.None).Wait());
        }
    }
}
