// TODO: Cleanup dead code
////
//// Copyright 2021 Google LLC
////
//// Licensed to the Apache Software Foundation (ASF) under one
//// or more contributor license agreements.  See the NOTICE file
//// distributed with this work for additional information
//// regarding copyright ownership.  The ASF licenses this file
//// to you under the Apache License, Version 2.0 (the
//// "License"); you may not use this file except in compliance
//// with the License.  You may obtain a copy of the License at
//// 
////   http://www.apache.org/licenses/LICENSE-2.0
//// 
//// Unless required by applicable law or agreed to in writing,
//// software distributed under the License is distributed on an
//// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
//// KIND, either express or implied.  See the License for the
//// specific language governing permissions and limitations
//// under the License.
////

//using Google.Apis.Compute.v1.Data;
//using Google.Solutions.Apis.Locator;
//using Google.Solutions.IapDesktop.Application.Services.Adapters;
//using Google.Solutions.IapDesktop.Core.ProjectModel;
//using NUnit.Framework;
//using System.Linq;

//namespace Google.Solutions.IapDesktop.Core.Test.ProjectModel
//{
//    [TestFixture]
//    public class TestProjectModelNodes : ApplicationFixtureBase
//    {
//        private static readonly Instance SampleWindowsInstanceInZone1 = new Instance()
//        {
//            Id = 1u,
//            Name = "windows-1",
//            Disks = new[]
//            {
//                new AttachedDisk()
//                {
//                    GuestOsFeatures = new []
//                    {
//                        new GuestOsFeature()
//                        {
//                            Type = "WINDOWS"
//                        }
//                    }
//                }
//            },
//            Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/zone-1",
//            Status = "RUNNING"
//        };

//        private static readonly Instance SampleLinuxInstanceInZone1 = new Instance()
//        {
//            Id = 2u,
//            Name = "linux-zone-1",
//            Disks = new[]
//            {
//                new AttachedDisk()
//                {
//                }
//            },
//            Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/zone-1",
//            Status = "RUNNING"
//        };

//        private static readonly Instance SampleLinuxInstanceWithoutDiskInZone1 = new Instance()
//        {
//            Id = 3u,
//            Name = "linux-nodisk-zone-1",
//            Disks = new AttachedDisk[0],
//            Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/zone-1"
//        };

//        private static readonly Instance SampleTerminatedLinuxInstanceInZone1 = new Instance()
//        {
//            Id = 4u,
//            Name = "linux-terminated-zone-1",
//            Disks = new[]
//            {
//                new AttachedDisk()
//                {
//                }
//            },
//            Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/zone-1",
//            Status = "TERMINATED"
//        };

//        private static readonly Instance SampleLinuxInstanceInZone2 = new Instance()
//        {
//            Id = 5u,
//            Name = "linux-zone-2",
//            Disks = new[]
//            {
//                new AttachedDisk()
//                {
//                }
//            },
//            Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/zone-2",
//            Status = "RUNNING"
//        };

//        //---------------------------------------------------------------------
//        // Project node.
//        //---------------------------------------------------------------------

//        [Test]
//        public void WhenInstancesNull_ThenFromProjectReturnsNode()
//        {
//            var node = ProjectNode.FromProject(
//                new Project()
//                {
//                    Name = "project-1",
//                    Description = "test"
//                },
//                null);

//            Assert.IsNotNull(node);
//            CollectionAssert.IsEmpty(node.Zones);
//        }

//        [Test]
//        public void WhenProjectInitialized_ThenDisplayNameOfProjectNodeIsDescription()
//        {
//            var node = ProjectNode.FromProject(
//                new Project()
//                {
//                    Name = "project-1",
//                    Description = "test"
//                },
//                new[] { SampleLinuxInstanceInZone1 });

//            Assert.AreEqual("test", node.DisplayName);
//            Assert.AreEqual(new ProjectLocator("project-1"), node.Project);
//        }

//        [Test]
//        public void WhenFilteringByAllOperatingSystems_ThenAllInstancesReturned()
//        {
//            var node = ProjectNode.FromProject(
//                new Project()
//                {
//                    Name = "project-1",
//                    Description = "test"
//                },
//                new[]
//                {
//                    SampleLinuxInstanceInZone1,
//                    SampleWindowsInstanceInZone1,
//                    SampleLinuxInstanceInZone2
//                });

//            var filtered = node.FilterBy(OperatingSystems.All);

//            Assert.AreEqual(node.Zones.Count(), filtered.Zones.Count());

//            var zone1 = filtered.Zones.First();
//            var zone2 = filtered.Zones.Last();

//            CollectionAssert.AreEqual(
//                new[] { SampleLinuxInstanceInZone1.Name, SampleWindowsInstanceInZone1.Name },
//                zone1.Instances.Select(i => i.DisplayName));
//            CollectionAssert.AreEqual(
//                new[] { SampleLinuxInstanceInZone2.Name },
//                zone2.Instances.Select(i => i.DisplayName));
//        }

//        [Test]
//        public void WhenFilteringByNoOperatingSystems_ThenNoZonesReturned()
//        {
//            var node = ProjectNode.FromProject(
//                new Project()
//                {
//                    Name = "project-1",
//                    Description = "test"
//                },
//                new[]
//                {
//                    SampleLinuxInstanceInZone1,
//                    SampleWindowsInstanceInZone1,
//                    SampleLinuxInstanceInZone2
//                });

