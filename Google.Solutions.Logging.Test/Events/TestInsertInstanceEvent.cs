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
using Google.Solutions.Logging.Records;
using NUnit.Framework;

namespace Google.Solutions.Logging.Test.Events
{
    [TestFixture]
    public class TestInsertInstanceEvent
    {
        [Test]
        public void WhenSeverityIsNotice_ThenFieldsAreExtracted()
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
                 'methodName': 'v1.compute.instances.insert',
                 'authorizationInfo': [
                 ],
                 'resourceName': 'projects/111/zones/us-central1-a/instances/instance-1',
                 'request': {
                   'name': 'instance-group-1-xbtt',
                   'machineType': 'projects/111/zones/us-central1-a/machineTypes/n1-standard-4',
                   'canIpForward': false,
                   'networkInterfaces': [
                     {
                       'network': 'projects/111/global/networks/default',
                       'accessConfigs': [
                         {
                           'type': 'ONE_TO_ONE_NAT',
                           'name': 'External NAT',
                           'networkTier': 'PREMIUM'
                         }
                       ],
                       'subnetwork': 'projects/111/regions/us-central1/subnetworks/default'
                     }
                   ],
                   'disks': [
                     {
                       'type': 'PERSISTENT',
                       'mode': 'READ_WRITE',
                       'deviceName': 'instance-1',
                       'boot': true,
                       'initializeParams': {
                         'sourceImage': 'projects/project-1/global/images/image-1',
                         'diskSizeGb': '127',
                         'diskType': 'projects/111/zones/us-central1-a/diskTypes/pd-standard'
                       },
                       'autoDelete': true
                     }
                   ],
                   'serviceAccounts': [
                     {
                       'email': '111-compute@developer.gserviceaccount.com',
                       'scopes': [
                         'https://www.googleapis.com/auth/devstorage.read_only',
                         'https://www.googleapis.com/auth/logging.write',
                         'https://www.googleapis.com/auth/monitoring.write',
                         'https://www.googleapis.com/auth/servicecontrol',
                         'https://www.googleapis.com/auth/service.management.readonly',
                         'https://www.googleapis.com/auth/trace.append'
                       ]
                     }
                   ],
                   'scheduling': {
                     'onHostMaintenance': 'TERMINATE',
                     'automaticRestart': false,
                     'preemptible': false,
                     'nodeAffinitys': [
                       {
                         'key': 'license',
                         'operator': 'IN',
                         'values': [
                           'byol'
                         ]
                       }
                     ]
                   },
                   'displayDevice': {
                     'enableDisplay': false
                   },
                   'links': [
                     {
                       'target': 'projects/111/locations/us-central1-a/instances/instance-group-1-xbtt',
                       'type': 'MEMBER_OF',
                       'source': 'projects/111/locations/us-central1-a/instanceGroupManagers/instance-group-1@3579973466633327805'
                     }
                   ],
                   'requestId': '4a68f20d-9f80-32f3-adc4-acf842d7ae0b',
                   '@type': 'type.googleapis.com/compute.instances.insert'
                 },
                 'response': {
                   'id': '5042353291971988238',
                   'name': 'operation-1588508129141-5a4bd5ec2a16d-418ba83e-11fc353d',
                   'zone': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a',
                   'clientOperationId': '4a68f20d-9f80-32f3-adc4-acf842d7ae0b',
                   'operationType': 'insert',
                   'targetLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/instances/instance-group-1-xbtt',
                   'targetId': '518436304627895054',
                   'status': 'RUNNING',
                   'user': '111@cloudservices.gserviceaccount.com',
                   'progress': '0',
                   'insertTime': '2020-05-03T05:15:29.813-07:00',
                   'startTime': '2020-05-03T05:15:29.817-07:00',
                   'selfLink': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/operations/operation-1588508129141-5a4bd5ec2a16d-418ba83e-11fc353d',
                   'selfLinkWithId': 'https://www.googleapis.com/compute/v1/projects/project-1/zones/us-central1-a/operations/5042353291971988238',
                   '@type': 'type.googleapis.com/operation'
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': '3vuqdhe1iqbu',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'zone': 'us-central1-a',
                   'instance_id': '11111111631960822',
                   'project_id': 'project-1'
                 }
               },
               'timestamp': '2020-05-03T12:15:29.009Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1588508129141-5a4bd5ec2a16d-418ba83e-11fc353d',
                 'producer': 'compute.googleapis.com',
                 'first': true
               },
               'receiveTimestamp': '2020-05-03T12:15:30.903794912Z'
             }
             ";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(InsertInstanceEvent.IsInsertInstanceEvent(r));

            var e = (InsertInstanceEvent)r.ToEvent();

            Assert.AreEqual(11111111631960822, e.InstanceId);
            Assert.AreEqual(
                new VmInstanceReference("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
        }

        [Test]
        public void WhenSeverityIsError_ThenFieldsAreExtracted()
        {
            Assert.Inconclusive();
        }
    }
}
