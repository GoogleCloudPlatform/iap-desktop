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
    public class TestOsLoginStartSessionEvent : ApplicationFixtureBase
    {
        [Test]
        public void ToEvent_When2faMethodsAvailable(
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
                    'methodName': 'google.cloud.oslogin.v1.OsLoginService.StartSession',
                    'authorizationInfo': [
                      {
                        'resource': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                        'granted': true
                      }
                    ],
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.StartSessionRequest',
                      'supportedChallengeTypes': [
                        'INTERNAL_TWO_FACTOR',
                        'SECURITY_KEY_OTP',
                        'AUTHZEN',
                        'TOTP',
                        'IDV_PREREGISTERED_PHONE'
                      ],
                      'numericProjectId': '8849500',
                      'email': '1100000000000000'
                    },
                    'response': {
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.StartOrContinueSessionResponse',
                      'status': 'CHALLENGE_REQUIRED',
                      'sessionId': 'AM3QAYav2Bgo3L5Imsqz4oIbyLPYNMm_s7AuxPbUAb4LVg-SX22Z7KBcbwpt28UU',
                      'challenges': [
                        {
                          'challengeType': 'INTERNAL_TWO_FACTOR',
                          'status': 'READY',
                          'challengeId': 2
                        }
                      ]
                    }
                  },
                  'insertId': '1fvx9yocvsi',
                  'resource': {
                    'type': 'audited_resource',
                    'labels': {
                      'project_id': 'project-1',
                      'service': 'oslogin.googleapis.com',
                      'method': 'google.cloud.oslogin.v1.OsLoginService.StartSession'
                    }
                  },
                  'timestamp': '2021-11-10T06:49:38.025734Z',
                  'severity': 'INFO',
                  'labels': {
                    'zone': 'ignoreme',
                    'instance_id': '1234567890'
                  },
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
                  'receiveTimestamp': '2021-11-10T06:49:39.615871551Z'
                }".Replace("v1", version);

            var r = LogRecord.Deserialize(json)!;
            Assert.That(OsLoginStartSessionEvent.IsStartOsLoginStartSessionEvent(r), Is.True);

            var e = (OsLoginStartSessionEvent)r.ToEvent();

            Assert.That(e.Severity, Is.EqualTo("INFO"));
            Assert.That(e.Instance?.ProjectId, Is.EqualTo("project-1"));
            Assert.That(e.Instance?.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(e.Instance?.Name, Is.EqualTo("instance-1"));
            Assert.That(e.InstanceId, Is.EqualTo(1234567890));
            Assert.That(e.Principal, Is.EqualTo("bob@example.com"));
            Assert.That(e.ChallengeStatus, Is.EqualTo("CHALLENGE_REQUIRED"));
            Assert.That(e.Message, Is.EqualTo("Start OS Login 2FA session for bob@example.com: CHALLENGE_REQUIRED"));
        }

        [Test]
        public void ToEvent_When2faMethodsUnavailable()
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
                    'methodName': 'google.cloud.oslogin.v1.OsLoginService.StartSession',
                    'authorizationInfo': [
                      {
                        'resource': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                        'granted': true
                      }
                    ],
                    'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                    'request': {
                      'supportedChallengeTypes': [
                        'INTERNAL_TWO_FACTOR',
                        'SECURITY_KEY_OTP',
                        'AUTHZEN',
                        'TOTP',
                        'IDV_PREREGISTERED_PHONE'
                      ],
                      'numericProjectId': '8849500000',
                      'email': '1075475743000',
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.StartSessionRequest'
                    },
                    'response': {
                      'sessionId': 'AM3QAYYXRnWWZW3vmO',
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.StartOrContinueSessionResponse',
                      'status': 'NO_AVAILABLE_CHALLENGES'
                    }
                  },
                  'insertId': '7hubh5cwcj',
                  'resource': {
                    'type': 'audited_resource',
                    'labels': {
                      'project_id': 'project-1',
                      'method': 'google.cloud.oslogin.v1.OsLoginService.StartSession',
                      'service': 'oslogin.googleapis.com'
                    }
                  },
                  'timestamp': '2021-11-10T07:51:48.475797Z',
                  'severity': 'INFO',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
                  'receiveTimestamp': '2021-11-10T07:51:49.608628875Z'
                }";

            var r = LogRecord.Deserialize(json)!;
            Assert.That(OsLoginStartSessionEvent.IsStartOsLoginStartSessionEvent(r), Is.True);

            var e = (OsLoginStartSessionEvent)r.ToEvent();

            Assert.That(e.Severity, Is.EqualTo("INFO"));
            Assert.That(e.Instance?.ProjectId, Is.EqualTo("project-1"));
            Assert.That(e.Instance?.Zone, Is.EqualTo("us-central1-a"));
            Assert.That(e.Instance?.Name, Is.EqualTo("instance-1"));
            Assert.That(e.Principal, Is.EqualTo("bob@example.com"));
            Assert.That(e.ChallengeStatus, Is.EqualTo("NO_AVAILABLE_CHALLENGES"));
            Assert.That(e.Message, Is.EqualTo("Start OS Login 2FA session for bob@example.com: NO_AVAILABLE_CHALLENGES"));
        }
    }
}
