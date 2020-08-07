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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Activity.Views.SerialOutput;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Views.SerialOutput
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestSerialOutputViewModel : FixtureBase
    {
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

        private SerialOutputViewModel CreateViewModel(ICredential credential)

        {
            var serviceProvider = new ServiceRegistry();
            serviceProvider.AddSingleton<IJobService, MockJobService>();
            serviceProvider.AddSingleton<IComputeEngineAdapter>(
                new ComputeEngineAdapter(credential));

            return new SerialOutputViewModel(serviceProvider, 1)
            {
                IsTailEnabled = false
            };
        }

        //---------------------------------------------------------------------
        // Tailing.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNotBlocked_ThenEnableControlsTailing(
            [WindowsInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            var viewModel = CreateViewModel(await credential.GetCredentialAsync());
            var node = await CreateNode(testInstance, true);
            await viewModel.SwitchToModelAsync(node);

            Assert.IsNull(viewModel.TailCancellationTokenSource, "not tailing yet");

            viewModel.IsTailBlocked = false;
            viewModel.IsTailEnabled = true;

            var tailCts = viewModel.TailCancellationTokenSource;
            Assert.IsNotNull(tailCts, "tailing");

            viewModel.IsTailEnabled = false;

            // CTS cancelled => not tailing.
            Assert.IsTrue(tailCts.IsCancellationRequested, "tailing stopped");
            Assert.IsNull(viewModel.TailCancellationTokenSource);
        }

        [Test]
        public async Task WhenBlocked_ThenEnableHasNoImpactOnTailing(
            [WindowsInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            var viewModel = CreateViewModel(await credential.GetCredentialAsync());
            var node = await CreateNode(testInstance, true);
            await viewModel.SwitchToModelAsync(node);

            viewModel.IsTailBlocked = true;
            viewModel.IsTailEnabled = true;

            Assert.IsNull(viewModel.TailCancellationTokenSource, "not tailing yet");
        }

        [Test]
        public async Task WhenEnabled_ThenBlockControlsTailing(
            [WindowsInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            var viewModel = CreateViewModel(await credential.GetCredentialAsync());
            var node = await CreateNode(testInstance, true);
            await viewModel.SwitchToModelAsync(node);

            Assert.IsNull(viewModel.TailCancellationTokenSource, "not tailing yet");

            viewModel.IsTailEnabled = true;
            viewModel.IsTailBlocked = false;

            var tailCts = viewModel.TailCancellationTokenSource;
            Assert.IsNotNull(tailCts, "tailing");

            viewModel.IsTailBlocked = true;

            // CTS cancelled => not tailing.
            Assert.IsTrue(tailCts.IsCancellationRequested, "tailing stopped");
            Assert.IsNull(viewModel.TailCancellationTokenSource);
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
        public async Task WhenSwitchingToCloudNode_ThenControlsAreDisabled(
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            var viewModel = CreateViewModel(await credential.GetCredentialAsync());
            var node = new Mock<IProjectExplorerCloudNode>();
            await viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(viewModel.IsEnableTailingButtonEnabled);
            Assert.IsFalse(viewModel.IsOutputBoxEnabled);
            StringAssert.Contains(SerialOutputViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }
        [Test]
        public async Task WhenSwitchingToStoppedInstanceNode_ThenControlsAreDisabled(
            [WindowsInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            var viewModel = CreateViewModel(await credential.GetCredentialAsync());
            var node = await CreateNode(testInstance, false);
            await viewModel.SwitchToModelAsync(node);

            Assert.IsFalse(viewModel.IsEnableTailingButtonEnabled);
            Assert.IsFalse(viewModel.IsOutputBoxEnabled);
            StringAssert.Contains(SerialOutputViewModel.DefaultWindowTitle, viewModel.WindowTitle);
        }

        [Test]
        public async Task WhenSwitchingToRunningInstanceNode_ThenOutputIsPopulated(
            [WindowsInstance] InstanceRequest testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] CredentialRequest credential)
        {
            var viewModel = CreateViewModel(await credential.GetCredentialAsync());
            var node = await CreateNode(testInstance, true);
            await viewModel.SwitchToModelAsync(node);

            Assert.IsTrue(viewModel.IsEnableTailingButtonEnabled);
            Assert.IsTrue(viewModel.IsOutputBoxEnabled);
            StringAssert.Contains("Finished running startup scripts", viewModel.Output);

            StringAssert.Contains(SerialOutputViewModel.DefaultWindowTitle, viewModel.WindowTitle);
            StringAssert.Contains(testInstance.Locator.Name, viewModel.WindowTitle);
        }

        [Test]
        public void WhenSwitchingPort_ThenOutputIsPopulated()
        {
            Assert.Inconclusive();
        }
    }
}