//            var filtered = node.FilterBy((OperatingSystems)0);
//            Assert.AreEqual(0, filtered.Zones.Count());
//        }

//        [Test]
//        public void WhenFilteringByOperatingSystems_ThenOnlyZonesWithMatchingInstancesReturned()
//        {
//            var node = ProjectNode.FromProject(
//                new Project()
//                {
//                    Name = "project-1",
//                    Description = "test"
//                },
//                new[]
//                {
//                    SampleLinuxInstanceInZone1,
//                    SampleWindowsInstanceInZone1,
//                    SampleLinuxInstanceInZone2
//                });

//            var filtered = node.FilterBy(OperatingSystems.Windows);

//            Assert.AreEqual(1, filtered.Zones.Count());
//            Assert.AreEqual("zone-1", filtered.Zones.First().DisplayName);

//            var zone1 = filtered.Zones.First();

//            CollectionAssert.AreEqual(
//                new[] { SampleWindowsInstanceInZone1.Name },
//                zone1.Instances.Select(i => i.DisplayName));
//        }

//        //---------------------------------------------------------------------
//        // Zone node.
//        //---------------------------------------------------------------------

//        [Test]
//        public void WhenInstancesSpreadAcrossZones_ThenInstancesAreGroupedByZone()
//        {
//            var node = ProjectNode.FromProject(
//                new Project()
//                {
//                    Name = "project-1",
//                    Description = "test"
//                },
//                new[] { SampleLinuxInstanceInZone1, SampleLinuxInstanceInZone2 });

//            Assert.AreEqual(2, node.Zones.Count());

//            var zone1 = node.Zones.First();
//            var zone2 = node.Zones.Last();

//            Assert.AreEqual(new ZoneLocator("project-1", "zone-1"), zone1.Zone);
//            Assert.AreEqual(new ZoneLocator("project-1", "zone-2"), zone2.Zone);

//            Assert.AreEqual(1, zone1.Instances.Count());
//            Assert.AreEqual(1, zone2.Instances.Count());
//        }

//        //---------------------------------------------------------------------
//        // Instance node.
//        //---------------------------------------------------------------------

//        [Test]
//        public void WhenInitialized_ThenIdAndLocatorAreSet()
//        {
//            var node = ProjectNode.FromProject(
//                new Project()
//                {
//                    Name = "project-1",
//                    Description = "test"
//                },
//                new[] { SampleWindowsInstanceInZone1 });

//            var windows = node.Zones.First().Instances.First();

//            Assert.AreEqual(SampleWindowsInstanceInZone1.Name, windows.DisplayName);
//            Assert.AreEqual(SampleWindowsInstanceInZone1.Id, windows.InstanceId);
//            Assert.AreEqual(
//                new InstanceLocator("project-1", "zone-1", SampleWindowsInstanceInZone1.Name),
//                windows.Instance);
//        }

//        [Test]
//        public void WhenInstanceHasNoDisk_ThenInstanceIsSkipped()
//        {
//            var node = ProjectNode.FromProject(
//                new Project()
//                {
//                    Name = "project-1",
//                    Description = "test"
//                },
//                new[] { SampleLinuxInstanceInZone1, SampleLinuxInstanceWithoutDiskInZone1 });

//            Assert.AreEqual(1, node.Zones.First().Instances.Count());
//            Assert.AreEqual(SampleLinuxInstanceInZone1.Name, node.Zones.First().Instances.First().DisplayName);
//        }

//        [Test]
//        public void WhenInstanceIsWindows_ThenIsWindowsInstanceIsTrue()
//        {
//            var node = ProjectNode.FromProject(
//                new Project()
//                {
//                    Name = "project-1",
//                    Description = "test"
//                },
//                new[] { SampleLinuxInstanceInZone1, SampleWindowsInstanceInZone1 });

//            Assert.AreEqual(2, node.Zones.First().Instances.Count());

//            var linux = node.Zones.First().Instances.First();
//            var windows = node.Zones.First().Instances.Last();

//            Assert.AreEqual(SampleLinuxInstanceInZone1.Name, linux.DisplayName);
//            Assert.IsFalse(linux.IsWindowsInstance);

//            Assert.AreEqual(SampleWindowsInstanceInZone1.Name, windows.DisplayName);
//            Assert.IsTrue(windows.IsWindowsInstance);
//        }

//        [Test]
//        public void WhenInstanceIsTerminated_ThenIsRunningIsFalse()
//        {
//            var node = ProjectNode.FromProject(
//                new Project()
//                {
//                    Name = "project-1",
//                    Description = "test"
//                },
//                new[] { SampleTerminatedLinuxInstanceInZone1, SampleLinuxInstanceInZone1 });

//            Assert.AreEqual(2, node.Zones.First().Instances.Count());

//            var terminated = node.Zones.First().Instances.First();
//            var running = node.Zones.First().Instances.Last();

//            Assert.AreEqual(SampleTerminatedLinuxInstanceInZone1.Name, terminated.DisplayName);
//            Assert.IsFalse(terminated.IsRunning);

//            Assert.AreEqual(SampleLinuxInstanceInZone1.Name, running.DisplayName);
//            Assert.IsTrue(running.IsRunning);
//        }
//    }
//}
