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

using Google.Solutions.Compute;
using Google.Solutions.Logging.Events;
using Google.Solutions.Logging.Events.System;
using Google.Solutions.Logging.Records;
using NUnit.Framework;

namespace Google.Solutions.Logging.Test.Events.System
{
    [TestFixture]
    public class TestTerminateOnHostMaintenanceEvent
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
                     'methodName': 'compute.instances.terminateOnHostMaintenance',
                     'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                     'request': {
                       '@type': 'type.googleapis.com/compute.instances.terminateOnHostMaintenance'
                     }
                   },
                   'insertId': 'n164j2e2uprm',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'instance_id': '2162224123123123213',
                       'project_id': 'project-1',
                       'zone': 'us-central1-a'
                     }
                   },
                   'timestamp': '2020-05-06T16:10:46.781Z',
                   'severity': 'INFO',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fsystem_event',
                   'operation': {
                     'id': 'systemevent-1588781345487-5a4fcfbb93bc2-b8c0e7d9-37cacfb7',
                     'producer': 'compute.instances.terminateOnHostMaintenance',
                     'first': true,
                     'last': true
                   },
                   'receiveTimestamp': '2020-05-06T16:10:47.109548606Z'
                 }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(TerminateOnHostMaintenanceEvent.IsTerminateOnHostMaintenanceEvent(r));


            Assert.AreEqual(2162224123123123213, ((TerminateOnHostMaintenanceEvent)r.ToEvent()).InstanceId);
            Assert.AreEqual(
                new VmInstanceReference("project-1", "us-central1-a", "instance-1"),
                ((TerminateOnHostMaintenanceEvent)r.ToEvent()).InstanceReference);
        }

        [Test]
        public void WhenSeverityIsError_ThenFieldsAreExtracted()
        {
            Assert.Inconclusive();
        }
    }
}
