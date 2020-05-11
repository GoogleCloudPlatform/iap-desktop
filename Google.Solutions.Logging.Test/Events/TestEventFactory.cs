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

using Google.Solutions.Logging.Events;
using Google.Solutions.Logging.Records;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Linq;
using System.IO;
using Google.Solutions.Logging.Events.Lifecycle;

namespace Google.Solutions.Logging.Test.Events
{
    [TestFixture]
    public class TestEventFactory
    {
        [Test]
        public void WhenStreamContainsTwoRecords_ThenReadReturnsTwoEvents()
        {
            var json = @"
            [ {
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
             },
             {
              'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'authenticationInfo': {
                 },
                 'requestMetadata': {
                 },
                 'serviceName': 'compute.googleapis.com',
                 'methodName': 'v1.compute.instances.start',
                 'authorizationInfo': [
                 ],
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-2',
                 'request': {
                   '@type': 'type.googleapis.com/compute.instances.start'
                 },
                 'response': {
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': 'vcq6epd7n72',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'project_id': 'project-1',
                   'instance_id': '489405111114222',
                   'zone': 'us-central1-a'
                 }
               },
               'timestamp': '2020-05-04T13:56:26.405Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1588600586345-5a4d2e5a39c56-47d0ce05-a9d7073c',
                 'producer': 'compute.googleapis.com',
                 'first': true
               },
               'receiveTimestamp': '2020-05-04T13:56:27.582777461Z'
             }]";

            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                var events = EventFactory.Read(reader).ToList();

                Assert.AreEqual(2, events.Count());

                Assert.IsInstanceOf(typeof(DeleteInstanceEvent), events.First());
                var deleteEvent = (DeleteInstanceEvent)events.First();
                Assert.AreEqual("instance-1", deleteEvent.InstanceReference.InstanceName);


                Assert.IsInstanceOf(typeof(StartInstanceEvent), events.Last());
                var startEvent = (StartInstanceEvent)events.Last();
                Assert.AreEqual("instance-2", startEvent.InstanceReference.InstanceName);
            }
        }

        [Test]
        public void WhenStreamContainsAnUnknownRecord_ThenReadReturnsUnknownEvent()
        {
            var json = @"
            [ {
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
             },
             {
              'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'authenticationInfo': {
                 },
                 'requestMetadata': {
                 },
                 'serviceName': 'compute.googleapis.com',
                 'methodName': 'unknown.method',
                 'authorizationInfo': [
                 ],
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-2',
                 'request': {
                   '@type': 'type.googleapis.com/compute.instances.start'
                 },
                 'response': {
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': 'vcq6epd7n72',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'project_id': 'project-1',
                   'instance_id': '489405111114222',
                   'zone': 'us-central1-a'
                 }
               },
               'timestamp': '2020-05-04T13:56:26.405Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1588600586345-5a4d2e5a39c56-47d0ce05-a9d7073c',
                 'producer': 'compute.googleapis.com',
                 'first': true
               },
               'receiveTimestamp': '2020-05-04T13:56:27.582777461Z'
             }]";

            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                var events = EventFactory.Read(reader).ToList();

                Assert.AreEqual(2, events.Count());

                Assert.IsInstanceOf(typeof(DeleteInstanceEvent), events.First());
                var deleteEvent = (DeleteInstanceEvent)events.First();
                Assert.AreEqual("instance-1", deleteEvent.InstanceReference.InstanceName);

                Assert.IsInstanceOf(typeof(UnknownEvent), events.Last());
            }
        }

        [Test]
        public void WhenStreamContainsObjectInsteadOfArry_ThenReadReturnsEmptyEnumeration()
        {
            var json = @"
            {
                'this': 'is invalid'
            }";

            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                var events = EventFactory.Read(reader).ToList();

                Assert.AreEqual(0, events.Count());
            }
        }
    }
}
