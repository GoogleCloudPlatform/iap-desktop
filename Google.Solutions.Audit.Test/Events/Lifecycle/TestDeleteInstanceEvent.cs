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
using Google.Solutions.Audit.Events.Lifecycle;
using Google.Solutions.Audit.Records;
using Google.Solutions.Compute;
using NUnit.Framework;

namespace Google.Solutions.Audit.Test.Events.Lifecycle
{
    [TestFixture]
    public class TestDeleteInstanceEvent
    {
        [Test]
        public void WhenSeverityIsInfo_ThenFieldsAreExtracted()
        {
            var json = @"
            {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'authenticationInfo': {
                 },
                 'requestMetadata': {
                 },
                 'serviceName': 'compute.googleapis.com',
                 'methodName': 'v1.compute.instances.delete',
                 'authorizationInfo': [
                 ],
                 'resourceName': 'projects/123/zones/us-central1-a/instances/instance-1',
                 'request': {
                   'requestId': 'f802d080-d71e-4cae-a105-41fed099e362',
                   '@type': 'type.googleapis.com/compute.instances.delete'
                 },
                 'response': {
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': '7rriyre2bn74',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'instance_id': '3771111960822',
                   'project_id': 'project-1',
                   'zone': 'us-central1-a'
                 }
               },
               'timestamp': '2020-05-04T02:07:40.933Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1588558060966-5a4c8feedd25b-f4637780-33f35e50',
                 'producer': 'compute.googleapis.com',
                 'first': true
               },
               'receiveTimestamp': '2020-05-04T02:07:41.604695630Z'
             }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(DeleteInstanceEvent.IsDeleteInstanceEvent(r));

            var e = (DeleteInstanceEvent)r.ToEvent();

            Assert.AreEqual(3771111960822, e.InstanceId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.IsNull(e.Status);
            Assert.AreEqual(
                new VmInstanceReference("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
        }

        [Test]
        public void WhenSeverityIsError_ThenFieldsAreExtracted()
        {
            var json = @"
                {
                   'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'status': {
                       'code': 5,
                       'message': 'NOT_FOUND'
                     },
                     'authenticationInfo': {
                     },
                     'requestMetadata': {
                       'callerIp': '46.88.79.14',
                       'callerSuppliedUserAgent': 'google-api-go-client/0.5,gzip(gfe)',
                       'requestAttributes': {},
                       'destinationAttributes': {}
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'v1.compute.instances.delete',
                     'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                     'request': {
                       '@type': 'type.googleapis.com/compute.instances.delete'
                     }
                   },
                   'insertId': 'j5g4dbpsk',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'zone': 'us-central1-a',
                       'instance_id': '3771111960822',
                       'project_id': 'project-1'
                     }
                   },
                   'timestamp': '2020-05-04T15:53:41.313Z',
                   'severity': 'ERROR',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                   'operation': {
                     'id': 'operation-1588607353687e332',
                     'producer': 'compute.googleapis.com',
                     'last': true
                   },
                   'receiveTimestamp': '2020-05-04T15:53:42.026014229Z'
                 }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(DeleteInstanceEvent.IsDeleteInstanceEvent(r));

            var e = (DeleteInstanceEvent)r.ToEvent();

            Assert.AreEqual(3771111960822, e.InstanceId);
            Assert.AreEqual("ERROR", e.Severity);
            Assert.AreEqual(5, e.Status.Code);
            Assert.AreEqual("NOT_FOUND", e.Status.Message);
            Assert.AreEqual(
                new VmInstanceReference("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
        }
    }
}
