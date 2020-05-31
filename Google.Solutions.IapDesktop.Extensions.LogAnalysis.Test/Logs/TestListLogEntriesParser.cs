﻿//
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

using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Events;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Events.Lifecycle;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Logs;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Test.Logs
{
    [TestFixture]
    public class TestListLogEntriesParser : FixtureBase
    {
        [Test]
        public void WhenPageIsEmpty_ThenReadPageReturnsEmptySequence()
        {
            var json = @"{}";

            var events = new List<EventBase>();

            var token = ListLogEntriesParser.Read(
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

            var token = ListLogEntriesParser.Read(
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

            var token = ListLogEntriesParser.Read(
                new JsonTextReader(new StringReader(json)),
                events.Add);

            Assert.AreEqual("EAE4oeeNuZes8rwyStsEIhoiCgoIZ", token);
            Assert.AreEqual(0, events.Count);
        }

        [Test]
        public void WhenStreamContainsTwoRecords_ThenReadReportsTwoEvents()
        {
            var json = @"
                {
                  'entries': [ {
                    'protoPayload': {
                        '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                        'authenticationInfo': {
                        },
                        'requestMetadata': {
                        },
                        'serviceName': 'compute.googleapis.com',
                        'methodName': 'v1.compute.instances.delete',
                        'authorizationInfo': [
                        ],
                        'resourceName': 'projects/123/zones/us-central1-a/instances/instance-1',
                        'request': {
                        'requestId': 'f802d080-d71e-4cae-a105-41fed099e362',
                        '@type': 'type.googleapis.com/compute.instances.delete'
                        },
                        'response': {
                        },
                        'resourceLocation': {
                        'currentLocations': [
                            'us-central1-a'
                        ]
                        }
                    },
                    'insertId': '7rriyre2bn74',
                    'resource': {
                        'type': 'gce_instance',
                        'labels': {
                        'instance_id': '3771111960822',
                        'project_id': 'project-1',
                        'zone': 'us-central1-a'
                        }
                    },
                    'timestamp': '2020-05-04T02:07:40.933Z',
                    'severity': 'NOTICE',
                    'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                    'operation': {
                        'id': 'operation-1588558060966-5a4c8feedd25b-f4637780-33f35e50',
                        'producer': 'compute.googleapis.com',
                        'first': true
                    },
                    'receiveTimestamp': '2020-05-04T02:07:41.604695630Z'
                    },
                    {
                    'protoPayload': {
                        '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                        'authenticationInfo': {
                        },
                        'requestMetadata': {
                        },
                        'serviceName': 'compute.googleapis.com',
                        'methodName': 'v1.compute.instances.start',
                        'authorizationInfo': [
                        ],
                        'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-2',
                        'request': {
                        '@type': 'type.googleapis.com/compute.instances.start'
                        },
                        'response': {
                        },
                        'resourceLocation': {
                        'currentLocations': [
                            'us-central1-a'
                        ]
                        }
                    },
                    'insertId': 'vcq6epd7n72',
                    'resource': {
                        'type': 'gce_instance',
                        'labels': {
                        'project_id': 'project-1',
                        'instance_id': '489405111114222',
                        'zone': 'us-central1-a'
                        }
                    },
                    'timestamp': '2020-05-04T13:56:26.405Z',
                    'severity': 'NOTICE',
                    'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                    'operation': {
                        'id': 'operation-1588600586345-5a4d2e5a39c56-47d0ce05-a9d7073c',
                        'producer': 'compute.googleapis.com',
                        'first': true
                    },
                    'receiveTimestamp': '2020-05-04T13:56:27.582777461Z'
                    }]
                }";

            var events = new List<EventBase>();

            var token = ListLogEntriesParser.Read(
                new JsonTextReader(new StringReader(json)),
                events.Add);

            Assert.AreEqual(2, events.Count());

            Assert.IsInstanceOf(typeof(DeleteInstanceEvent), events.First());
            var deleteEvent = (DeleteInstanceEvent)events.First();
            Assert.AreEqual("instance-1", deleteEvent.InstanceReference.Name);


            Assert.IsInstanceOf(typeof(StartInstanceEvent), events.Last());
            var startEvent = (StartInstanceEvent)events.Last();
            Assert.AreEqual("instance-2", startEvent.InstanceReference.Name);
        }

        [Test]
        public void WhenStreamContainsAnUnknownRecord_ThenReadReportsUnknownEvent()
        {
            var json = @"{
                'entries': [ {
                   'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'authenticationInfo': {
                     },
                     'requestMetadata': {
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'v1.compute.instances.delete',
                     'authorizationInfo': [
                     ],
                     'resourceName': 'projects/123/zones/us-central1-a/instances/instance-1',
                     'request': {
                       'requestId': 'f802d080-d71e-4cae-a105-41fed099e362',
                       '@type': 'type.googleapis.com/compute.instances.delete'
                     },
                     'response': {
                     },
                     'resourceLocation': {
                       'currentLocations': [
                         'us-central1-a'
                       ]
                     }
                   },
                   'insertId': '7rriyre2bn74',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'instance_id': '3771111960822',
                       'project_id': 'project-1',
                       'zone': 'us-central1-a'
                     }
                   },
                   'timestamp': '2020-05-04T02:07:40.933Z',
                   'severity': 'NOTICE',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                   'operation': {
                     'id': 'operation-1588558060966-5a4c8feedd25b-f4637780-33f35e50',
                     'producer': 'compute.googleapis.com',
                     'first': true
                   },
                   'receiveTimestamp': '2020-05-04T02:07:41.604695630Z'
                 },
                 {
                  'protoPayload': {
                     '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                     'authenticationInfo': {
                     },
                     'requestMetadata': {
                     },
                     'serviceName': 'compute.googleapis.com',
                     'methodName': 'unknown.method',
                     'authorizationInfo': [
                     ],
                     'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-2',
                     'request': {
                       '@type': 'type.googleapis.com/compute.instances.start'
                     },
                     'response': {
                     },
                     'resourceLocation': {
                       'currentLocations': [
                         'us-central1-a'
                       ]
                     }
                   },
                   'insertId': 'vcq6epd7n72',
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'project_id': 'project-1',
                       'instance_id': '489405111114222',
                       'zone': 'us-central1-a'
                     }
                   },
                   'timestamp': '2020-05-04T13:56:26.405Z',
                   'severity': 'NOTICE',
                   'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
                   'operation': {
                     'id': 'operation-1588600586345-5a4d2e5a39c56-47d0ce05-a9d7073c',
                     'producer': 'compute.googleapis.com',
                     'first': true
                   },
                   'receiveTimestamp': '2020-05-04T13:56:27.582777461Z'
                 }]
                }";

            var events = new List<EventBase>();

            var token = ListLogEntriesParser.Read(
                new JsonTextReader(new StringReader(json)),
                events.Add);

            Assert.AreEqual(2, events.Count());

            Assert.IsInstanceOf(typeof(DeleteInstanceEvent), events.First());
            var deleteEvent = (DeleteInstanceEvent)events.First();
            Assert.AreEqual("instance-1", deleteEvent.InstanceReference.Name);

            Assert.IsInstanceOf(typeof(UnknownEvent), events.Last());
        }

        [Test]
        public void WhenStreamContainsObjectInsteadOfArry_ThenNoEventsAreReported()
        {
            var json = @"
            {
                'this': 'is invalid'
            }";

            var events = new List<EventBase>();

            var token = ListLogEntriesParser.Read(
                new JsonTextReader(new StringReader(json)),
                events.Add);

            Assert.AreEqual(0, events.Count());
        }
    }
}
