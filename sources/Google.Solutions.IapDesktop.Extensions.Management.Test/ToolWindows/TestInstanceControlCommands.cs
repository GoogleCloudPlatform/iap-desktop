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
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.ToolWindows;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Testing.Apis.Mocks;
using Google.Solutions.Testing.Application.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.ToolWindows
{
    [TestFixture]
    public class TestInstanceControlCommands
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private static IContextCommand<IProjectModelNode> CreateCommand(
            IServiceProvider serviceProvider,
            InstanceControlCommand controlCommand)
        {
            var commands = new InstanceControlCommands(serviceProvider);
            return controlCommand switch
            {
                InstanceControlCommand.Start => commands.ContextMenuStart,
                InstanceControlCommand.Stop => commands.ContextMenuStop,
                InstanceControlCommand.Suspend => commands.ContextMenuSuspend,
                InstanceControlCommand.Resume => commands.ContextMenuResume,
                InstanceControlCommand.Reset => commands.ContextMenuReset,
                _ => throw new ArgumentException(
                    "Unknown InstanceControlCommand: " + controlCommand),
            };
        }

        //---------------------------------------------------------------------
        // ContextMenuStart.
        //---------------------------------------------------------------------

        [Test]
        public void ContextMenuStart_WhenInstanceStartable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.CanStart).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuStart.QueryState(vm.Object));
        }

        [Test]
        public void ContextMenuStart_WhenInstanceNotStartable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.CanStart).Returns(false);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuStart.QueryState(vm.Object));
        }

        //---------------------------------------------------------------------
        // ContextMenuResume.
        //---------------------------------------------------------------------

        [Test]
        public void ContextMenuResume_WhenInstanceResumeable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.CanResume).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuResume.QueryState(vm.Object));
        }

        [Test]
        public void ContextMenuResume_WhenInstanceNotResumeable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.CanResume).Returns(false);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuResume.QueryState(vm.Object));
        }

        //---------------------------------------------------------------------
        // ContextMenuStop.
        //---------------------------------------------------------------------

        [Test]
        public void ContextMenuStop_WhenInstanceStoppable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.CanStop).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuStop.QueryState(vm.Object));
        }

        [Test]
        public void ContextMenuStop_WhenInstanceNotStoppable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.CanStop).Returns(false);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuStop.QueryState(vm.Object));
        }

        //---------------------------------------------------------------------
        // ContextMenuSuspend.
        //---------------------------------------------------------------------

        [Test]
        public void ContextMenuSuspend_WhenInstanceSuspendable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.CanSuspend).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuSuspend.QueryState(vm.Object));
        }

        [Test]
        public void ContextMenuSuspend_WhenInstanceNotSuspendable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.CanSuspend).Returns(false);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuSuspend.QueryState(vm.Object));
        }

        //---------------------------------------------------------------------
        // ContextMenuReset.
        //---------------------------------------------------------------------

        [Test]
        public void ContextMenuReset_WhenInstanceResetable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.CanReset).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuReset.QueryState(vm.Object));
        }

        [Test]
        public void ContextMenuReset_WhenInstanceNotResetable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.CanReset).Returns(false);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuReset.QueryState(vm.Object));
        }

        //---------------------------------------------------------------------
        // ContextMenuXxx.
        //---------------------------------------------------------------------

        [Test]
        public void ContextMenuXxx_WhenNotApplicable_ThenContextMenuXxxIsUnavailable(
            [Values(
                InstanceControlCommand.Start,
                InstanceControlCommand.Stop,
                InstanceControlCommand.Suspend,
                InstanceControlCommand.Resume,
                InstanceControlCommand.Reset)] InstanceControlCommand controlCommand)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var command = CreateCommand(serviceProvider.Object, controlCommand);

            Assert.AreEqual(
                CommandState.Unavailable,
                command.QueryState(new Mock<IProjectModelCloudNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                command.QueryState(new Mock<IProjectModelProjectNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                command.QueryState(new Mock<IProjectModelZoneNode>().Object));
        }

        [Test]
        public async Task ContextMenuXxx_WhenNotConfirmed_ThenContextMenuXxxDoesNothing(
            [Values(
                InstanceControlCommand.Start,
                InstanceControlCommand.Stop,
                InstanceControlCommand.Suspend,
                InstanceControlCommand.Resume,
                InstanceControlCommand.Reset)] InstanceControlCommand controlCommand)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var command = CreateCommand(serviceProvider.Object, controlCommand);

            var confirmation = serviceProvider.AddMock<IConfirmationDialog>();
            confirmation
                .Setup(d => d.Confirm(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(DialogResult.Cancel);

            var startableVm = new Mock<IProjectModelInstanceNode>();
            startableVm.SetupGet(n => n.CanStart).Returns(true);
            startableVm.SetupGet(n => n.Instance).Returns(SampleLocator);
            await command
                .ExecuteAsync(startableVm.Object)
                .ConfigureAwait(false);

            confirmation.Verify(
                d => d.Confirm(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once());
            serviceProvider.Verify(
                s => s.GetService(It.Is<Type>(t => t == typeof(IJobService))),
                Times.Never);
        }

        [Test]
        public async Task ContextMenuXxx_WhenConfirmed_ThenContextMenuXxxControlsInstance(
            [Values(
                InstanceControlCommand.Start,
                InstanceControlCommand.Stop,
                InstanceControlCommand.Suspend,
                InstanceControlCommand.Resume,
                InstanceControlCommand.Reset)] InstanceControlCommand controlCommand)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var command = CreateCommand(serviceProvider.Object, controlCommand);

            serviceProvider.Add<IJobService>(new SynchronousJobService());

            var confirmation = serviceProvider.AddMock<IConfirmationDialog>();
            confirmation
                .Setup(d => d.Confirm(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(DialogResult.Yes);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.CanStart).Returns(true);
            vm.SetupGet(n => n.CanStop).Returns(true);
            vm.SetupGet(n => n.CanSuspend).Returns(true);
            vm.SetupGet(n => n.CanResume).Returns(true);
            vm.SetupGet(n => n.CanReset).Returns(true);
            vm.SetupGet(n => n.Instance).Returns(SampleLocator);
            await command
                .ExecuteAsync(vm.Object)
                .ConfigureAwait(false);

            vm.Verify(
                s => s.ControlInstanceAsync(
                    controlCommand,
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }

        //---------------------------------------------------------------------
        // ContextMenuJoinToActiveDirectory.
        //---------------------------------------------------------------------

        [Test]
        public void ContextMenuJoinToActiveDirectory_WhenInstanceIsWindowsAndRunning_ThenIsEnabled()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Windows);
            vm.SetupGet(n => n.IsRunning).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuJoinToActiveDirectory.QueryState(vm.Object));
        }

        [Test]
        public void ContextMenuJoinToActiveDirectory_WhenInstanceIsWindowsButNotRunning_ThenIsDisabled()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var vm = new Mock<IProjectModelInstanceNode>();
            vm.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Windows);
            vm.SetupGet(n => n.IsRunning).Returns(false);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ContextMenuJoinToActiveDirectory.QueryState(vm.Object));
        }

        [Test]
        public void ContextMenuJoinToActiveDirectory_WhenNotApplicable_ThenIsUnavailable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new InstanceControlCommands(serviceProvider.Object);

            var linuxVm = new Mock<IProjectModelInstanceNode>();
            linuxVm.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Linux);
            linuxVm.SetupGet(n => n.IsRunning).Returns(true);

            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuJoinToActiveDirectory.QueryState(linuxVm.Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuJoinToActiveDirectory.QueryState(new Mock<IProjectModelCloudNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuJoinToActiveDirectory.QueryState(new Mock<IProjectModelProjectNode>().Object));
            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuJoinToActiveDirectory.QueryState(new Mock<IProjectModelZoneNode>().Object));
        }
    }
}
