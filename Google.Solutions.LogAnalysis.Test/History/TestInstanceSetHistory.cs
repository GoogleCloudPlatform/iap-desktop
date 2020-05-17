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
using Google.Solutions.LogAnalysis.Events;
using Google.Solutions.LogAnalysis.Events.Lifecycle;
using Google.Solutions.LogAnalysis.Events.System;
using Google.Solutions.LogAnalysis.History;
using Google.Solutions.LogAnalysis.Logs;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Google.Solutions.LogAnalysis.Test.History
{
    [TestFixture]
    public class TestInstanceSetHistory
    {
        [Test]
        public void WhenSerializedAndDeserialized_ThenObjectsAreEquivalent()
        {
            var history = new InstanceSetHistory(
                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new[]
                {
                    new InstanceHistory(
                        188550847350222232,
                        new VmInstanceReference("project-1", "us-central1-a", "instance-1"),
                        null,
                        Tenancy.Fleet,
                        new []
                        {
                            new Placement(
                                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                                new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc)),
                            new Placement(
                                new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                                new DateTime(2019, 12, 3, 0, 0, 0, DateTimeKind.Utc))
                        })
                },
                new[]
                {
                     new InstanceHistory(
                        118550847350222232,
                        null,
                        null,
                        Tenancy.SoleTenant,
                        new []
                        {
                            new Placement(
                                "server-1",
                                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                                new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc))
                        })
                });

            using (var memoryStream = new MemoryStream())
            {
                var s = new StringWriter();
                    history.Serialize(s);

                var writer = new StreamWriter(memoryStream);
                history.Serialize(writer);
                writer.Flush();

                memoryStream.Position = 0;

                var restoredHistory = InstanceSetHistory.Deserialize(
                    new StreamReader(memoryStream));

                Assert.AreEqual(history.StartDate, restoredHistory.StartDate);
                Assert.AreEqual(history.EndDate, restoredHistory.EndDate);

                Assert.AreEqual(1, restoredHistory.Instances.Count());
                var i = restoredHistory.Instances.First();

                Assert.AreEqual(history.Instances.First().InstanceId, i.InstanceId);
                Assert.AreEqual(history.Instances.First().Reference, i.Reference);
                Assert.AreEqual(history.Instances.First().Tenancy, i.Tenancy);

                Assert.AreEqual(history.Instances.First().Placements.Count(), i.Placements.Count());
                Assert.AreEqual(history.Instances.First().Placements.First().From, i.Placements.First().From);
                Assert.AreEqual(history.Instances.First().Placements.First().To, i.Placements.First().To);
                Assert.AreEqual(history.Instances.First().Placements.First().ServerId, i.Placements.First().ServerId);
                Assert.AreEqual(history.Instances.First().Placements.First().Tenancy, i.Placements.First().Tenancy);

                Assert.AreEqual(history.Instances.First().Placements.Last().From, i.Placements.Last().From);
                Assert.AreEqual(history.Instances.First().Placements.Last().To, i.Placements.Last().To);
                Assert.AreEqual(history.Instances.First().Placements.Last().ServerId, i.Placements.Last().ServerId);
                Assert.AreEqual(history.Instances.First().Placements.Last().Tenancy, i.Placements.Last().Tenancy);

                Assert.AreEqual(1, restoredHistory.InstancesWithIncompleteInformation.Count());
                i = restoredHistory.InstancesWithIncompleteInformation.First();

                Assert.AreEqual(history.InstancesWithIncompleteInformation.First().InstanceId, i.InstanceId);
                Assert.AreEqual(history.InstancesWithIncompleteInformation.First().Reference, i.Reference);
                Assert.AreEqual(history.InstancesWithIncompleteInformation.First().Tenancy, i.Tenancy);

                Assert.AreEqual(history.InstancesWithIncompleteInformation.First().Placements.Count(), i.Placements.Count());
                Assert.AreEqual(history.InstancesWithIncompleteInformation.First().Placements.First().From, i.Placements.First().From);
                Assert.AreEqual(history.InstancesWithIncompleteInformation.First().Placements.First().To, i.Placements.First().To);
                Assert.AreEqual(history.InstancesWithIncompleteInformation.First().Placements.First().ServerId, i.Placements.First().ServerId);
                Assert.AreEqual(history.InstancesWithIncompleteInformation.First().Placements.First().Tenancy, i.Placements.First().Tenancy);
            }
        }

        [Test]
        public void WhenTypeAnnotationIsMissing_ThenDeserializeThrowsFormatException()
        {
            var json = "{}";

            using (var reader = new StringReader(json))
            {
                Assert.Throws<FormatException>(() => InstanceSetHistory.Deserialize(reader));
            }
        }

        [Test]
        public void WhenTypeAnnotationIsWrong_ThenDeserializeThrowsFormatException()
        {
            var json = @"
            {
              '@type': 'type.googleapis.com/google.solutions.loganalysis.Foo',
              'start': '2019-12-01T00:00:00Z',
              'end': '2020-01-01T00:00:00Z'
            }";

            using (var reader = new StringReader(json))
            {
                Assert.Throws<FormatException>(() => InstanceSetHistory.Deserialize(reader));
            }
        }

        [Test]
        public void WhenJsonValid_ThenDeserializeReturnsObject()
        {
            var json = @"
            {
              '@type': 'type.googleapis.com/google.solutions.loganalysis.InstanceSetHistory',
              'start': '2019-12-01T00:00:00Z',
              'end': '2020-01-01T00:00:00Z',
              'instances': [
                {
                  'id': 188550847350222232,
                  'vm': {
                    'projectId': 'project-1',
                    'zone': 'us-central1-a',
                    'instanceName': 'instance-1'
                  },
                  'placements': [
                    {
                      'tenancy': 1,
                      'from': '2019-12-01T00:00:00Z',
                      'to': '2019-12-02T00:00:00Z'
                    },
                    {
                      'tenancy': 1,
                      'from': '2019-12-02T00:00:00Z',
                      'to': '2019-12-03T00:00:00Z'
                    }
                  ],
                  'tenancy': 1
                }
              ],
              'incompleteInstances': [
                {
                  'id': 118550847350222232,
                  'placements': [
                    {
                      'tenancy': 2,
                      'server': 'server-1',
                      'from': '2019-12-01T00:00:00Z',
                      'to': '2019-12-02T00:00:00Z'
                    }
                  ],
                  'tenancy': 2
                }
              ]
            }";

            using (var reader = new StringReader(json))
            {
                var restoredHistory = InstanceSetHistory.Deserialize(reader);

                Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), restoredHistory.StartDate);
                Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), restoredHistory.EndDate);

                Assert.AreEqual(1, restoredHistory.Instances.Count());
                var i = restoredHistory.Instances.First();

                Assert.AreEqual(188550847350222232, i.InstanceId);
                Assert.AreEqual(
                    new VmInstanceReference("project-1", "us-central1-a", "instance-1"), 
                    i.Reference);
                Assert.AreEqual(Tenancy.Fleet, i.Tenancy);

                Assert.AreEqual(2, i.Placements.Count());
                Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), i.Placements.First().From);
                Assert.AreEqual(new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc), i.Placements.First().To);
                Assert.IsNull(i.Placements.First().ServerId);
                Assert.AreEqual(Tenancy.Fleet, i.Placements.First().Tenancy);

                Assert.AreEqual(new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc), i.Placements.Last().From);
                Assert.AreEqual(new DateTime(2019, 12, 3, 0, 0, 0, DateTimeKind.Utc), i.Placements.Last().To);
                Assert.IsNull(i.Placements.Last().ServerId);
                Assert.AreEqual(Tenancy.Fleet, i.Placements.Last().Tenancy);

                Assert.AreEqual(1, restoredHistory.InstancesWithIncompleteInformation.Count());
                i = restoredHistory.InstancesWithIncompleteInformation.First();

                Assert.AreEqual(118550847350222232, i.InstanceId);
                Assert.IsNull(i.Reference);
                Assert.AreEqual(Tenancy.SoleTenant, i.Tenancy);

                Assert.AreEqual(1, i.Placements.Count());
                Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), i.Placements.First().From);
                Assert.AreEqual(new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc), i.Placements.First().To);
                Assert.AreEqual("server-1", i.Placements.First().ServerId);
                Assert.AreEqual(Tenancy.SoleTenant, i.Placements.First().Tenancy);
            }
        }
    }
}
