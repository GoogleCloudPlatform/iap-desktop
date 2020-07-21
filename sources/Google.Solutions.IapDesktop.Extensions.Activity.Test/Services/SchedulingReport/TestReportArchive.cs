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
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.SchedulingReport;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.SchedulingReport
{
    [TestFixture]
    public class TestReportArchive : FixtureBase
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
                        new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                        InstanceHistoryState.Complete,
                        new ImageLocator("project-1", "windows"),
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

            var annotatedHistory = ReportArchive.FromInstanceSetHistory(history);
            annotatedHistory.AddLicenseAnnotation(
                new ImageLocator("project-1", "windows"),
                OperatingSystemTypes.Windows,
                LicenseTypes.Spla);

            using (var memoryStream = new MemoryStream())
            {
                var s = new StringWriter();
                annotatedHistory.Serialize(s);

                var writer = new StreamWriter(memoryStream);
                annotatedHistory.Serialize(writer);
                writer.Flush();

                memoryStream.Position = 0;

                var restoredAnnotatedHistory = ReportArchive
                    .Deserialize(new StreamReader(memoryStream));
                var restoredHistory = restoredAnnotatedHistory.History;

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

                var annotation = restoredAnnotatedHistory.LicenseAnnotations[history.Instances.First().Image.ToString()];
                Assert.IsNotNull(annotation);
                Assert.AreEqual(OperatingSystemTypes.Windows, annotation.OperatingSystem);
                Assert.AreEqual(LicenseTypes.Spla, annotation.LicenseType);

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
                Assert.Throws<FormatException>(() => ReportArchive.Deserialize(reader));
            }
        }

        [Test]
        public void WhenTypeAnnotationIsWrong_ThenDeserializeThrowsFormatException()
        {
            var json = @"
            {
              '@type': 'type.googleapis.com/Google.Solutions.IapDesktop.Extensions.Activity.Foo',
              'start': '2019-12-01T00:00:00Z',
              'end': '2020-01-01T00:00:00Z'
            }";

            using (var reader = new StringReader(json))
            {
                Assert.Throws<FormatException>(() => ReportArchive.Deserialize(reader));
            }
        }

        [Test]
        public void WhenJsonValid_ThenDeserializeReturnsObject()
        {
            var json = @"
            {
              '@type': 'type.googleapis.com/Google.Solutions.IapDesktop.Extensions.Activity.ReportArchive',
              'annotatedInstanceSetHistory': {
                'history': {
                  'start': '2019-12-01T00:00:00Z',
                  'end': '2020-01-01T00:00:00Z',
                  'instances': [
                    {
                      'id': 188550847350222232,
                      'vm': {
                        'zone': 'us-central1-a',
                        'resourceType': 'instances',
                        'projectId': 'project-1',
                        'name': 'instance-1'
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
                      'image': {
                        'resourceType': 'images',
                        'projectId': 'project-1',
                        'name': 'windows'
                      },
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
                      'state': 3
                    }
                  ]
                },
                'licenseAnnotations': {
                  'projects/project-1/global/images/windows': {
                    'licenseType': 4,
                    'os': 2
                  }
                }
              }
            }";

            using (var reader = new StringReader(json))
            {
                var restoredAnnotatedHistory = ReportArchive.Deserialize(reader);
                var restoredHistory = restoredAnnotatedHistory.History;

                Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), restoredHistory.StartDate);
                Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), restoredHistory.EndDate);

                Assert.AreEqual(2, restoredHistory.Instances.Count());
                var completeInstance = restoredHistory.Instances.First(i => i.InstanceId == 188550847350222232);

                Assert.AreEqual(InstanceHistoryState.Complete, completeInstance.State);
                Assert.AreEqual(
                    new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                    completeInstance.Reference);

                Assert.AreEqual(2, completeInstance.Placements.Count());
                Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), completeInstance.Placements.First().From);
                Assert.AreEqual(new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc), completeInstance.Placements.First().To);
                Assert.IsNull(completeInstance.Placements.First().ServerId);
                Assert.AreEqual(Tenancies.Fleet, completeInstance.Placements.First().Tenancy);

                Assert.AreEqual(new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc), completeInstance.Placements.Last().From);
                Assert.AreEqual(new DateTime(2019, 12, 3, 0, 0, 0, DateTimeKind.Utc), completeInstance.Placements.Last().To);
                Assert.IsNull(completeInstance.Placements.Last().ServerId);
                Assert.AreEqual(Tenancies.Fleet, completeInstance.Placements.Last().Tenancy);


                var annotation = restoredAnnotatedHistory.LicenseAnnotations["projects/project-1/global/images/windows"];
                Assert.IsNotNull(annotation);
                Assert.AreEqual(OperatingSystemTypes.Windows, annotation.OperatingSystem);
                Assert.AreEqual(LicenseTypes.Spla, annotation.LicenseType);

                var incompleteInstance = restoredHistory.Instances.First(i => i.InstanceId == 118550847350222232);

                Assert.AreEqual(InstanceHistoryState.MissingImage, incompleteInstance.State);
                Assert.IsNull(incompleteInstance.Reference);

                Assert.AreEqual(1, incompleteInstance.Placements.Count());
                Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), incompleteInstance.Placements.First().From);
                Assert.AreEqual(new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc), incompleteInstance.Placements.First().To);
                Assert.AreEqual("server-1", incompleteInstance.Placements.First().ServerId);
                Assert.AreEqual(Tenancies.SoleTenant, incompleteInstance.Placements.First().Tenancy);
            }
        }

        [Test]
        public void WhenImageNotAnnotated_ThenQueryUnknownReturnsInstance()
        {
            var history = new InstanceSetHistory(
                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new[]
                {
                    new InstanceHistory(
                        188550847350222232,
                        new InstanceLocator("project-1", "us-central1-a", "instance-1"),
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
                        })
                });

            var annotatedHistory = ReportArchive.FromInstanceSetHistory(history);
            Assert.IsTrue(annotatedHistory.GetInstances(
                    OperatingSystemTypes.Unknown,
                    LicenseTypes.Unknown).Any());

            Assert.IsFalse(annotatedHistory.GetInstances(
                    OperatingSystemTypes.Linux,
                    LicenseTypes.Unknown).Any());

            Assert.IsFalse(annotatedHistory.GetInstances(
                    OperatingSystemTypes.Unknown,
                    LicenseTypes.Spla).Any());
        }

        [Test]
        public void WhenImageAnnotatedAsWindowsSpla_ThenQueryWindowsSplaReturnsInstance()
        {
            var history = new InstanceSetHistory(
                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new[]
                {
                    new InstanceHistory(
                        188550847350222232,
                        new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                        InstanceHistoryState.Complete,
                        new ImageLocator("project-1", "image"),
                        new []
                        {
                            new InstancePlacement(
                                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                                new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc)),
                            new InstancePlacement(
                                new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                                new DateTime(2019, 12, 3, 0, 0, 0, DateTimeKind.Utc))
                        })
                });

            var annotatedHistory = ReportArchive.FromInstanceSetHistory(history);
            annotatedHistory.AddLicenseAnnotation(
                new ImageLocator("project-1", "image"),
                OperatingSystemTypes.Windows,
                LicenseTypes.Spla);

            Assert.IsTrue(annotatedHistory.GetInstances(
                    OperatingSystemTypes.Windows,
                    LicenseTypes.Spla).Any());

            Assert.IsTrue(annotatedHistory.GetInstances(
                    OperatingSystemTypes.Windows | OperatingSystemTypes.Linux,
                    LicenseTypes.Spla | LicenseTypes.Byol).Any());

            Assert.IsFalse(annotatedHistory.GetInstances(
                    OperatingSystemTypes.Linux,
                    LicenseTypes.Spla).Any());

            Assert.IsFalse(annotatedHistory.GetInstances(
                    OperatingSystemTypes.Windows,
                    LicenseTypes.Byol).Any());
        }
    }
}
