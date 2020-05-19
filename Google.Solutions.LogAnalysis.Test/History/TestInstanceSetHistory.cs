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
                        InstanceHistoryState.Complete,
                        null,
                        new []
                        {
                            new InstancePlacement(
                                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                                new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc)),
                            new InstancePlacement(
                                new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                                new DateTime(2019, 12, 3, 0, 0, 0, DateTimeKind.Utc))
                        }),
                     new InstanceHistory(
                        118550847350222232,
                        null,
                        InstanceHistoryState.MissingImage,
                        null,
                        new []
                        {
                            new InstancePlacement(
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

                Assert.AreEqual(2, restoredHistory.Instances.Count());
                var completeInstance = restoredHistory.Instances.First(i => i.InstanceId == 188550847350222232);

                Assert.AreEqual(history.Instances.First().Reference, completeInstance.Reference);

                Assert.AreEqual(history.Instances.First().Placements.Count(), completeInstance.Placements.Count());
                Assert.AreEqual(history.Instances.First().Placements.First().From, completeInstance.Placements.First().From);
                Assert.AreEqual(history.Instances.First().Placements.First().To, completeInstance.Placements.First().To);
                Assert.AreEqual(history.Instances.First().Placements.First().ServerId, completeInstance.Placements.First().ServerId);
                Assert.AreEqual(history.Instances.First().Placements.First().Tenancy, completeInstance.Placements.First().Tenancy);

                Assert.AreEqual(history.Instances.First().Placements.Last().From, completeInstance.Placements.Last().From);
                Assert.AreEqual(history.Instances.First().Placements.Last().To, completeInstance.Placements.Last().To);
                Assert.AreEqual(history.Instances.First().Placements.Last().ServerId, completeInstance.Placements.Last().ServerId);
                Assert.AreEqual(history.Instances.First().Placements.Last().Tenancy, completeInstance.Placements.Last().Tenancy);

                var incompleteInstance = restoredHistory.Instances.First(i => i.InstanceId == 118550847350222232);

                Assert.AreEqual(history.Instances.Last().InstanceId, incompleteInstance.InstanceId);
                Assert.AreEqual(history.Instances.Last().Reference, incompleteInstance.Reference);

                Assert.AreEqual(history.Instances.Last().Placements.Count(), incompleteInstance.Placements.Count());
                Assert.AreEqual(history.Instances.Last().Placements.First().From, incompleteInstance.Placements.First().From);
                Assert.AreEqual(history.Instances.Last().Placements.First().To, incompleteInstance.Placements.First().To);
                Assert.AreEqual(history.Instances.Last().Placements.First().ServerId, incompleteInstance.Placements.First().ServerId);
                Assert.AreEqual(history.Instances.Last().Placements.First().Tenancy, incompleteInstance.Placements.First().Tenancy);
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
              'instanceSetHistory': {
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
                    'tenancy': 1,
                    'state': 0
                  },
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
                    'tenancy': 2,
                    'state': 3
                  }
                ]
              }
            }";

            using (var reader = new StringReader(json))
            {
                var restoredHistory = InstanceSetHistory.Deserialize(reader);

                Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), restoredHistory.StartDate);
                Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), restoredHistory.EndDate);

                Assert.AreEqual(2, restoredHistory.Instances.Count());
                var completeInstance = restoredHistory.Instances.First(i => i.InstanceId == 188550847350222232);

                Assert.AreEqual(InstanceHistoryState.Complete, completeInstance.State);
                Assert.AreEqual(
                    new VmInstanceReference("project-1", "us-central1-a", "instance-1"),
                    completeInstance.Reference);

                Assert.AreEqual(2, completeInstance.Placements.Count());
                Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), completeInstance.Placements.First().From);
                Assert.AreEqual(new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc), completeInstance.Placements.First().To);
                Assert.IsNull(completeInstance.Placements.First().ServerId);
                Assert.AreEqual(Tenancy.Fleet, completeInstance.Placements.First().Tenancy);

                Assert.AreEqual(new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc), completeInstance.Placements.Last().From);
                Assert.AreEqual(new DateTime(2019, 12, 3, 0, 0, 0, DateTimeKind.Utc), completeInstance.Placements.Last().To);
                Assert.IsNull(completeInstance.Placements.Last().ServerId);
                Assert.AreEqual(Tenancy.Fleet, completeInstance.Placements.Last().Tenancy);

                var incompleteInstance = restoredHistory.Instances.First(i => i.InstanceId == 118550847350222232);

                Assert.AreEqual(InstanceHistoryState.MissingImage, incompleteInstance.State);
                Assert.IsNull(incompleteInstance.Reference);

                Assert.AreEqual(1, incompleteInstance.Placements.Count());
                Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), incompleteInstance.Placements.First().From);
                Assert.AreEqual(new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc), incompleteInstance.Placements.First().To);
                Assert.AreEqual("server-1", incompleteInstance.Placements.First().ServerId);
                Assert.AreEqual(Tenancy.SoleTenant, incompleteInstance.Placements.First().Tenancy);
            }
        }
    }
}
