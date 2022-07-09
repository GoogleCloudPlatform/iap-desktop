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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.Events.Lifecycle;
using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Events.Lifecycle
{
    [TestFixture]
    public class TestStartWithEncryptionKeyEvent : ApplicationFixtureBase
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
                 'methodName': 'v1.compute.instances.startWithEncryptionKey',
                 'authorizationInfo': [
                 ],
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                   '@type': 'type.googleapis.com/compute.instances.startWithEncryptionKey',
                   'disks': [
                     {
                       'diskEncryptionKey': {
                         'rawKey': 'REDACTED'
                       },
                       'source': 'projects/project-1/zones/us-central1-a/disks/instance-1'
                     }
                   ]
                 },
                 'response': {
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': '-1xwojrd7zqm',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'zone': 'us-central1-a',
                   'instance_id': '4894051111144103',
                   'project_id': 'project-1'
                 }
               },
               'timestamp': '2020-05-13T08:51:21.082Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1589359880972-5a583af202822-0e230dcd-0bb00231',
                 'producer': 'compute.googleapis.com',
                 'first': true
               },
               'receiveTimestamp': '2020-05-13T08:51:22.568391463Z'
             }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(StartWithEncryptionKeyEvent.IsStartWithEncryptionKeyEvent(r));

            var e = (StartWithEncryptionKeyEvent)r.ToEvent();

            Assert.AreEqual(4894051111144103, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference.Name);
            Assert.AreEqual("us-central1-a", e.InstanceReference.Zone);
            Assert.AreEqual("project-1", e.InstanceReference.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.IsNull(e.Status);
            Assert.AreEqual(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
        }


        [Test]
        public void WhenSeverityIsNoticeAndVersionIsBeta_ThenFieldsAreExtracted()
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
                 'methodName': 'beta.compute.instances.startWithEncryptionKey',
                 'authorizationInfo': [
                 ],
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                   '@type': 'type.googleapis.com/compute.instances.startWithEncryptionKey',
                   'disks': [
                     {
                       'diskEncryptionKey': {
                         'rawKey': 'REDACTED'
                       },
                       'source': 'projects/project-1/zones/us-central1-a/disks/instance-1'
                     }
                   ]
                 },
                 'response': {
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': '-1xwojrd7zqm',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'zone': 'us-central1-a',
                   'instance_id': '4894051111144103',
                   'project_id': 'project-1'
                 }
               },
               'timestamp': '2020-05-13T08:51:21.082Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1589359880972-5a583af202822-0e230dcd-0bb00231',
                 'producer': 'compute.googleapis.com',
                 'first': true
               },
               'receiveTimestamp': '2020-05-13T08:51:22.568391463Z'
             }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(StartWithEncryptionKeyEvent.IsStartWithEncryptionKeyEvent(r));

            var e = (StartWithEncryptionKeyEvent)r.ToEvent();

            Assert.AreEqual(4894051111144103, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference.Name);
            Assert.AreEqual("us-central1-a", e.InstanceReference.Zone);
            Assert.AreEqual("project-1", e.InstanceReference.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.IsNull(e.Status);
            Assert.AreEqual(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
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
                       'code': 3,
                       'message': 'INVALID_ARGUMENT'
                     },
                     'authenticationInfo': {
                     },
                     'requestMetadata': {
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'beta.compute.instances.startWithEncryptionKey',
                     'authorizationInfo': [
                     ],
                     'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                     'request': {
                       '@type': 'type.googleapis.com/compute.instances.startWithEncryptionKey',
                       'disks': [
                         {
                           'diskEncryptionKey': {
                             'rawKey': 'REDACTED'
                           },
                           'source': 'projects/project-1/zones/us-central1-a/disks/instance-1'
                         }
                       ]
                     },
                     'response': {
                       '@type': 'type.googleapis.com/error',
                       'error': {
                         'errors': [
                           {
                             'message': 'The encryption key provided for projects/project-1/zones/us-central1-a/disks/instance-1 does not match the key that it was encrypted with.',
                             'domain': 'global',
                             'reason': 'customerEncryptionKeyIsIncorrect'
                           }
                         ],
                         'code': 400,
                         'message': 'The encryption key provided for projects/project-1/zones/us-central1-a/disks/instance-1 does not match the key that it was encrypted with.'
                       }
                     },
                     'resourceLocation': {
                       'currentLocations': [
                         'us-central1-a'
                       ]
                     }
                   },
                   'insertId': 'p95599d1jxk',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'instance_id': '',
                       'project_id': 'project-1',
                       'zone': 'us-central1-a'
                     }
                   },
                   'timestamp': '2020-05-13T09:01:42.191Z',
                   'severity': 'ERROR',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                   'receiveTimestamp': '2020-05-13T09:01:43.257996525Z'
                 }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(StartWithEncryptionKeyEvent.IsStartWithEncryptionKeyEvent(r));

            var e = (StartWithEncryptionKeyEvent)r.ToEvent();

            Assert.AreEqual(0, e.InstanceId);   // b/156451226
            Assert.AreEqual("instance-1", e.InstanceReference.Name);
            Assert.AreEqual("us-central1-a", e.InstanceReference.Zone);
            Assert.AreEqual("project-1", e.InstanceReference.ProjectId);
            Assert.AreEqual("ERROR", e.Severity);
            Assert.AreEqual(3, e.Status.Code);
            Assert.AreEqual("INVALID_ARGUMENT", e.Status.Message);
            Assert.AreEqual(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
        }
    }
}