//
// Copyright 2023 Google LLC
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
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel.Nodes;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Core.Test.ProjectModel
{
    [TestFixture]
    public class TestInstanceNode
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // TargetName.
        //---------------------------------------------------------------------

        [Test]
        public void TargetName()
        {
            var node = new InstanceNode(
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "RUNNING");

            Assert.AreEqual(SampleLocator.Name, node.TargetName);
        }

        //---------------------------------------------------------------------
        // OperatingSystem.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNodeHasWindowsTraits_ThenOperatingSystemIsWindows()
        {
            var node = new InstanceNode(
                1,
                SampleLocator,
                new IProtocolTargetTrait[] { InstanceTrait.Instance, WindowsTrait.Instance },
                "RUNNING");

            Assert.AreEqual(OperatingSystems.Windows, node.OperatingSystem);
        }

        [Test]
        public void WhenNodeHasNoOsTraits_ThenOperatingSystemIsLinux()
        {
            var node = new InstanceNode(
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "RUNNING");

            Assert.AreEqual(OperatingSystems.Linux, node.OperatingSystem);
        }

        //---------------------------------------------------------------------
        // IsRunning.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRunning_ThenPropertiesAreSet()
        {
            var node = new InstanceNode(
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "RUNNING");

            Assert.IsTrue(node.IsRunning);
            Assert.IsFalse(node.CanStart);
            Assert.IsTrue(node.CanReset);
            Assert.IsTrue(node.CanSuspend);
            Assert.IsFalse(node.CanResume);
            Assert.IsTrue(node.CanStop);
        }

        [Test]
        public void WhenTerminated_ThenPropertiesAreSet()
        {
            var node = new InstanceNode(
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "TERMINATED");

            Assert.IsFalse(node.IsRunning);
            Assert.IsTrue(node.CanStart);
            Assert.IsFalse(node.CanReset);
            Assert.IsFalse(node.CanSuspend);
            Assert.IsFalse(node.CanResume);
            Assert.IsFalse(node.CanStop);
        }

        [Test]
        public void WhenSuspended_ThenPropertiesAreSet()
        {
            var node = new InstanceNode(
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "SUSPENDED");

            Assert.IsFalse(node.IsRunning);
            Assert.IsFalse(node.CanStart);
            Assert.IsFalse(node.CanReset);
            Assert.IsFalse(node.CanSuspend);
            Assert.IsTrue(node.CanResume);
            Assert.IsFalse(node.CanStop);
        }

        [Test]
        public void WhenRepariring_ThenPropertiesAreSet()
        {
            var node = new InstanceNode(
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "REPAIRING");

            Assert.IsFalse(node.IsRunning);
            Assert.IsFalse(node.CanStart);
            Assert.IsTrue(node.CanReset);
            Assert.IsTrue(node.CanSuspend);
            Assert.IsFalse(node.CanResume);
            Assert.IsTrue(node.CanStop);
        }
    }
}
