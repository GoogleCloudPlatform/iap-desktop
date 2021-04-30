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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.Events.Access;
using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using NUnit.Framework;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Events.Access
{
    [TestFixture]
    public class TestStartOsLoginSession : ActivityFixtureBase
    {
        [Test]
        public void WhenSeverityIsInfo_ThenFieldsAreExtracted()
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
                        'resource': 'instances/instance-1',
                        'granted': true
                      }
                    ],
                    'resourceName': 'instances/instance-1',
                    'request': {
                      'numericProjectId': '1234',
                      'supportedChallengeTypes': [
                        'INTERNAL_TWO_FACTOR',
                        'SECURITY_KEY_OTP',
                        'AUTHZEN',
                        'TOTP',
                        'IDV_PREREGISTERED_PHONE'
                      ],
                      'email': '100121652021111111',
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.StartSessionRequest'
                    },
                    'response': {
                      'status': 'CHALLENGE_REQUIRED',
                      '@type': 'type.googleapis.com/google.cloud.oslogin.v1.StartOrContinueSessionResponse',
                      'sessionId': 'AM3QAYZunevS0v_sC_F0XKkRe6',
                      'challenges': [
                        {
                          'challengeId': 5,
                          'challengeType': 'SECURITY_KEY_OTP',
                          'status': 'READY'
                        },
                        {
                          'status': 'PROPOSED',
                          'challengeId': 2,
                          'challengeType': 'TOTP'
                        }
                      ]
                    }
                  },
                  'insertId': '1flxz1dc9hg',
                  'resource': {
                    'type': 'audited_resource',
                    'labels': {
                      'project_id': 'project-1',
                      'service': 'oslogin.googleapis.com',
                      'method': 'google.cloud.oslogin.v1.OsLoginService.StartSession'
                    }
                  },
                  'timestamp': '2021-04-30T09:48:28.775Z',
                  'severity': 'INFO',
                  'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
                  'receiveTimestamp': '2021-04-30T09:48:30.613071901Z'
                }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(StartOsLoginSession.IsStartOsLoginSessionEvent(r));

            var e = (StartOsLoginSession)r.ToEvent();

            // TODO: add assertions
        }


        [Test]
        public void WhenFieldContentsAreMissing_ThenFieldsAreExtracted()
        {
            
        }
    }
}
