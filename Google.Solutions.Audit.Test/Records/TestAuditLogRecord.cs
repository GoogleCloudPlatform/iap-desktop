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

using Google.Solutions.Audit.Records;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;

namespace Google.Solutions.Audit.Test.Records
{
    [TestFixture]
    public class TestAuditLogRecord
    {
        [Test]
        public void WhenJsonContainsAuditLogRecord_ThenAuditLogRecordIsDeserialized()
        {
            var json = @"
                 {
                   'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'authenticationInfo': {
                       'principalEmail': 'system@google.com'
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'NotifyInstanceLocation',
                     'resourceName': 'foo',
                     'request': {
                       '@type': 'type.googleapis.com/NotifyInstanceLocation'
                     },
                     'metadata': {
                       'serverId': 'b67639853d26e39b79a4fb306fd7d297',
                       'timestamp': '2020-03-23T10:35:09Z',
                       '@type': 'type.googleapis.com/google.cloud.audit.GceInstanceLocationMetadata'
                     }
                   },
                   'insertId': 'kj1zbe4j2eg',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'project_id': 'project-1',
                       'instance_id': '22470777052',
                       'zone': 'asia-east1-c'
                     }
                   },
                   'timestamp': '2020-03-23T10:35:10Z',
                   'severity': 'INFO',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fsystem_event',
                   'receiveTimestamp': '2020-03-23T10:35:11.269405964Z'
                 }";

            var record = LogRecord.Deserialize(json);

            Assert.IsInstanceOf<AuditLogRecord>(record.ProtoPayload);
            var auditLog = (AuditLogRecord)record.ProtoPayload;

            Assert.AreEqual("system@google.com", auditLog.AuthenticationInfo.PrincipalEmail);
            Assert.AreEqual("compute.googleapis.com", auditLog.ServiceName);
            Assert.AreEqual("NotifyInstanceLocation", auditLog.MethodName);
            Assert.AreEqual("foo", auditLog.ResourceName);
        }

        [Test]
        public void WhenJsonContainsMetadata_ThenMetadataIsDeserialized()
        {
            var json = @"
                 {
                   'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'authenticationInfo': {
                       'principalEmail': 'system@google.com'
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'NotifyInstanceLocation',
                     'resourceName': 'foo',
                     'request': {
                       '@type': 'type.googleapis.com/NotifyInstanceLocation'
                     },
                     'metadata': {
                       'serverId': 'b67639853d26e39b79a4fb306fd7d297',
                       'timestamp': '2020-03-23T10:35:09Z',
                       '@type': 'type.googleapis.com/google.cloud.audit.GceInstanceLocationMetadata'
                     }
                   },
                   'insertId': 'kj1zbe4j2eg',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'project_id': 'project-1',
                       'instance_id': '22470777052',
                       'zone': 'asia-east1-c'
                     }
                   },
                   'timestamp': '2020-03-23T10:35:10Z',
                   'severity': 'INFO',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fsystem_event',
                   'receiveTimestamp': '2020-03-23T10:35:11.269405964Z'
                 }";

            var record = LogRecord.Deserialize(json);

            Assert.IsInstanceOf<AuditLogRecord>(record.ProtoPayload);
            var auditLog = (AuditLogRecord)record.ProtoPayload;

            Assert.IsNotNull(auditLog.Metadata);
            Assert.AreEqual("b67639853d26e39b79a4fb306fd7d297", auditLog.Metadata["serverId"].Value<string>());
            Assert.AreEqual(new DateTime(2020, 3, 23, 10, 35, 9), auditLog.Metadata["timestamp"].Value<DateTime>());
        }

        [Test]
        public void WhenJsonContainsRequest_ThenRequestIsDeserialized()
        {
            var json = @"
                 {
                   'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'authenticationInfo': {
                       'principalEmail': '123234345@cloudservices.gserviceaccount.com'
                     },
                     'requestMetadata': {
                       'callerSuppliedUserAgent': 'GCE Managed Instance Group',
                       'requestAttributes': {
                         'time': '2020-05-11T01:26:37.924Z',
                         'reason': '8uSywAYkGiJGb3IgbG9uZy1ydW5uaW5nIGFyY3VzIG9wZXJhdGlvbnMu',
                         'auth': {}
                       },
                       'destinationAttributes': {}
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'v1.compute.instances.insert',
                     'authorizationInfo': [
                     ],
                     'resourceName': 'projects/123234345/zones/us-central1-b/instances/my-managed-group2-f3ng',
                     'request': {
                       'name': 'my-managed-group2-f3ng',
                       'machineType': 'projects/123234345/zones/us-central1-b/machineTypes/custom-4-8192',
                       'canIpForward': false,
                       'requestId': '15b235bf-0a1b-379d-9870-0b31bd4d4243',
                       '@type': 'type.googleapis.com/compute.instances.insert'
                     },
                     'response': {
                       'id': '2713656233496483618',
                       'name': 'operation-1589160396842-5a5553cf1e7c8-796d6bb5-473f0464',
       
                       '@type': 'type.googleapis.com/operation'
                     },
                     'resourceLocation': {
                       'currentLocations': [
                         'us-central1-b'
                       ]
                     }
                   },
                   'insertId': '-oyhqund58si',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'zone': 'us-central1-b',
                       'project_id': 'project-1',
                       'instance_id': '3416990581716752163'
                     }
                   },
                   'timestamp': '2020-05-11T01:26:36.738Z',
                   'severity': 'NOTICE',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                   'operation': {
                     'id': 'operation-1589160396842-5a5553cf1e7c8-796d6bb5-473f0464',
                     'producer': 'compute.googleapis.com',
                     'first': true
                   },
                   'receiveTimestamp': '2020-05-11T01:26:38.791926381Z'
                 }";

            var record = LogRecord.Deserialize(json);

            Assert.IsInstanceOf<AuditLogRecord>(record.ProtoPayload);
            var auditLog = (AuditLogRecord)record.ProtoPayload;

            Assert.IsNotNull(auditLog.Request);
            Assert.AreEqual("my-managed-group2-f3ng", auditLog.Request["name"].Value<string>());
            Assert.AreEqual(false, auditLog.Request["canIpForward"].Value<bool>());
        }



        [Test]
        public void WhenJsonContainsResponse_ThenResponseIsDeserialized()
        {
            var json = @"
                 {
                   'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'authenticationInfo': {
                       'principalEmail': '123234345@cloudservices.gserviceaccount.com'
                     },
                     'requestMetadata': {
                       'callerSuppliedUserAgent': 'GCE Managed Instance Group',
                       'requestAttributes': {
                         'time': '2020-05-11T01:26:37.924Z',
                         'reason': '8uSywAYkGiJGb3IgbG9uZy1ydW5uaW5nIGFyY3VzIG9wZXJhdGlvbnMu',
                         'auth': {}
                       },
                       'destinationAttributes': {}
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'v1.compute.instances.insert',
                     'authorizationInfo': [
                     ],
                     'resourceName': 'projects/123234345/zones/us-central1-b/instances/my-managed-group2-f3ng',
                     'request': {
                       'name': 'my-managed-group2-f3ng',
                       'machineType': 'projects/123234345/zones/us-central1-b/machineTypes/custom-4-8192',
                       'canIpForward': false,
                       'requestId': '15b235bf-0a1b-379d-9870-0b31bd4d4243',
                       '@type': 'type.googleapis.com/compute.instances.insert'
                     },
                     'response': {
                       'id': '2713656233496483618',
                       'name': 'operation-1589',
       
                       '@type': 'type.googleapis.com/operation'
                     },
                     'resourceLocation': {
                       'currentLocations': [
                         'us-central1-b'
                       ]
                     }
                   },
                   'insertId': '-oyhqund58si',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'zone': 'us-central1-b',
                       'project_id': 'project-1',
                       'instance_id': '3416990581716752163'
                     }
                   },
                   'timestamp': '2020-05-11T01:26:36.738Z',
                   'severity': 'NOTICE',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                   'operation': {
                     'id': 'operation-1589160396842-5a5553cf1e7c8-796d6bb5-473f0464',
                     'producer': 'compute.googleapis.com',
                     'first': true
                   },
                   'receiveTimestamp': '2020-05-11T01:26:38.791926381Z'
                 }";

            var record = LogRecord.Deserialize(json);

            Assert.IsInstanceOf<AuditLogRecord>(record.ProtoPayload);
            var auditLog = (AuditLogRecord)record.ProtoPayload;

            Assert.IsNotNull(auditLog.Response);
            Assert.AreEqual("2713656233496483618", auditLog.Response["id"].Value<string>());
            Assert.AreEqual("operation-1589", auditLog.Response["name"].Value<string>());
        }
    }
}
