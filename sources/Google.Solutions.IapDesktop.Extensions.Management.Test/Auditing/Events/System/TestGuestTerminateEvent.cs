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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.System;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.Auditing.Events.System
{
    [TestFixture]
    public class TestGuestTerminateEvent : ApplicationFixtureBase
    {
        [Test]
        public void ToEvent_WhenSeverityIsInfo()
        {
            var json = @"
                 {
                   'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'status': {},
                     'authenticationInfo': {
                     },
                     'requestMetadata': {
                       'requestAttributes': {},
                       'destinationAttributes': {}
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'compute.instances.guestTerminate',
                     'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                     'request': {
                       '@type': 'type.googleapis.com/compute.instances.guestTerminate'
                     }
                   },
                   'insertId': '-usf2yfe25ij2',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'instance_id': '2162224123123123213',
                       'zone': 'us-central1-a',
                       'project_id': 'project-1'
                     }
                   },
                   'timestamp': '2020-05-06T17:39:34.635Z',
                   'severity': 'INFO',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fsystem_event',
                   'operation': {
                     'id': 'systemevent-1588786729260-5a4fe3c9f15b7-52b60abd-2478beea',
                     'producer': 'compute.instances.guestTerminate',
                     'first': true,
                     'last': true
                   },
                   'receiveTimestamp': '2020-05-06T17:39:35.248673021Z'
                 }";

            var r = LogRecord.Deserialize(json)!;
            Assert.That(GuestTerminateEvent.IsGuestTerminateEvent(r), Is.True);

            var e = (GuestTerminateEvent)r.ToEvent();

            Assert.That(e.InstanceId, Is.EqualTo(2162224123123123213));
            Assert.That(e.Instance?.Name, Is.EqualTo("instance-1"));
            Assert.That(e.Instance?.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(e.Instance?.ProjectId, Is.EqualTo("project-1"));
            Assert.That(e.Severity, Is.EqualTo("INFO"));
            Assert.IsNull(e.Status);
            Assert.That(
                e.Instance, Is.EqualTo(new InstanceLocator("project-1", "us-central1-a", "instance-1")));
        }
    }
}
