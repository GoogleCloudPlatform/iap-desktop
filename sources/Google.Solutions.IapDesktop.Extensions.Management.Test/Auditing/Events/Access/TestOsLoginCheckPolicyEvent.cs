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

using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.Access;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.Auditing.Events.Access
{
    [TestFixture]
    public class TestOsLoginCheckPolicyEvent : ApplicationFixtureBase
    {
        [Test]
        public void WhenCheckSucceeded_ThenFieldsAreExtracted(
            [Values("v1", "v1beta")] string version)
        {
            var json = @"
                {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'authenticationInfo': {
                      'principalEmail': 'bob@example.com'
                    },
                    'requestMetadata': {
                    },
                    'serviceName': 'oslogin.googleapis.com',
                    'methodName': 'google.cloud.oslogin.v1.OsLoginService.CheckPolicy',
                    'authorizationInfo': [
                      {
                        'resource': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                        'granted': true
                      }
                    ],
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                      'email': '10315820000000000000',
                      'policy': 'LOGIN',
                      'projectId': 'project-1',
                      'serviceAccount': '884959770000-compute@developer.gserviceaccount.com',
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.CheckPolicyRequest',
                      'zone': 'us-central1-a',
                      'instance': 'instance-1',
                      'numericProjectId': '884959770000'
                    },
                    'response': {
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.CheckPolicyResponse',
                      'success': true
                    }
                  },
                  'insertId': 'p9fqrhcly1',
                  'resource': {
                    'type': 'audited_resource',
                    'labels': {
                      'method': 'google.cloud.oslogin.v1.OsLoginService.CheckPolicy',
                      'project_id': 'project-1',
                      'service': 'oslogin.googleapis.com'
                    }
                  },
                  'timestamp': '2021-11-10T06:54:56.704058Z',
                  'severity': 'INFO',
                  'labels': {
                    'zone': 'ignoreme',
                    'instance_id': '1234567890'
                  },
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
                  'receiveTimestamp': '2021-11-10T06:54:57.478165572Z'
                }".Replace("v1", version);

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(OsLoginCheckPolicyEvent.IsStartOsLoginCheckPolicyEvent(r));

            var e = (OsLoginCheckPolicyEvent)r.ToEvent();

            Assert.AreEqual("INFO", e.Severity);
            Assert.AreEqual("project-1", e.InstanceReference.ProjectId);
            Assert.AreEqual("us-central1-a", e.InstanceReference.Zone);
            Assert.AreEqual("instance-1", e.InstanceReference.Name);
            Assert.AreEqual(1234567890, e.InstanceId);
            Assert.AreEqual("bob@example.com", e.Principal);
            Assert.IsTrue(e.IsSuccess);
            Assert.AreEqual("OS Login access for bob@example.com and policy LOGIN granted", e.Message);
        }

        [Test]
        public void WhenLegacyLogRecordLacksTopLevelLabels_ThenFieldsAreExtracted()
        {
            var json = @"
                {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'authenticationInfo': {
                      'principalEmail': 'bob@example.com'
                    },
                    'requestMetadata': {
                    },
                    'serviceName': 'oslogin.googleapis.com',
                    'methodName': 'google.cloud.oslogin.v1.OsLoginService.CheckPolicy',
                    'authorizationInfo': [
                      {
                        'resource': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                        'granted': true
                      }
                    ],
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                      'email': '10315820000000000000',
                      'policy': 'LOGIN',
                      'projectId': 'project-1',
                      'serviceAccount': '884959770000-compute@developer.gserviceaccount.com',
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.CheckPolicyRequest',
                      'zone': 'us-central1-a',
                      'instance': 'instance-1',
                      'numericProjectId': '884959770000'
                    },
                    'response': {
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.CheckPolicyResponse',
                      'success': true
                    }
                  },
                  'insertId': 'p9fqrhcly1',
                  'resource': {
                    'type': 'audited_resource',
                    'labels': {
                      'method': 'google.cloud.oslogin.v1.OsLoginService.CheckPolicy',
                      'project_id': 'project-1',
                      'service': 'oslogin.googleapis.com'
                    }
                  },
                  'timestamp': '2021-11-10T06:54:56.704058Z',
                  'severity': 'INFO',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
                  'receiveTimestamp': '2021-11-10T06:54:57.478165572Z'
                }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(OsLoginCheckPolicyEvent.IsStartOsLoginCheckPolicyEvent(r));

            var e = (OsLoginCheckPolicyEvent)r.ToEvent();

            Assert.AreEqual("us-central1-a", e.InstanceReference.Zone);
            Assert.AreEqual(0, e.InstanceId);
        }

        [Test]
        public void WhenCheckFailed_ThenFieldsAreExtracted()
        {
            var json = @"
                {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'authenticationInfo': {
                      'principalEmail': 'bob@example.com'
                    },
                    'requestMetadata': {
                    },
                    'serviceName': 'oslogin.googleapis.com',
                    'methodName': 'google.cloud.oslogin.v1.OsLoginService.CheckPolicy',
                    'authorizationInfo': [
                      {
                        'resource': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                        'granted': true
                      }
                    ],
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                      'policy': 'LOGIN',
                      'instance': 'instance-1',
                      'serviceAccount': '88495000-compute@developer.gserviceaccount.com',
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.CheckPolicyRequest',
                      'numericProjectId': '884959000',
                      'zone': 'us-central1-a',
                      'email': '107547574000',
                      'projectId': 'project-1'
                    }
                  },
                  'insertId': '1ultdnncw78',
                  'resource': {
                    'type': 'audited_resource',
                    'labels': {
                      'service': 'oslogin.googleapis.com',
                      'method': 'google.cloud.oslogin.v1.OsLoginService.CheckPolicy',
                      'project_id': 'project-1'
                    }
                  },
                  'timestamp': '2021-11-10T07:27:23.512514Z',
                  'severity': 'INFO',
                  'labels': {
                    'zone': 'ignoreme',
                    'instance_id': '1234567890'
                  },
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
                  'receiveTimestamp': '2021-11-10T07:27:24.463212211Z'
                }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(OsLoginCheckPolicyEvent.IsStartOsLoginCheckPolicyEvent(r));

            var e = (OsLoginCheckPolicyEvent)r.ToEvent();

            Assert.AreEqual("INFO", e.Severity);
            Assert.AreEqual("project-1", e.InstanceReference.ProjectId);
            Assert.AreEqual("us-central1-a", e.InstanceReference.Zone);
            Assert.AreEqual("instance-1", e.InstanceReference.Name);
            Assert.AreEqual(1234567890, e.InstanceId);
            Assert.AreEqual("bob@example.com", e.Principal);
            Assert.IsFalse(e.IsSuccess);
            Assert.AreEqual("OS Login access for bob@example.com and policy LOGIN denied", e.Message);
        }

        [Test]
        public void WhenCheckFailedAndResponsMissing_ThenFieldsAreExtracted()
        {
            var json = @"
                {
                  'protoPayload': {
                    '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                    'authenticationInfo': {
                      'principalEmail': 'bob@example.com'
                    },
                    'requestMetadata': {
                      'callerIp': '2002:a61:a294::',
                      'callerSuppliedUserAgent': 'stubby_client'
                    },
                    'serviceName': 'oslogin.googleapis.com',
                    'methodName': 'google.cloud.oslogin.v1.OsLoginService.CheckPolicy',
                    'authorizationInfo': [
                      {
                        'resource': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                        'granted': true
                      }
                    ],
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                      'policy': 'LOGIN',
                      'instance': 'instance-1',
                      'serviceAccount': '88495000-compute@developer.gserviceaccount.com',
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.CheckPolicyRequest',
                      'numericProjectId': '884959000',
                      'zone': 'us-central1-a',
                      'email': '107547574000',
                      'projectId': 'project-1'
                    },
                    'response': {
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.CheckPolicyResponse',
                      'success': false
                    }
                  },
                  'insertId': '1ultdnncw78',
                  'resource': {
                    'type': 'audited_resource',
                    'labels': {
                      'service': 'oslogin.googleapis.com',
                      'method': 'google.cloud.oslogin.v1.OsLoginService.CheckPolicy',
                      'project_id': 'project-1'
                    }
                  },
                  'timestamp': '2021-11-10T07:27:23.512514Z',
                  'severity': 'INFO',
                  'labels': {
                    'zone': 'ignoreme',
                    'instance_id': '1234567890'
                  },
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
                  'receiveTimestamp': '2021-11-10T07:27:24.463212211Z'
                }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(OsLoginCheckPolicyEvent.IsStartOsLoginCheckPolicyEvent(r));

            var e = (OsLoginCheckPolicyEvent)r.ToEvent();

            Assert.AreEqual("INFO", e.Severity);
            Assert.AreEqual("project-1", e.InstanceReference.ProjectId);
            Assert.AreEqual("us-central1-a", e.InstanceReference.Zone);
            Assert.AreEqual("instance-1", e.InstanceReference.Name);
            Assert.AreEqual(1234567890, e.InstanceId);
            Assert.AreEqual("bob@example.com", e.Principal);
            Assert.IsFalse(e.IsSuccess);
            Assert.AreEqual("OS Login access for bob@example.com and policy LOGIN denied", e.Message);
        }
    }
}
