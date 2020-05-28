﻿//
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

using Google.Solutions.Common;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Events;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Events.System;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Logs;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Test.Events.System
{
    [TestFixture]
    public class TestInstancePreemptedEvent : FixtureBase
    {
        [Test]
        public void WhenSeverityIsInfo_ThenFieldsAreExtracted()
        {
            var json = @"
                {
                   'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'status': {},
                     'authenticationInfo': {
                       'principalEmail': 'system@google.com'
                     },
                     'requestMetadata': {
                       'requestAttributes': {},
                       'destinationAttributes': {}
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'compute.instances.preempted',
                     'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                     'request': {
                       '@type': 'type.googleapis.com/compute.instances.preempted'
                     }
                   },
                   'insertId': 'epf7fkd2vsm',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'zone': 'us-central1-a',
                       'instance_id': '2162224123123123213',
                       'project_id': 'project-1'
                     }
                   },
                   'timestamp': '2020-05-11T15:56:44.808Z',
                   'severity': 'INFO',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fsystem_event',
                   'operation': {
                     'id': 'systemevent-1589212587537-5a56163c0b559-3e94b149-65935fdf',
                     'producer': 'compute.instances.preempted',
                     'first': true,
                     'last': true
                   },
                   'receiveTimestamp': '2020-05-11T15:56:45.533622382Z'
                 }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(InstancePreemptedEvent.IsInstancePreemptedEvent(r));

            var e = (InstancePreemptedEvent)r.ToEvent();

            Assert.AreEqual(2162224123123123213, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference.Name);
            Assert.AreEqual("us-central1-a", e.InstanceReference.Zone);
            Assert.AreEqual("project-1", e.InstanceReference.ProjectId);
            Assert.AreEqual("INFO", e.Severity);
            Assert.IsNull(e.Status);
            Assert.AreEqual(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
        }
    }
}
