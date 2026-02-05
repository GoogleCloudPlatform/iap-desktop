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

using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel.Nodes;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.ProjectModel.Nodes
{
    [TestFixture]
    public class TestInstanceNode
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private static ProjectWorkspace CreateWorkspace()
        {
            return new ProjectWorkspace(
                new Mock<IComputeEngineClient>().Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IProjectRepository>().Object,
                new Mock<IEventQueue>().Object);
        }

        //---------------------------------------------------------------------
        // TargetName.
        //---------------------------------------------------------------------

        [Test]
        public void TargetName()
        {
            var node = new InstanceNode(
                CreateWorkspace(),
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "RUNNING");

            Assert.That(node.TargetName, Is.EqualTo(SampleLocator.Name));
        }

        //---------------------------------------------------------------------
        // OperatingSystem.
        //---------------------------------------------------------------------

        [Test]
        public void OperatingSystem_WhenNodeHasWindowsTraits()
        {
            var node = new InstanceNode(
                CreateWorkspace(),
                1,
                SampleLocator,
                new ITrait[] { InstanceTrait.Instance, WindowsTrait.Instance },
                "RUNNING");

            Assert.That(node.OperatingSystem, Is.EqualTo(OperatingSystems.Windows));
        }

        [Test]
        public void OperatingSystem_WhenNodeHasNoOsTraits()
        {
            var node = new InstanceNode(
                CreateWorkspace(),
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "RUNNING");

            Assert.That(node.OperatingSystem, Is.EqualTo(OperatingSystems.Linux));
        }

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        [Test]
        public void CanXxx_WhenRunning()
        {
            var node = new InstanceNode(
                CreateWorkspace(),
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "RUNNING");

            Assert.IsTrue(node.IsRunning);
            Assert.That(node.CanStart, Is.False);
            Assert.IsTrue(node.CanReset);
            Assert.IsTrue(node.CanSuspend);
            Assert.That(node.CanResume, Is.False);
            Assert.IsTrue(node.CanStop);
        }

        [Test]
        public void CanXxx_WhenTerminated()
        {
            var node = new InstanceNode(
                CreateWorkspace(),
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "TERMINATED");

            Assert.That(node.IsRunning, Is.False);
            Assert.IsTrue(node.CanStart);
            Assert.That(node.CanReset, Is.False);
            Assert.That(node.CanSuspend, Is.False);
            Assert.That(node.CanResume, Is.False);
            Assert.That(node.CanStop, Is.False);
        }

        [Test]
        public void CanXxx_WhenSuspended()
        {
            var node = new InstanceNode(
                CreateWorkspace(),
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "SUSPENDED");

            Assert.That(node.IsRunning, Is.False);
            Assert.That(node.CanStart, Is.False);
            Assert.That(node.CanReset, Is.False);
            Assert.That(node.CanSuspend, Is.False);
            Assert.IsTrue(node.CanResume);
            Assert.That(node.CanStop, Is.False);
        }

        [Test]
        public void CanXxx()
        {
            var node = new InstanceNode(
                CreateWorkspace(),
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "REPAIRING");

            Assert.That(node.IsRunning, Is.False);
            Assert.That(node.CanStart, Is.False);
            Assert.IsTrue(node.CanReset);
            Assert.IsTrue(node.CanSuspend);
            Assert.That(node.CanResume, Is.False);
            Assert.IsTrue(node.CanStop);
        }

        //---------------------------------------------------------------------
        // ControlInstance.
        //---------------------------------------------------------------------

        [Test]
        public async Task ControlInstance_WhenStartOrResumeSucceeds(
            [Values(
                InstanceControlCommand.Reset,
                InstanceControlCommand.Start,
                InstanceControlCommand.Resume)]
            InstanceControlCommand command)
        {
            var computeAdapter = new Mock<IComputeEngineClient>();
            var eventQueue = new Mock<IEventQueue>();
            var workspace = new ProjectWorkspace(
                computeAdapter.Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IProjectRepository>().Object,
                eventQueue.Object);

            var node = new InstanceNode(
                workspace,
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "RUNNING");

            await node.ControlInstanceAsync(
                    command,
                    CancellationToken.None)
                .ConfigureAwait(false);

            computeAdapter.Verify(
                c => c.ControlInstanceAsync(
                    SampleLocator,
                    command,
                    CancellationToken.None),
                Times.Once);
            eventQueue.Verify(s => s.PublishAsync(
                It.Is<InstanceStateChangedEvent>(e => e.Instance == SampleLocator && e.IsRunning)),
                Times.Once);
        }

        [Test]
        public async Task ControlInstance_WhenStopOrSuspendSucceeds(
            [Values(
                InstanceControlCommand.Stop,
                InstanceControlCommand.Suspend)]
            InstanceControlCommand command)
        {
            var computeAdapter = new Mock<IComputeEngineClient>();
            var eventQueue = new Mock<IEventQueue>();
            var workspace = new ProjectWorkspace(
                computeAdapter.Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IProjectRepository>().Object,
                eventQueue.Object);

            var node = new InstanceNode(
                workspace,
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "RUNNING");

            await node.ControlInstanceAsync(
                    command,
                    CancellationToken.None)
                .ConfigureAwait(false);

            computeAdapter.Verify(
                c => c.ControlInstanceAsync(
                    SampleLocator,
                    command,
                    CancellationToken.None),
                Times.Once);
            eventQueue.Verify(s => s.PublishAsync(
                It.Is<InstanceStateChangedEvent>(e => e.Instance == SampleLocator && !e.IsRunning)),
                Times.Once);
        }

        [Test]
        public void ControlInstance_WhenOperationFails()
        {
            var computeAdapter = new Mock<IComputeEngineClient>();
            computeAdapter
                .Setup(a => a.ControlInstanceAsync(
                    SampleLocator,
                    InstanceControlCommand.Start,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("mock"));

            var eventQueue = new Mock<IEventQueue>();
            var workspace = new ProjectWorkspace(
                computeAdapter.Object,
                new Mock<IResourceManagerClient>().Object,
                new Mock<IProjectRepository>().Object,
                eventQueue.Object);

            var node = new InstanceNode(
                workspace,
                1,
                SampleLocator,
                new[] { InstanceTrait.Instance },
                "RUNNING");

            ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                () => node.ControlInstanceAsync(
                    InstanceControlCommand.Start,
                    CancellationToken.None).Wait());

            computeAdapter.Verify(
                c => c.ControlInstanceAsync(
                    SampleLocator,
                    InstanceControlCommand.Start,
                    CancellationToken.None),
                Times.Once);
            eventQueue.Verify(s => s.PublishAsync(
                It.IsAny<InstanceStateChangedEvent>()),
                Times.Never);
        }
    }
}
