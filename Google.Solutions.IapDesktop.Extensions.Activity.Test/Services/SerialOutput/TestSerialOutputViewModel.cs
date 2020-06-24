//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Test.Testbed;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.SerialOutput;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.EventLog
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestSerialOutputViewModel : FixtureBase
    {
        private SerialOutputViewModel viewModel;

        private class MockJobService : IJobService
        {
            public Task<T> RunInBackground<T>(
                JobDescription jobDescription,
                Func<CancellationToken, Task<T>> jobFunc)
            {
                return jobFunc(CancellationToken.None);
            }
        }

        private static async Task<IProjectExplorerVmInstanceNode> CreateNode(
            InstanceRequest testInstance,
            bool markAsRunning)
        {
            await testInstance.AwaitReady();
            var instanceLocator = await testInstance.GetInstanceAsync();

            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.IsRunning).Returns(markAsRunning);
            node.SetupGet(n => n.ProjectId).Returns(instanceLocator.ProjectId);
            node.SetupGet(n => n.ZoneId).Returns(instanceLocator.Zone);
            node.SetupGet(n => n.InstanceName).Returns(instanceLocator.Name);

            return node.Object;
        }

        [SetUp]
        public void SetUp()
        {
            var serviceProvider = new ServiceRegistry();
            serviceProvider.AddSingleton<IJobService, MockJobService>();
            serviceProvider.AddSingleton<IComputeEngineAdapter>(
                new ComputeEngineAdapter(Defaults.GetCredential()));

            this.viewModel = new SerialOutputViewModel(serviceProvider)
            {
                IsTailEnabled = false
            };
        }

        //---------------------------------------------------------------------
        // Tailing.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNotBlocked_ThenEnableControlsTailing(
            [WindowsInstance] InstanceRequest testInstance)
        {
            var node = await CreateNode(testInstance, true);
            await this.viewModel.SwitchToModelAsync(node);

            Assert.IsNull(this.viewModel.TailCancellationTokenSource, "not tailing yet");

            this.viewModel.IsTailBlocked = false;
            this.viewModel.IsTailEnabled = true;

            var tailCts = this.viewModel.TailCancellationTokenSource;
            Assert.IsNotNull(tailCts, "tailing");

            this.viewModel.IsTailEnabled = false;

            // CTS cancelled => not tailing.
            Assert.IsTrue(tailCts.IsCancellationRequested, "tailing stopped");
            Assert.IsNull(this.viewModel.TailCancellationTokenSource);
        }

        [Test]
        public async Task WhenBlocked_ThenEnableHasNoImpactOnTailing(
            [WindowsInstance] InstanceRequest testInstance)
        {
            var node = await CreateNode(testInstance, true);
            await this.viewModel.SwitchToModelAsync(node);

            this.viewModel.IsTailBlocked = true;
            this.viewModel.IsTailEnabled = true;

            Assert.IsNull(this.viewModel.TailCancellationTokenSource, "not tailing yet");
        }

        [Test]
        public async Task WhenEnabled_ThenBlockControlsTailing(
            [WindowsInstance] InstanceRequest testInstance)
        {
            var node = await CreateNode(testInstance, true);
            await this.viewModel.SwitchToModelAsync(node);

            Assert.IsNull(this.viewModel.TailCancellationTokenSource, "not tailing yet");

            this.viewModel.IsTailEnabled = true;
            this.viewModel.IsTailBlocked = false;

            var tailCts = this.viewModel.TailCancellationTokenSource;
            Assert.IsNotNull(tailCts, "tailing");

            this.viewModel.IsTailBlocked = true;

            // CTS cancelled => not tailing.
            Assert.IsTrue(tailCts.IsCancellationRequested, "tailing stopped");
            Assert.IsNull(this.viewModel.TailCancellationTokenSource);
        }

        //---------------------------------------------------------------------
        // Command state.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNodeIsCloudNode_ThenCommandStateIsUnavailable()
        {
            var node = new Mock<IProjectExplorerCloudNode>().Object;
            Assert.AreEqual(CommandState.Unavailable, SerialOutputViewModel.GetCommandState(node));
        }

        [Test]
        public void WhenNodeIsProjectNode_ThenCommandStateIsUnavailable()
        {
            var node = new Mock<IProjectExplorerProjectNode>().Object;
            Assert.AreEqual(CommandState.Unavailable, SerialOutputViewModel.GetCommandState(node));
        }

        [Test]
        public void WhenNodeIsZoneNode_ThenCommandStateIsUnavailable()
        {
            var node = new Mock<IProjectExplorerZoneNode>().Object;
            Assert.AreEqual(CommandState.Unavailable, SerialOutputViewModel.GetCommandState(node));
        }

        [Test]
        public void WhenNodeIsVmNodeAndRunning_ThenCommandStateIsEnabled()
        {
            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.IsRunning).Returns(true);
            Assert.AreEqual(CommandState.Enabled, SerialOutputViewModel.GetCommandState(node.Object));
        }

        [Test]
        public void WhenNodeIsVmNodeAndStopped_ThenCommandStateIsEnabled()
        {
            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.IsRunning).Returns(false);
            Assert.AreEqual(CommandState.Disabled, SerialOutputViewModel.GetCommandState(node.Object));
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingToCloudNode_ThenControlsAreDisabled()
        {
            var node = new Mock<IProjectExplorerCloudNode>();
            await this.viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(this.viewModel.IsPortComboBoxEnabled);
            Assert.IsFalse(this.viewModel.IsOutputBoxEnabled);
        }
        [Test]
        public async Task WhenSwitchingToStoppedInstanceNode_ThenControlsAreDisabled(
            [WindowsInstance] InstanceRequest testInstance)
        {
            var node = await CreateNode(testInstance, false);
            await this.viewModel.SwitchToModelAsync(node);

            Assert.IsFalse(this.viewModel.IsPortComboBoxEnabled);
            Assert.IsFalse(this.viewModel.IsOutputBoxEnabled);
        }

        [Test]
        public async Task WhenSwitchingToRunningInstanceNode_ThenOutputIsPopulated(
            [WindowsInstance] InstanceRequest testInstance)
        {
            var node = await CreateNode(testInstance, true);
            await this.viewModel.SwitchToModelAsync(node);

            Assert.IsTrue(this.viewModel.IsPortComboBoxEnabled);
            Assert.IsTrue(this.viewModel.IsOutputBoxEnabled);
            StringAssert.Contains("Instance setup finished", this.viewModel.Output);
        }

        [Test]
        public void WhenSwitchingPort_ThenOutputIsPopulated()
        {
            Assert.Inconclusive();
        }
    }
}
