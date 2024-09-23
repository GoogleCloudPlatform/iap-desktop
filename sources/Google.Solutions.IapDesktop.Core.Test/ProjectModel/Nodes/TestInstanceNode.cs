//
// Copyright 2024 Google LLC
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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel.Nodes;
using Moq;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.Test.ProjectModel.Nodes
{
    [TestFixture]
    public class TestInstanceNode
    {
        private static readonly InstanceLocator SampleLocatpr =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // OperatingSystem.
        //---------------------------------------------------------------------

        [Test]
        public void OperatingSystem_WhenTraintsEmpty()
        {
            var instance = new InstanceNode(
                new Mock<IProjectWorkspace>().Object,
                1,
                SampleLocatpr,
                Array.Empty<ITrait>(),
                "RUNNING");

            Assert.AreEqual(OperatingSystems.Linux, instance.OperatingSystem);
        }

        [Test]
        public void OperatingSystem_WhenTraintsContainsWindowsTrait()
        {
            var instance = new InstanceNode(
                new Mock<IProjectWorkspace>().Object,
                1,
                SampleLocatpr,
                new[] { WindowsTrait.Instance },
                "RUNNING");

            Assert.AreEqual(OperatingSystems.Windows, instance.OperatingSystem);
        }

        //---------------------------------------------------------------------
        // CanSuspend.
        //---------------------------------------------------------------------

        [Test]
        public void CanSuspend_WhenRunning(
            [Values("RUNNING", "REPAIRING")] string status)
        {
            var instance = new InstanceNode(
                new Mock<IProjectWorkspace>().Object,
                1,
                SampleLocatpr,
                Array.Empty<ITrait>(),
                status);

            Assert.IsTrue(instance.CanSuspend);
        }

        [Test]
        public void CanSuspend_WhenNotRunning(
            [Values("SUSPENDED", "TERMINATED")] string status)
        {
            var instance = new InstanceNode(
                new Mock<IProjectWorkspace>().Object,
                1,
                SampleLocatpr,
                Array.Empty<ITrait>(),
                status);

            Assert.IsFalse(instance.CanSuspend);
        }

        //---------------------------------------------------------------------
        // CanReset.
        //---------------------------------------------------------------------

        [Test]
        public void CanReset_WhenRunning(
            [Values("RUNNING", "REPAIRING")] string status)
        {
            var instance = new InstanceNode(
                new Mock<IProjectWorkspace>().Object,
                1,
                SampleLocatpr,
                Array.Empty<ITrait>(),
                status);

            Assert.IsTrue(instance.CanReset);
        }

        [Test]
        public void CanReset_WhenNotRunning(
            [Values("SUSPENDED", "TERMINATED")] string status)
        {
            var instance = new InstanceNode(
                new Mock<IProjectWorkspace>().Object,
                1,
                SampleLocatpr,
                Array.Empty<ITrait>(),
                status);

            Assert.IsFalse(instance.CanReset);
        }
    }
}
