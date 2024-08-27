//
// Copyright 2019 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional NOTICErmation
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

using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.Access;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.Auditing.Events.Access
{
    [TestFixture]
    public class TestSetMetadataEvent : ApplicationFixtureBase
    {
        [Test]
        public void WhenOperationIsFirstAndRecordContainsWindowsKeys_ThenFieldsAreExtracted()
        {
            var json = @"
             {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'authenticationInfo': {
                   'principalEmail': 'user@example.com',
                 },
                 'requestMetadata': {
                   'callerIp': '1.2.3.4',
                   'callerSuppliedUserAgent': 'IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)',
                 },
                 'serviceName': 'compute.googleapis.com',
                 'methodName': 'v1.compute.instances.setMetadata',
                 'authorizationInfo': [
                   {
                     'permission': 'compute.instances.setMetadata',
                     'granted': true,
                     'resourceAttributes': {
                       'service': 'compute',
                       'name': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                       'type': 'compute.instances'
                     }
                   }
                 ],
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                   '@type': 'type.googleapis.com/compute.instances.setMetadata'
                 },
                 'response': {
                   'operationType': 'setMetadata',
                   'progress': '0',
                   'targetId': '20008111111111111',
                   'user': 'user@example.com',
                   '@type': 'type.googleapis.com/operation'
                 },
                 'metadata': {
                   'instanceMetadataDelta': {
                     'modifiedMetadataKeys': [
                       'windows-keys'
                     ]
                   },
                   '@type': 'type.googleapis.com/google.cloud.audit.GceInstanceAuditMetadata'
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': 'bnzqs0e3nris',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'zone': 'us-central1-a',
                   'project_id': 'project-1',
                   'instance_id': '20008111111111111'
                 }
               },
               'timestamp': '2020-10-07T05:52:20.573709Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'producer': 'compute.googleapis.com',
                 'first': true
               },
               'receiveTimestamp': '2020-10-07T05:52:21.557770447Z'
             }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetMetadataEvent.IsSetMetadataEvent(r));

            var e = (SetMetadataEvent)r.ToEvent();

            Assert.AreEqual("user@example.com", e.Principal);
            Assert.AreEqual(20008111111111111, e.InstanceId);
            Assert.AreEqual("us-central1-a", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.IsNull(e.Status);

            Assert.AreEqual("1.2.3.4", e.SourceHost);
            Assert.AreEqual("IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)", e.UserAgent);

            Assert.AreEqual("Windows credential update from 1.2.3.4 using IAP-Desktop/1.0.1.0 (operation started)", e.Message);
        }

        [Test]
        public void WhenOperationIsLastAndRecordContainsWindowsKeys_ThenFieldsAreExtracted()
        {
            var json = @"
             {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'authenticationInfo': {
                   'principalEmail': 'user@example.com'
                 },
                 'requestMetadata': {
                 },
                 'serviceName': 'compute.googleapis.com',
                 'methodName': 'v1.compute.instances.setMetadata',
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                   '@type': 'type.googleapis.com/compute.instances.setMetadata',
                   'Metadata Keys Modified': [
                     'windows-keys'
                   ]
                 },
                 'metadata': {
                   '@type': 'type.googleapis.com/google.cloud.audit.GceInstanceAuditMetadata',
                   'instanceMetadataDelta': {
                     'addedMetadataKeys': [
                       'windows-keys'
                     ]
                   }
                 }
               },
               'insertId': 'r7acnzdkgve',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'project_id': 'project-1',
                   'instance_id': '20008111111111111',
                   'zone': 'us-central1-a'
                 }
               },
               'timestamp': '2020-10-07T05:52:24.332835Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'producer': 'compute.googleapis.com',
                 'last': true
               },
               'receiveTimestamp': '2020-10-07T05:52:24.881768477Z'
             }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetMetadataEvent.IsSetMetadataEvent(r));

            var e = (SetMetadataEvent)r.ToEvent();

            Assert.AreEqual("user@example.com", e.Principal);
            Assert.AreEqual(20008111111111111, e.InstanceId);
            Assert.AreEqual("us-central1-a", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.IsNull(e.Status);

            Assert.IsNull(e.SourceHost);
            Assert.IsNull(e.UserAgent);

            Assert.AreEqual("Windows credential update from (unknown) using (unknown agent) (operation completed)", e.Message);
        }

        [Test]
        public void WhenOperationIsLastAndRecordContainsSshKeys_ThenFieldsAreExtracted()
        {
            var json = @"
             {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'authenticationInfo': {
                   'principalEmail': 'user@example.com',
                 },
                 'requestMetadata': {
                   'callerIp': '1.2.3.4',
                   'callerSuppliedUserAgent': 'IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)',
                 },
                 'serviceName': 'compute.googleapis.com',
                 'methodName': 'v1.compute.instances.setMetadata',
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-a',
                 'request': {
                   'Metadata Keys Added': [
                     'ssh-keys'
                   ],
                   '@type': 'type.googleapis.com/compute.instances.setMetadata'
                 },
                 'metadata': {
                   '@type': 'type.googleapis.com/google.cloud.audit.GceInstanceAuditMetadata',
                   'instanceMetadataDelta': {
                     'addedMetadataKeys': [
                       'ssh-keys'
                     ]
                   }
                 }
               },
               'insertId': '-8b5rzjcui4',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'project_id': 'project-1',
                   'instance_id': '37848154511111',
                   'zone': 'us-central1-a'
                 }
               },
               'timestamp': '2020-10-08T14:10:30.078247Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'producer': 'compute.googleapis.com',
                 'last': true
               },
               'receiveTimestamp': '2020-10-08T14:10:30.777783607Z'
             }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetMetadataEvent.IsSetMetadataEvent(r));

            var e = (SetMetadataEvent)r.ToEvent();

            Assert.AreEqual("user@example.com", e.Principal);
            Assert.AreEqual(37848154511111, e.InstanceId);
            Assert.AreEqual("us-central1-a", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.IsNull(e.Status);

            Assert.AreEqual("1.2.3.4", e.SourceHost);
            Assert.AreEqual("IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)", e.UserAgent);

            Assert.AreEqual("Linux SSH keys update from 1.2.3.4 using IAP-Desktop/1.0.1.0 (operation completed)", e.Message);
        }

        [Test]
        public void WhenSeverityIsError_ThenFieldsAreExtracted()
        {
            var json = @"
             {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'status': {
                   'code': 7,
                   'message': 'Required ...'
                 },
                 'authenticationInfo': {
                   'principalEmail': 'user@example.com',
                 },
                 'requestMetadata': {
                   'callerIp': '1.2.3.4',
                   'callerSuppliedUserAgent': 'IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)',
                 },
                 'serviceName': 'compute.googleapis.com',
                 'methodName': 'v1.compute.instances.setMetadata',
                 'authorizationInfo': [
                   {
                     'permission': 'compute.instances.setMetadata',
                     'resourceAttributes': {
                       'service': 'compute',
                       'name': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                       'type': 'compute.instances'
                     }
                   }
                 ],
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                   '@type': 'type.googleapis.com/compute.instances.setMetadata'
                 },
                 'response': {
                   'error': { },
                   '@type': 'type.googleapis.com/error'
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': '-cgz01ge2d7zg',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'project_id': 'project-1',
                   'instance_id': '',
                   'zone': 'us-central1-a'
                 }
               },
               'timestamp': '2020-10-06T09:22:13.252691Z',
               'severity': 'ERROR',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'receiveTimestamp': '2020-10-06T09:22:14.159875014Z'
             }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetMetadataEvent.IsSetMetadataEvent(r));

            var e = (SetMetadataEvent)r.ToEvent();

            Assert.AreEqual("user@example.com", e.Principal);
            Assert.AreEqual(0, e.InstanceId);
            Assert.AreEqual("us-central1-a", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual("ERROR", e.Severity);
            Assert.AreEqual(7, e.Status?.Code);
            Assert.AreEqual("Required ...", e.Status?.Message);

            Assert.AreEqual("1.2.3.4", e.SourceHost);
            Assert.AreEqual("IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)", e.UserAgent);

            Assert.AreEqual("Metadata, Windows credentials, or SSH key update from 1.2.3.4 using IAP-Desktop/1.0.1.0 failed [Required ...]", e.Message);
        }

        [Test]
        public void WhenRecordContainsOtherMetadataKeys_ThenIsResetWindowsUserEventReturnsFalse()
        {
            var json = @"
             {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'authenticationInfo': {
                   'principalEmail': 'user@example.com',
                 },
                 'requestMetadata': {
                   'callerIp': '1.2.3.4',
                   'callerSuppliedUserAgent': 'IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)',
                 },
                 'serviceName': 'compute.googleapis.com',
                 'methodName': 'v1.compute.instances.setMetadata',
                 'authorizationInfo': [
                   {
                     'permission': 'compute.instances.setMetadata',
                     'granted': true,
                     'resourceAttributes': {
                       'service': 'compute',
                       'name': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                       'type': 'compute.instances'
                     }
                   }
                 ],
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                   '@type': 'type.googleapis.com/compute.instances.setMetadata'
                 },
                 'response': {
                   'operationType': 'setMetadata',
                   'progress': '0',
                   'targetId': '20008111111111111',
                   'user': 'user@example.com',
                   '@type': 'type.googleapis.com/operation'
                 },
                 'metadata': {
                   'instanceMetadataDelta': {
                     'modifiedMetadataKeys': [
                     ]
                   },
                   '@type': 'type.googleapis.com/google.cloud.audit.GceInstanceAuditMetadata'
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': 'bnzqs0e3nris',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'zone': 'us-central1-a',
                   'project_id': 'project-1',
                   'instance_id': '20008111111111111'
                 }
               },
               'timestamp': '2020-10-07T05:52:20.573709Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'producer': 'compute.googleapis.com',
                 'first': true
               },
               'receiveTimestamp': '2020-10-07T05:52:21.557770447Z'
             }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(SetMetadataEvent.IsSetMetadataEvent(r));

            var e = (SetMetadataEvent)r.ToEvent();

            Assert.AreEqual("user@example.com", e.Principal);
            Assert.AreEqual(20008111111111111, e.InstanceId);
            Assert.AreEqual("us-central1-a", e.InstanceReference?.Zone);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.IsNull(e.Status);

            Assert.AreEqual("1.2.3.4", e.SourceHost);
            Assert.AreEqual("IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)", e.UserAgent);

            Assert.AreEqual("Metadata update from 1.2.3.4 using IAP-Desktop/1.0.1.0 (operation started)", e.Message);
        }
    }
}
