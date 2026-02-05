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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.SerialOutput;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.Mocks;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.ToolWindows.SerialOutput
{
    [TestFixture]
    [UsesCloudResources]
    public class TestSerialOutputViewModel : ApplicationFixtureBase
    {
        private static async Task<IProjectModelInstanceNode> CreateNode(
            ResourceTask<InstanceLocator> testInstance,
            bool markAsRunning)
        {
            await testInstance;
            var instanceLocator = await testInstance;

            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.IsRunning).Returns(markAsRunning);
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator(
                    instanceLocator.ProjectId,
                    instanceLocator.Zone,
                    instanceLocator.Name));

            return node.Object;
        }

        private static SerialOutputViewModel CreateViewModel(IAuthorization authorization)

        {
            var serviceProvider = new ServiceRegistry();
            serviceProvider.AddSingleton<IJobService, SynchronousJobService>();
            serviceProvider.AddSingleton<IComputeEngineClient>(
                new ComputeEngineClient(
                    ComputeEngineClient.CreateEndpoint(),
                    authorization,
                    TestProject.UserAgent));

            return new SerialOutputViewModel(serviceProvider)
            {
                SerialPortNumber = 1,
                IsTailEnabled = false
            };
        }

        //---------------------------------------------------------------------
        // Tailing.
        //---------------------------------------------------------------------

        [Test]
        public async Task Tailing_WhenNotBlocked_ThenEnableControlsTailing(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var viewModel = CreateViewModel(await auth);
            var node = await CreateNode(testInstance, true).ConfigureAwait(true);
            await viewModel
                .SwitchToModelAsync(node)
                .ConfigureAwait(true);

            Assert.IsNull(viewModel.TailCancellationTokenSource, "not tailing yet");

            viewModel.IsTailBlocked = false;
            viewModel.IsTailEnabled = true;

            var tailCts = viewModel.TailCancellationTokenSource;
            Assert.IsNotNull(tailCts, "tailing");

            viewModel.IsTailEnabled = false;

            // CTS cancelled => not tailing.
            Assert.That(tailCts!.IsCancellationRequested, Is.True, "tailing stopped");
            Assert.IsNull(viewModel.TailCancellationTokenSource);
        }

        [Test]
        public async Task Tailing_WhenBlocked_ThenEnableHasNoImpactOnTailing(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var viewModel = CreateViewModel(await auth);
            var node = await CreateNode(testInstance, true).ConfigureAwait(true);
            await viewModel
                .SwitchToModelAsync(node)
                .ConfigureAwait(true);

            viewModel.IsTailBlocked = true;
            viewModel.IsTailEnabled = true;

            Assert.IsNull(viewModel.TailCancellationTokenSource, "not tailing yet");
        }

        [Test]
        public async Task Tailing_WhenEnabled_ThenBlockControlsTailing(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var viewModel = CreateViewModel(await auth);
            var node = await CreateNode(testInstance, true).ConfigureAwait(true);
            await viewModel
                .SwitchToModelAsync(node)
                .ConfigureAwait(true);

            Assert.IsNull(viewModel.TailCancellationTokenSource, "not tailing yet");

            viewModel.IsTailEnabled = true;
            viewModel.IsTailBlocked = false;

            var tailCts = viewModel.TailCancellationTokenSource;
            Assert.IsNotNull(tailCts, "tailing");

            viewModel.IsTailBlocked = true;

            // CTS cancelled => not tailing.
            Assert.That(tailCts!.IsCancellationRequested, Is.True, "tailing stopped");
            Assert.IsNull(viewModel.TailCancellationTokenSource);
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task SwitchToModel_WhenCloudNode_ThenControlsAreDisabled(
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var viewModel = CreateViewModel(await auth);
            var node = new Mock<IProjectModelCloudNode>();
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.That(viewModel.IsEnableTailingButtonEnabled, Is.False);
            Assert.That(viewModel.IsOutputBoxEnabled, Is.False);
            Assert.That(viewModel.WindowTitle, Does.Contain(SerialOutputViewModel.DefaultWindowTitle));
        }
        [Test]
        public async Task SwitchToModel_WhenStoppedInstanceNode_ThenControlsAreDisabled(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var viewModel = CreateViewModel(await auth);
            var node = await CreateNode(testInstance, false).ConfigureAwait(true);
            await viewModel
                .SwitchToModelAsync(node)
                .ConfigureAwait(true);

            Assert.That(viewModel.IsEnableTailingButtonEnabled, Is.False);
            Assert.That(viewModel.IsOutputBoxEnabled, Is.False);
            Assert.That(viewModel.WindowTitle, Does.Contain(SerialOutputViewModel.DefaultWindowTitle));
        }

        [Test]
        public async Task SwitchToModel_WhenRunningInstanceNode_ThenOutputIsPopulated(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.ComputeViewer)] ResourceTask<IAuthorization> auth)
        {
            var viewModel = CreateViewModel(await auth);
            var node = await CreateNode(testInstance, true).ConfigureAwait(true);
            await viewModel
                .SwitchToModelAsync(node)
                .ConfigureAwait(true);

            var instanceLocator = await testInstance;

            Assert.That(viewModel.IsEnableTailingButtonEnabled, Is.True);
            Assert.That(viewModel.IsOutputBoxEnabled, Is.True);
            Assert.That(viewModel.Output, Does.Contain("Finished running startup scripts"));

            Assert.That(viewModel.WindowTitle, Does.Contain(SerialOutputViewModel.DefaultWindowTitle));
            Assert.That(viewModel.WindowTitle, Does.Contain(instanceLocator.Name));
        }
    }
}
