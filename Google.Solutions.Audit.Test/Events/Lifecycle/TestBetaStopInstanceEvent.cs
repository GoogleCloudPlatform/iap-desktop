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

using Google.Solutions.Compute;
using Google.Solutions.Audit.Events;
using Google.Solutions.Audit.Events.Lifecycle;
using Google.Solutions.Audit.Records;
using NUnit.Framework;

namespace Google.Solutions.Audit.Test.Events.Lifecycle
{
    [TestFixture]
    public class TestBetaStopInstanceEvent
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
                  'methodName': 'beta.compute.instances.stop',
                  'authorizationInfo': [
                  ],
                  'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                  'request': {
                    '@type': 'type.googleapis.com/compute.instances.stop'
                  },
                  'response': {
                  },
                  'resourceLocation': {
                    'currentLocations': [
                      'us-central1-a'
                    ]
                  }
                },
                'insertId': '-gwwofzdc39e',
                'resource': {
                  'type': 'gce_instance',
                  'labels': {
                    'instance_id': '2162224123123123213',
                    'project_id': 'project-1',
                    'zone': 'us-central1-a'
                  }
                },
                'timestamp': '2020-05-04T08:49:27.470Z',
                'severity': 'NOTICE',
                'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                'operation': {
                  'id': 'operation-1588582167320-5a4ce9bc79851-98bb5945-b9e2b31d',
                  'producer': 'compute.googleapis.com',
                  'first': true
                },
                'receiveTimestamp': '2020-05-04T08:49:28.949573763Z'
              }";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(StopInstanceEvent.IsStopInstanceEvent(r));

            var e = (StopInstanceEvent)r.ToEvent();

            Assert.AreEqual(2162224123123123213, e.InstanceId);
            Assert.AreEqual(
                new VmInstanceReference("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
        }

        [Test]
        public void WhenSeverityIsError_ThenFieldsAreExtracted()
        {
            Assert.Inconclusive();
        }
    }
}