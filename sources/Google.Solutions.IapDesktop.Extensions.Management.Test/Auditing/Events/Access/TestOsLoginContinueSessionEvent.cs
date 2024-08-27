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
    public class TestOsLoginContinueSessionEven : ApplicationFixtureBase
    {
        [Test]
        public void WhenChallengePending_ThenFieldsAreExtracted(
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
                    'methodName': 'google.cloud.oslogin.v1.OsLoginService.ContinueSession',
                    'authorizationInfo': [
                      {
                        'resource': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                        'granted': true
                      }
                    ],
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.ContinueSessionRequest',
                      'email': '1100000000000000',
                      'action': 'RESPOND',
                      'numericProjectId': '8849500',
                      'challengeId': 2,
                      'sessionId': 'AM3QAYav2Bgo3L5'
                    },
                    'response': {
                      'sessionId': 'AM3QAYav2Bgo3L5',
                      'status': 'CHALLENGE_PENDING',
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.StartOrContinueSessionResponse',
                      'challenges': [
                        {
                          'challengeType': 'INTERNAL_TWO_FACTOR',
                          'status': 'READY',
                          'challengeId': 2
                        }
                      ]
                    }
                  },
                  'insertId': '7hubh5cvo9',
                  'resource': {
                    'type': 'audited_resource',
                    'labels': {
                      'service': 'oslogin.googleapis.com',
                      'project_id': 'project-1',
                      'method': 'google.cloud.oslogin.v1.OsLoginService.ContinueSession'
                    }
                  },
                  'timestamp': '2021-11-10T06:49:48.923896Z',
                  'severity': 'INFO',
                  'labels': {
                    'zone': 'ignoreme',
                    'instance_id': '1234567890'
                  },
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
                  'receiveTimestamp': '2021-11-10T06:49:50.169748838Z'
                }".Replace("v1", version);

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(OsLoginContinueSessionEvent.IsStartOsLoginContinueSessionEvent(r));

            var e = (OsLoginContinueSessionEvent)r.ToEvent();

            Assert.AreEqual("INFO", e.Severity);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual("us-central1-a", e.InstanceReference?.Zone);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual(1234567890, e.InstanceId);
            Assert.AreEqual("bob@example.com", e.Principal);
            Assert.AreEqual("CHALLENGE_PENDING", e.ChallengeStatus);
            Assert.AreEqual("Continue OS Login 2FA session for bob@example.com: CHALLENGE_PENDING", e.Message);
        }

        [Test]
        public void WhenAuthenticated_ThenFieldsAreExtracted()
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
                    'methodName': 'google.cloud.oslogin.v1.OsLoginService.ContinueSession',
                    'authorizationInfo': [
                      {
                        'resource': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                        'granted': true
                      }
                    ],
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.ContinueSessionRequest',
                      'numericProjectId': '8849500',
                      'sessionId': 'AM3QAYap37meA0G',
                      'email': '103158246741215050671',
                      'challengeId': 2,
                      'action': 'RESPOND'
                    },
                    'response': {
                      'status': 'AUTHENTICATED',
                      'sessionId': 'AM3QAYap37meA0G',
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.StartOrContinueSessionResponse'
                    }
                  },
                  'insertId': '1ultdnncvvk',
                  'resource': {
                    'type': 'audited_resource',
                    'labels': {
                      'service': 'oslogin.googleapis.com',
                      'method': 'google.cloud.oslogin.v1.OsLoginService.ContinueSession',
                      'project_id': 'project-1'
                    }
                  },
                  'timestamp': '2021-11-10T06:54:55.701510Z',
                  'severity': 'INFO',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
                  'receiveTimestamp': '2021-11-10T06:54:57.097840918Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(OsLoginContinueSessionEvent.IsStartOsLoginContinueSessionEvent(r));

            var e = (OsLoginContinueSessionEvent)r.ToEvent();

            Assert.AreEqual("INFO", e.Severity);
            Assert.AreEqual("project-1", e.InstanceReference?.ProjectId);
            Assert.AreEqual("us-central1-a", e.InstanceReference?.Zone);
            Assert.AreEqual("instance-1", e.InstanceReference?.Name);
            Assert.AreEqual("bob@example.com", e.Principal);
            Assert.AreEqual("AUTHENTICATED", e.ChallengeStatus);
            Assert.AreEqual("Continue OS Login 2FA session for bob@example.com: AUTHENTICATED", e.Message);
        }
    }
}
