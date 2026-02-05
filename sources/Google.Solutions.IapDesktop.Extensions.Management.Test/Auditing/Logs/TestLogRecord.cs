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
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.Auditing.Logs
{
    [TestFixture]
    public class TestLogRecord : ApplicationFixtureBase
    {
        [Test]
        public void Deserialize_WhenSystemEventJsonValid()
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
                     'request': {
                       '@type': 'type.googleapis.com/NotifyInstanceLocation'
                     },
                     'metadata': {
                       'serverId': 'b67639853d26e39b79a4fb306fd7d297',
                       'timestamp': '2020-03-23T10:35:09.025059Z',
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

            Assert.That(record.InsertId, Is.EqualTo("kj1zbe4j2eg"));
            Assert.That(record.LogName, Is.EqualTo("projects/project-1/logs/cloudaudit.googleapis.com%2Fsystem_event"));
            Assert.That(record.Severity, Is.EqualTo("INFO"));
            Assert.That(record.Timestamp, Is.EqualTo(new DateTime(2020, 3, 23, 10, 35, 10)));

            Assert.IsNotNull(record.Resource);
            Assert.That(record.Resource?.Type, Is.EqualTo("gce_instance"));
            Assert.That(record.Resource?.Labels?["project_id"], Is.EqualTo("project-1"));
            Assert.That(record.Resource?.Labels?["instance_id"], Is.EqualTo("22470777052"));
            Assert.That(record.Resource?.Labels?["zone"], Is.EqualTo("asia-east1-c"));

            Assert.IsNull(record.Operation);

            Assert.That(record.ProjectId, Is.EqualTo("project-1"));
            Assert.That(record.IsSystemEvent, Is.True);
            Assert.That(record.IsActivityEvent, Is.False);
        }

        [Test]
        public void Deserialize_WhenFirstActivityEventJsonValid()
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
                       'instance_id': '16752163'
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

            Assert.That(record.InsertId, Is.EqualTo("-oyhqund58si"));
            Assert.That(record.LogName, Is.EqualTo("projects/project-1/logs/cloudaudit.googleapis.com%2Factivity"));
            Assert.That(record.Severity, Is.EqualTo("NOTICE"));
            Assert.That(record.Timestamp, Is.EqualTo(new DateTime(2020, 5, 11, 1, 26, 36, 738)));

            Assert.IsNotNull(record.Resource);
            Assert.That(record.Resource?.Type, Is.EqualTo("gce_instance"));
            Assert.That(record.Resource?.Labels?["project_id"], Is.EqualTo("project-1"));
            Assert.That(record.Resource?.Labels?["instance_id"], Is.EqualTo("16752163"));
            Assert.That(record.Resource?.Labels?["zone"], Is.EqualTo("us-central1-b"));

            Assert.That(record.Operation?.Id, Is.EqualTo("operation-1589160396842-5a5553cf1e7c8-796d6bb5-473f0464"));
            Assert.That(record.Operation?.IsFirst, Is.True);
            Assert.That(record.Operation?.IsLast, Is.False);

            Assert.That(record.ProjectId, Is.EqualTo("project-1"));
            Assert.That(record.IsSystemEvent, Is.False);
            Assert.That(record.IsActivityEvent, Is.True);
        }

        [Test]
        public void Deserialize_WhenLastActivityEventJsonValid()
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
                    'callerIp': '1.2.3.4',
                    'callerSuppliedUserAgent': 'google-cloud)',
                    'requestAttributes': {},
                    'destinationAttributes': {}
                    },
                    'serviceName': 'compute.googleapis.com',
                    'methodName': 'v1.compute.instances.insert',
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                    '@type': 'type.googleapis.com/compute.instances.insert'
                    }
                },
                'insertId': '-vwncp9d6006',
                'resource': {
                    'type': 'gce_instance',
                    'labels': {
                    'instance_id': '1123123123',
                    'project_id': 'project-1',
                    'zone': 'us-central1-a'
                    }
                },
                'timestamp': '2020-04-24T08:13:39.103Z',
                'severity': 'ERROR',
                'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                'operation': {
                    'id': 'operation-1587715943067-5a404ecca6fa4-dc7e343f-dbc3ca83',
                    'producer': 'compute.googleapis.com',
                    'last': true
                },
                'receiveTimestamp': '2020-04-24T08:13:40.134230447Z'
                }
            ";

            var record = LogRecord.Deserialize(json)!;

            Assert.That(record.InsertId, Is.EqualTo("-vwncp9d6006"));

            Assert.That(record.Operation?.IsFirst, Is.False);
            Assert.That(record.Operation?.IsLast, Is.True);

            Assert.That(record.ProjectId, Is.EqualTo("project-1"));
            Assert.That(record.IsSystemEvent, Is.False);
            Assert.That(record.IsActivityEvent, Is.True);
        }
    }
}
