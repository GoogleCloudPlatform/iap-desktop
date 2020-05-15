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

using Google.Solutions.LogAnalysis.Events;
using Google.Solutions.LogAnalysis.Extensions;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace Google.Solutions.LogAnalysis.Test.Extensions
{
    [TestFixture]
    public class TestListLogEntriesPage
    {
        [Test]
        public void WhenPageIsEmpty_ThenReadPageReturnsEmptySequence()
        {
            var json = @"{}";

            var events = new List<EventBase>();

            var token = ListLogEntriesPage.Read(
                new JsonTextReader(new StringReader(json)),
                events.Add);

            Assert.IsNull(token);
            Assert.AreEqual(0, events.Count);
        }

        [Test]
        public void WhenPageHasDataAndNextPageToken_ThenReadPageReturnsToken()
        {
            var json = @"
                {
                  'entries': [
                    {
                      'protoPayload': {
                        '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                        'authenticationInfo': {
                        },
                        'requestMetadata': {
                        },
                        'serviceName': 'compute.googleapis.com',
                        'methodName': 'beta.compute.instances.listReferrers',
                        'authorizationInfo': [
                        ],
                        'resourceName': 'projects/project-1/zones/us-central1-b/instances',
                        'request': {
                          '@type': 'type.googleapis.com/compute.instances.listReferrers'
                        },
                        'resourceLocation': {
                          'currentLocations': [
                            'us-central1-b'
                          ]
                        }
                      },
                      'insertId': 'gdq8xba8',
                      'resource': {
                        'type': 'gce_instance',
                        'labels': {
                          'zone': 'us-central1-b',
                          'instance_id': '',
                          'project_id': 'project-1'
                        }
                      },
                      'timestamp': '2020-05-15T10:56:20.474Z',
                      'severity': 'INFO',
                      'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
                      'receiveTimestamp': '2020-05-15T10:56:26.900097443Z'
                    }
                  ],
                  'nextPageToken': 'EAE4oeeNuZes8rwyStsEIhoiCgoIZ'
                }";

            var events = new List<EventBase>();

            var token = ListLogEntriesPage.Read(
                new JsonTextReader(new StringReader(json)),
                events.Add);

            Assert.AreEqual("EAE4oeeNuZes8rwyStsEIhoiCgoIZ", token);
            Assert.AreEqual(1, events.Count);
        }

        [Test]
        public void WhenPageOnlyHasNextPageToken_ThenReadPageReturnsToken()
        {
            var json = @"
                {
                  'nextPageToken': 'EAE4oeeNuZes8rwyStsEIhoiCgoIZ'
                }";


            var events = new List<EventBase>();

            var token = ListLogEntriesPage.Read(
                new JsonTextReader(new StringReader(json)),
                events.Add);

            Assert.AreEqual("EAE4oeeNuZes8rwyStsEIhoiCgoIZ", token);
            Assert.AreEqual(0, events.Count);
        }
    }
}
