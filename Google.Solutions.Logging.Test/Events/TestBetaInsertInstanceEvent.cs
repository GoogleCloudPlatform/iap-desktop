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

using Google.Solutions.Compute;
using Google.Solutions.Logging.Events;
using Google.Solutions.Logging.Records;
using NUnit.Framework;

namespace Google.Solutions.Logging.Test.Events
{
    [TestFixture]
    public class TestBetaInsertInstanceEvent
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
                 'methodName': 'beta.compute.instances.insert',
                 'authorizationInfo': [
                 ],
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                   'deletionProtection': false,
                   'serviceAccounts': [
                     {
                       'scopes': [
                         'https://www.googleapis.com/auth/devstorage.read_only',
                         'https://www.googleapis.com/auth/logging.write',
                         'https://www.googleapis.com/auth/monitoring.write',
                         'https://www.googleapis.com/auth/servicecontrol',
                         'https://www.googleapis.com/auth/service.management.readonly',
                         'https://www.googleapis.com/auth/trace.append'
                       ],
                       'email': '111-compute@developer.gserviceaccount.com'
                     }
                   ],
                   'name': 'instance-1',
                   'disks': [
                     {
                       'type': 'PERSISTENT',
                       'mode': 'READ_WRITE',
                       'autoDelete': true,
                       'initializeParams': {
                         'sourceImage': 'projects/project-1/global/images/image-1',
                         'diskType': 'projects/project-1/zones/us-central1-a/diskTypes/pd-standard',
                         'diskSizeGb': '127'
                       },
                       'deviceName': 'instance-1',
                       'boot': true
                     }
                   ],
                   '@type': 'type.googleapis.com/compute.instances.insert',
                   'machineType': 'projects/project-1/zones/us-central1-a/machineTypes/n1-standard-2',
                   'canIpForward': false,
                   'scheduling': {
                     'nodeAffinitys': [
                       {
                         'key': 'license',
                         'values': [
                           'byol'
                         ],
                         'operator': 'IN'
                       }
                     ],
                     'preemptible': false,
                     'automaticRestart': true,
                     'onHostMaintenance': 'TERMINATE'
                   },
                   'networkInterfaces': [
                     {
                       'accessConfigs': [
                         {
                           'name': 'External NAT',
                           'type': 'ONE_TO_ONE_NAT',
                           'networkTier': 'PREMIUM'
                         }
                       ],
                       'subnetwork': 'projects/project-1/regions/us-central1/subnetworks/default'
                     }
                   ],
                   'description': '',
                   'displayDevice': {
                     'enableDisplay': false
                   }
                 },
                 'response': {
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': '-jz0t6pe2g0hg',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'instance_id': '11111111631960822',
                   'zone': 'us-central1-a',
                   'project_id': 'project-1'
                 }
               },
               'timestamp': '2020-05-05T08:31:40.864Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1588667500800-5a4e27a0d2ac3-4908c06c-7b1344e1',
                 'producer': 'compute.googleapis.com',
                 'first': true
               },
               'receiveTimestamp': '2020-05-05T08:31:42.772051011Z'
             }  ";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(InsertInstanceEvent.IsInsertInstanceEvent(r));

            var e = new InsertInstanceEvent(r);

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
