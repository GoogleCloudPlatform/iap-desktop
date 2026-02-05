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

using Google.Apis.Logging.v2.Data;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.Lifecycle;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.System;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using Google.Solutions.IapDesktop.Extensions.Management.History;
using Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.EventLog;
using Google.Solutions.Testing.Application.Mocks;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.ToolWindows.EventLog
{
    [TestFixture]
    public class TestEventLogViewModel : ApplicationFixtureBase
    {

        private class AuditLogClientMock : IAuditLogClient
        {
            public int CallCount = 0;

            public Task<IEnumerable<LogSink>> ListCloudStorageSinksAsync(
                string projectId,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task ProcessInstanceEventsAsync(
                IEnumerable<string>? projectIds,
                IEnumerable<string>? zones,
                IEnumerable<ulong>? instanceIds,
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

                processor.Process(new NotifyInstanceLocationEvent(LogRecord.Deserialize(systemEventJson)!));
                processor.Process(new ResetInstanceEvent(LogRecord.Deserialize(lifecycleEventJson)!));
                return Task.CompletedTask;
            }
        }

        private static EventLogViewModel CreateViewModel(
            IJobService jobService,
            IAuditLogClient auditLogClient)
        {
            var registry = new ServiceRegistry();
            registry.AddMock<ICloudConsoleClient>();
            registry.AddSingleton<IJobService>(jobService);
            registry.AddSingleton<IAuditLogClient>(auditLogClient);

            return new EventLogViewModel(registry);
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task SwitchToModel_WhenCloudNode_ThenListIsDisabled()
        {
            var node = new Mock<IProjectModelCloudNode>();

            var viewModel = CreateViewModel(
                new SynchronousJobService(),
                new AuditLogClientMock());

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsFalse(viewModel.IsEventListEnabled);
            Assert.That(viewModel.WindowTitle, Is.EqualTo(EventLogViewModel.DefaultWindowTitle));

            Assert.IsFalse(viewModel.Events.Any());
        }

        [Test]
        public async Task SwitchToModel_WhenProjectNode_ThenListIsPopulated()
        {
            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator("project-1"));

            var viewModel = CreateViewModel(
                new SynchronousJobService(),
                new AuditLogClientMock());

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsTrue(viewModel.IsEventListEnabled);
            Assert.That(viewModel.WindowTitle, Does.Contain(EventLogViewModel.DefaultWindowTitle));
            Assert.That(viewModel.WindowTitle, Does.Contain("project-1"));

            Assert.That(viewModel.Events.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task SwitchToModel_WhenZoneNode_ThenListIsPopulated()
        {
            var node = new Mock<IProjectModelZoneNode>();
            node.SetupGet(n => n.Zone).Returns(new ZoneLocator("project-1", "zone-1"));

            var viewModel = CreateViewModel(
                new SynchronousJobService(),
                new AuditLogClientMock());

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsTrue(viewModel.IsEventListEnabled);
            Assert.That(viewModel.WindowTitle, Does.Contain(EventLogViewModel.DefaultWindowTitle));
            Assert.That(viewModel.WindowTitle, Does.Contain("zone-1"));

            Assert.That(viewModel.Events.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task SwitchToModel_WhenInstanceNode_ThenListIsPopulated()
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator("project-1", "zone-1", "instance-1"));

            var viewModel = CreateViewModel(
                new SynchronousJobService(),
                new AuditLogClientMock());

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsTrue(viewModel.IsEventListEnabled);
            Assert.That(viewModel.WindowTitle, Does.Contain(EventLogViewModel.DefaultWindowTitle));
            Assert.That(viewModel.WindowTitle, Does.Contain("instance-1"));

            Assert.That(viewModel.Events.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task SwitchToModel_WhenSwitchingNodes_ThenSelectionIsCleared()
        {
            var node1 = new Mock<IProjectModelInstanceNode>();
            node1
                .SetupGet(n => n.Instance)
                .Returns(new InstanceLocator("project-1", "zone-1", "instance-1"));

            var node2 = new Mock<IProjectModelInstanceNode>();
            node2
                .SetupGet(n => n.Instance)
                .Returns(new InstanceLocator("project-1", "zone-1", "instance-2"));

            var viewModel = CreateViewModel(
                new SynchronousJobService(),
                new AuditLogClientMock());

            await viewModel
                .SwitchToModelAsync(node1.Object)
                .ConfigureAwait(true);

            Assert.That(viewModel.Events.Count, Is.EqualTo(2));
            viewModel.SelectedEvent = viewModel.Events.First();

            // Switch to different node.
            await viewModel
                .SwitchToModelAsync(node2.Object)
                .ConfigureAwait(true);
            Assert.IsNull(viewModel.SelectedEvent);
            Assert.IsFalse(viewModel.IsOpenSelectedEventInCloudConsoleButtonEnabled);
        }

        //---------------------------------------------------------------------
        // Filtering.
        //---------------------------------------------------------------------

        [Test]
        public async Task IsIncludeSystemEventsButtonChecked_WhenChanged_ThenEventListIsUpdated()
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator("project-1", "zone-1", "instance-1"));

            var viewModel = CreateViewModel(
                new SynchronousJobService(),
                new AuditLogClientMock());

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.That(viewModel.Events.Count, Is.EqualTo(2));

            viewModel.IsIncludeSystemEventsButtonChecked = false;
            Assert.That(viewModel.Events.Count, Is.EqualTo(1));
            Assert.IsTrue(viewModel.Events.All(e => e.LogRecord.IsActivityEvent));
        }

        [Test]
        public async Task IsIncludeLifecycleEventsButtonChecked_WhenChanged_ThenEventListIsUpdated()
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator("project-1", "zone-1", "instance-1"));

            var viewModel = CreateViewModel(
                new SynchronousJobService(),
                new AuditLogClientMock());

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.That(viewModel.Events.Count, Is.EqualTo(2));

            viewModel.IsIncludeLifecycleEventsButtonChecked = false;
            Assert.That(viewModel.Events.Count, Is.EqualTo(1));
            Assert.IsTrue(viewModel.Events.All(e => e.LogRecord.IsSystemEvent));
        }

        [Test]
        public async Task SelectedTimeframeIndex_WhenChanged_ThenReloadIsTriggered()
        {
            var jobService = new SynchronousJobService();

            var viewModel = CreateViewModel(
                jobService,
                new AuditLogClientMock());

            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator("project-1", "zone-1", "instance-1"));

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.That(jobService.JobsCompleted, Is.EqualTo(1));

            viewModel.SelectedTimeframeIndex = 2;

            Assert.That(jobService.JobsCompleted, Is.EqualTo(2));
        }
    }
}
