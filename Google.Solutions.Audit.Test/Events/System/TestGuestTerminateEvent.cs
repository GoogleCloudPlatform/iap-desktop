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

using Google.Solutions.Audit.Events;
using Google.Solutions.Audit.Events.System;
using Google.Solutions.Audit.Records;
using Google.Solutions.Compute;
using NUnit.Framework;

namespace Google.Solutions.Audit.Test.Events.System
{
    [TestFixture]
    public class TestGuestTerminateEvent
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

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(GuestTerminateEvent.IsGuestTerminateEvent(r));

            var e = (GuestTerminateEvent)r.ToEvent();

            Assert.AreEqual(2162224123123123213, e.InstanceId);
            Assert.AreEqual(
                new VmInstanceReference("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
        }
    }
}
