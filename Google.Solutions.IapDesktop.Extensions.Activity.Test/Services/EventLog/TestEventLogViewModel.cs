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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Activity.Events.Lifecycle;
using Google.Solutions.IapDesktop.Extensions.Activity.Events.System;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.EventLog
{
    [TestFixture]
    public class TestEventLogViewModel : FixtureBase
    {
        private JobServiceMock jobServiceMock;
        private AuditLogAdapterMock auditLogAdapter;
        private EventLogViewModel viewModel;

        private class JobServiceMock : IJobService
        {
            public int Calls = 0;

            public Task<T> RunInBackground<T>(
                JobDescription jobDescription, 
                Func<CancellationToken, Task<T>> jobFunc)
            {
                Calls++;
                return jobFunc(CancellationToken.None);
            }
        }

        private class AuditLogAdapterMock : IAuditLogAdapter
        {
            public int CallCount = 0;
            public Task ListInstanceEventsAsync(
                IEnumerable<string> projectIds, 
                IEnumerable<string> zones, 
                IEnumerable<ulong> instanceIds, 
                DateTime startTime, 
                IEventProcessor processor, 
                CancellationToken cancellationToken)
            {
                this.CallCount++;

                var systemEventJson = @"
                 {
                   'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'authenticationInfo': {
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'NotifyInstanceLocation',
                     'request': {
                       '@type': 'type.googleapis.com/NotifyInstanceLocation'
                     },
                     'metadata': {
                       'serverId': '4aaaa7b32a208e7ccb4ee62acedee725',
                       'timestamp': '2020-05-04T01:50:10.917Z',
                       '@type': 'type.googleapis.com/google.cloud.audit.GceInstanceLocationMetadata'
                     }
                   },
                   'insertId': '-x0boqfe25xye',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'instance_id': '7045222222254025',
                       'project_id': 'project-1',
                       'zone': 'us-central1-a'
                     }
                   },
                   'timestamp': '2020-05-04T01:50:16.885Z',
                   'severity': 'INFO',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fsystem_event',
                   'receiveTimestamp': '2020-05-04T01:50:17.020301892Z'
                 } ";

                var lifecycleEventJson = @"
                {
                   'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'authenticationInfo': {
                     },
                     'requestMetadata': {
                       'callerIp': '1.2.3.4',
                       'callerSuppliedUserAgent': 'Mozilla'
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'v1.compute.instances.reset',
                     'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                     'request': {
                       '@type': 'type.googleapis.com/compute.instances.reset'
                     }
                   },
                   'insertId': 'yz07i2c',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'instance_id': '4894051111144103',
                       'project_id': 'project-1',
                       'zone': 'us-central1-a'
                     }
                   },
                   'timestamp': '2020-05-11T14:41:30.863Z',
                   'severity': 'NOTICE',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                   'operation': {
                     'id': 'operation-1589208088486-5a5605796a1ac-2d2b0706-bf57b173',
                     'producer': 'compute.googleapis.com',
                     'last': true
                   },
                   'receiveTimestamp': '2020-05-11T14:41:31.096086630Z'
                 }";

                processor.Process(new NotifyInstanceLocationEvent(LogRecord.Deserialize(systemEventJson)));
                processor.Process(new ResetInstanceEvent(LogRecord.Deserialize(lifecycleEventJson)));
                return Task.CompletedTask;
            }
        }

        [SetUp]
        public void SetUp()
        {
            var registry = new ServiceRegistry();
            this.jobServiceMock = new JobServiceMock();
            registry.AddSingleton<IJobService>(this.jobServiceMock);

            this.auditLogAdapter = new AuditLogAdapterMock();
            registry.AddSingleton<IAuditLogAdapter>(this.auditLogAdapter);

            viewModel = new EventLogViewModel(
                null,
                registry);
        }

        [Test]
        public void WhenNodeIsCloudNode_ThenCommandStateIsUnavailable()
        {
            var node = new Mock<IProjectExplorerCloudNode>().Object;
            Assert.AreEqual(CommandState.Unavailable, EventLogViewModel.GetCommandState(node));
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingToCloudNode_ThenListIsDisabled()
        {
            var node = new Mock<IProjectExplorerCloudNode>();
            await this.viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(this.viewModel.IsEventListEnabled);
            Assert.IsFalse(this.viewModel.Events.Any());
        }

        [Test]
        public async Task WhenSwitchingToProjectNode_ThenListIsPopulated()
        {
            var node = new Mock<IProjectExplorerProjectNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");

            await this.viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await this.viewModel.SwitchToModelAsync(node.Object);

            Assert.IsTrue(this.viewModel.IsEventListEnabled);
            Assert.AreEqual(2, this.viewModel.Events.Count);
        }

        [Test]
        public async Task WhenSwitchingToZoneNode_ThenListIsPopulated()
        {
            var node = new Mock<IProjectExplorerZoneNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.ZoneId).Returns("zone-1");

            await this.viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await this.viewModel.SwitchToModelAsync(node.Object);

            Assert.IsTrue(this.viewModel.IsEventListEnabled);
            Assert.AreEqual(2, this.viewModel.Events.Count);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNode_ThenListIsPopulated()
        {
            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.InstanceName).Returns("instance-1");

            await this.viewModel.SwitchToModelAsync(node.Object);

            // Switch again.
            await this.viewModel.SwitchToModelAsync(node.Object);

            Assert.IsTrue(this.viewModel.IsEventListEnabled);
            Assert.AreEqual(2, this.viewModel.Events.Count);
        }

        //---------------------------------------------------------------------
        // Filtering.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenChangingIsIncludeSystemEventsButtonChecked_ThenEventListIsUpdated()
        {
            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.InstanceName).Returns("instance-1");

            await this.viewModel.SwitchToModelAsync(node.Object);

            Assert.AreEqual(2, this.viewModel.Events.Count);

            this.viewModel.IsIncludeSystemEventsButtonChecked = false;
            Assert.AreEqual(1, this.viewModel.Events.Count);
            Assert.IsTrue(this.viewModel.Events.All(e => e.LogRecord.IsActivityEvent));
        }

        [Test]
        public async Task WhenChangingIsIncludeLifecycleEventsButtonChecked_ThenEventListIsUpdated()
        {
            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.InstanceName).Returns("instance-1");

            await this.viewModel.SwitchToModelAsync(node.Object);

            Assert.AreEqual(2, this.viewModel.Events.Count);

            this.viewModel.IsIncludeLifecycleEventsButtonChecked = false;
            Assert.AreEqual(1, this.viewModel.Events.Count);
            Assert.IsTrue(this.viewModel.Events.All(e => e.LogRecord.IsSystemEvent));
        }

        [Test]
        public async Task WhenChangingTimeframe_ThenReloadIsTriggered()
        {
            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.ProjectId).Returns("project-1");
            node.SetupGet(n => n.ZoneId).Returns("zone-1");
            node.SetupGet(n => n.InstanceName).Returns("instance-1");

            await this.viewModel.SwitchToModelAsync(node.Object);

            Assert.AreEqual(1, this.jobServiceMock.Calls);

            this.viewModel.SelectedTimeframeIndex = 2;

            Assert.AreEqual(2, this.jobServiceMock.Calls);
        }
    }
}
