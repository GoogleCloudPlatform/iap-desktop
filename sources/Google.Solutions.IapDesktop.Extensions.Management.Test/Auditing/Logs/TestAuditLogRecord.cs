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

using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using Google.Solutions.Testing.Application.Test;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.Auditing.Logs
{
    [TestFixture]
    public class TestAuditLogRecord : ApplicationFixtureBase
    {
        [Test]
        public void Deserialize_WhenJsonContainsAuditLogRecord()
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

            var record = LogRecord.Deserialize(json)!;

            Assert.That(record.ProtoPayload, Is.InstanceOf<AuditLogRecord>());
            var auditLog = (AuditLogRecord)record.ProtoPayload!;

            Assert.That(auditLog.AuthenticationInfo?.PrincipalEmail, Is.EqualTo("system@google.com"));
            Assert.That(auditLog.ServiceName, Is.EqualTo("compute.googleapis.com"));
            Assert.That(auditLog.MethodName, Is.EqualTo("NotifyInstanceLocation"));
            Assert.That(auditLog.ResourceName, Is.EqualTo("foo"));
        }

        [Test]
        public void Deserialize_WhenJsonContainsMetadata()
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

            var record = LogRecord.Deserialize(json)!;

            Assert.That(record.ProtoPayload, Is.InstanceOf<AuditLogRecord>());
            var auditLog = (AuditLogRecord)record.ProtoPayload!;

            Assert.That(auditLog.Metadata, Is.Not.Null);
            Assert.That(auditLog.Metadata?["serverId"]?.Value<string>(), Is.EqualTo("b67639853d26e39b79a4fb306fd7d297"));
            Assert.That(auditLog.Metadata?["timestamp"]?.Value<DateTime>(), Is.EqualTo(new DateTime(2020, 3, 23, 10, 35, 9)));
        }

        [Test]
        public void Deserialize_WhenJsonContainsRequest()
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

            var record = LogRecord.Deserialize(json)!;

            Assert.That(record.ProtoPayload, Is.InstanceOf<AuditLogRecord>());
            var auditLog = (AuditLogRecord)record.ProtoPayload!;

            Assert.That(auditLog.Request, Is.Not.Null);
            Assert.That(auditLog.Request?["name"]?.Value<string>(), Is.EqualTo("my-managed-group2-f3ng"));
            Assert.That(auditLog.Request?["canIpForward"]?.Value<bool>(), Is.EqualTo(false));
        }

        [Test]
        public void Deserialize_WhenJsonContainsResponse()
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

            var record = LogRecord.Deserialize(json)!;

            Assert.That(record.ProtoPayload, Is.InstanceOf<AuditLogRecord>());
            var auditLog = (AuditLogRecord)record.ProtoPayload!;

            Assert.That(auditLog.Response, Is.Not.Null);
            Assert.That(auditLog.Response?["id"]?.Value<string>(), Is.EqualTo("2713656233496483618"));
            Assert.That(auditLog.Response?["name"]?.Value<string>(), Is.EqualTo("operation-1589"));
        }
    }
}
