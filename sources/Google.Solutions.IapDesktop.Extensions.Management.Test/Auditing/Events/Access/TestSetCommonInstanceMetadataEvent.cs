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
    public class TestSetCommonInstanceMetadataEvent : ApplicationFixtureBase
    {
        [Test]
        public void ToEvent_WhenOperationIsFirst()
        {
            var json = @"
                {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'authenticationInfo': {
                      'principalEmail': 'user@example.com'
                    },
                    'requestMetadata': {
                      'callerIp': '1.2.3.4',
                      'callerSuppliedUserAgent': 'IAP-Desktop/1.0.1.0',
                    },
                    'serviceName': 'compute.googleapis.com',
                    'methodName': 'v1.compute.projects.setCommonInstanceMetadata',
                    'authorizationInfo': [
                      {
                        'permission': 'compute.projects.setCommonInstanceMetadata',
                        'granted': true,
                        'resourceAttributes': {
                          'service': 'compute',
                          'name': 'projects/project-1',
                          'type': 'compute.projects'
                        }
                      }
                    ],
                    'resourceName': 'projects/project-1',
                    'request': {
                      '@type': 'type.googleapis.com/compute.projects.setCommonInstanceMetadata'
                    },
                    'response': {
                      'operationType': 'compute.projects.setCommonInstanceMetadata',
                      'user': 'bob@example.com',
                      'insertTime': '2021-03-24T02:59:22.000-07:00',
                      'name': 'operation-1616579961482-5be455a5aee52-be810183-fc366490',
                      'selfLinkWithId': 'https://www.googleapis.com/compute/v1/projects/project-1/global/operations/777842148836',
                      'status': 'RUNNING',
                      'progress': '0',
                      '@type': 'type.googleapis.com/operation',
                      'selfLink': 'https://www.googleapis.com/compute/v1/projects/project-1/global/operations/operation-16165799',
                      'startTime': '2021-03-24T02:59:22.004-07:00',
                      'targetId': '123',
                      'targetLink': 'https://www.googleapis.com/compute/v1/projects/project-1',
                      'id': '7778421488'
                    },
                    'resourceLocation': {
                      'currentLocations': [
                        'global'
                      ]
                    }
                  },
                  'insertId': 'h7dimrc14e',
                  'resource': {
                    'type': 'gce_project',
                    'labels': {
                      'project_id': '123'
                    }
                  },
                  'timestamp': '2021-03-24T09:59:21.610017Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1616579961482-5be455a5aee52-be8',
                    'producer': 'compute.googleapis.com',
                    'first': true
                  },
                  'receiveTimestamp': '2021-03-24T09:59:22.809077586Z'
                }
              ";

            var r = LogRecord.Deserialize(json)!;
            Assert.That(SetCommonInstanceMetadataEvent.IsSetCommonInstanceMetadataEvent(r), Is.True);

            var e = (SetCommonInstanceMetadataEvent)r.ToEvent();

            Assert.That(e.Principal, Is.EqualTo("user@example.com"));
            Assert.That(e.ProjectId, Is.EqualTo("project-1"));
            Assert.That(e.Severity, Is.EqualTo("NOTICE"));
            Assert.That(e.Status, Is.Null);

            Assert.That(e.SourceHost, Is.EqualTo("1.2.3.4"));
            Assert.That(e.UserAgent, Is.EqualTo("IAP-Desktop/1.0.1.0"));

            Assert.That(
                e.Message, Is.EqualTo("Linux SSH keys or metadata update from 1.2.3.4 using IAP-Desktop/1.0.1.0 (operation started)"));
        }

        [Test]
        public void ToEvent_WhenOperationIsLastAndLacksModifiedFields()
        {
            var json = @"
                {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'authenticationInfo': {
                      'principalEmail': 'user@example.com'
                    },
                    'serviceName': 'compute.googleapis.com',
                    'methodName': 'v1.compute.projects.setCommonInstanceMetadata',
                    'resourceName': 'projects/project-1',
                    'request': {
                      '@type': 'type.googleapis.com/compute.projects.setCommonInstanceMetadata'
                    }
                  },
                  'insertId': 'a9re9fd7yhq',
                  'resource': {
                    'type': 'gce_project',
                    'labels': {
                      'project_id': '123'
                    }
                  },
                  'timestamp': '2021-03-24T09:59:46.832678Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1616579961482-5be455a5ae',
                    'producer': 'compute.googleapis.com',
                    'last': true
                  },
                  'receiveTimestamp': '2021-03-24T09:59:47.688249630Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.That(SetCommonInstanceMetadataEvent.IsSetCommonInstanceMetadataEvent(r), Is.True);

            var e = (SetCommonInstanceMetadataEvent)r.ToEvent();

            Assert.That(e.Principal, Is.EqualTo("user@example.com"));
            Assert.That(e.ProjectId, Is.EqualTo("project-1"));
            Assert.That(e.Severity, Is.EqualTo("NOTICE"));
            Assert.That(e.Status, Is.Null);

            Assert.That(e.SourceHost, Is.Null);
            Assert.That(e.UserAgent, Is.Null);

            Assert.That(e.Message, Is.EqualTo("Linux SSH keys or metadata update from (unknown) using (unknown agent) (operation completed)"));
        }



        [Test]
        public void ToEvent_WhenOperationIsLastAndIncludesModifiedFields()
        {
            var json = @"
                {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'authenticationInfo': {
                      'principalEmail': 'user@example.com'
                    },
                    'serviceName': 'compute.googleapis.com',
                    'methodName': 'v1.compute.projects.setCommonInstanceMetadata',
                    'resourceName': 'projects/project-1',
                    'request': {
                      '@type': 'type.googleapis.com/compute.projects.setCommonInstanceMetadata'
                    },
                    'metadata': {
                      '@type': 'type.googleapis.com/google.cloud.audit.GceProjectAuditMetadata',
                      'projectMetadataDelta': {
                        'addedMetadataKeys': ['foo'],
                        'modifiedMetadataKeys': ['ssh-keys']
                      }
                    }
                  },
                  'insertId': 'a9re9fd7yhq',
                  'resource': {
                    'type': 'gce_project',
                    'labels': {
                      'project_id': '123'
                    }
                  },
                  'timestamp': '2021-03-24T09:59:46.832678Z',
                  'severity': 'NOTICE',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'operation': {
                    'id': 'operation-1616579961482-5be455a5ae',
                    'producer': 'compute.googleapis.com',
                    'last': true
                  },
                  'receiveTimestamp': '2021-03-24T09:59:47.688249630Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.That(SetCommonInstanceMetadataEvent.IsSetCommonInstanceMetadataEvent(r), Is.True);

            var e = (SetCommonInstanceMetadataEvent)r.ToEvent();

            Assert.That(e.Principal, Is.EqualTo("user@example.com"));
            Assert.That(e.ProjectId, Is.EqualTo("project-1"));
            Assert.That(e.Severity, Is.EqualTo("NOTICE"));
            Assert.That(e.Status, Is.Null);

            Assert.That(e.SourceHost, Is.Null);
            Assert.That(e.UserAgent, Is.Null);

            Assert.That(e.Message, Is.EqualTo("Linux SSH keys update from (unknown) using (unknown agent) (operation completed)"));
        }

        [Test]
        public void ToEvent_WhenSeverityIsError()
        {
            var json = @"
                {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'status': {
                      'code': 7,
                      'message': 'Required iam.serviceAccounts.actAs permission for projects/project-1'
                    },
                    'authenticationInfo': {
                      'principalEmail': 'user@example.com'
                    },
                    'requestMetadata': {
                      'callerIp': '1.2.3.4',
                      'callerSuppliedUserAgent': 'IAP-Desktop/1.1',
                      'callerNetwork': '//compute.googleapis.com/projects/project-1/global/networks/__unknown__',
                      'destinationAttributes': {}
                    },
                    'serviceName': 'compute.googleapis.com',
                    'methodName': 'v1.compute.projects.setCommonInstanceMetadata',
                    'authorizationInfo': [
                      {
                        'permission': 'compute.projects.setCommonInstanceMetadata',
                        'granted': true,
                        'resourceAttributes': {
                          'service': 'compute',
                          'name': 'projects/project-1',
                          'type': 'compute.projects'
                        }
                      }
                    ],
                    'resourceName': 'projects/project-1',
                    'request': {
                      '@type': 'type.googleapis.com/compute.projects.setCommonInstanceMetadata'
                    },
                    'response': {
                      'error': {
                        'message': 'Required iam.serviceAccounts.actAs permission for projects/project-1',
                        'errors': [
                          {
                            'reason': 'forbidden',
                            'domain': 'global',
                            'message': 'Required iam.serviceAccounts.actAs permission for projects/project-1'
                          }
                        ],
                        'code': 403
                      },
                      '@type': 'type.googleapis.com/error'
                    },
                    'resourceLocation': {
                      'currentLocations': [
                        'global'
                      ]
                    }
                  },
                  'insertId': '-yybg9jdyltq',
                  'resource': {
                    'type': 'gce_project',
                    'labels': {
                      'project_id': ''
                    }
                  },
                  'timestamp': '2021-03-11T15:33:35.267517Z',
                  'severity': 'ERROR',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                  'receiveTimestamp': '2021-03-11T15:33:35.703168353Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.That(SetCommonInstanceMetadataEvent.IsSetCommonInstanceMetadataEvent(r), Is.True);

            var e = (SetCommonInstanceMetadataEvent)r.ToEvent();

            Assert.That(e.Principal, Is.EqualTo("user@example.com"));
            Assert.That(e.ProjectId, Is.EqualTo("project-1"));
            Assert.That(e.Severity, Is.EqualTo("ERROR"));
            Assert.That(e.Status?.Code, Is.EqualTo(7));
            Assert.That(e.Status?.Message, Is.EqualTo("Required iam.serviceAccounts.actAs permission for projects/project-1"));

            Assert.That(e.SourceHost, Is.EqualTo("1.2.3.4"));
            Assert.That(e.UserAgent, Is.EqualTo("IAP-Desktop/1.1"));

            Assert.That(
                e.Message, Is.EqualTo("Linux SSH keys or metadata update from 1.2.3.4 using IAP-Desktop/1.1 failed " +
                "[Required iam.serviceAccounts.actAs permission for projects/project-1]"));
        }
    }
}
