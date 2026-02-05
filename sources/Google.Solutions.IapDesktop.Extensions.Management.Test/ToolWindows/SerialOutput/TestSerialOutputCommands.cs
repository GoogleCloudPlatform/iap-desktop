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

using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.SerialOutput;
using Google.Solutions.Mvvm.Binding.Commands;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.ToolWindows.SerialOutput
{
    [TestFixture]
    public class TestSerialOutputCommands
    {
        //---------------------------------------------------------------------
        // ContextMenuOpenCom1.
        //---------------------------------------------------------------------

        [Test]
        public void ContextMenuOpenCom1_WhenVmRunning_ThenIsEnabled()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var commands = new SerialOutputCommands(toolWindowHost.Object);

            var runningVm = new Mock<IProjectModelInstanceNode>();
            runningVm.SetupGet(n => n.IsRunning).Returns(true);

            Assert.That(
                commands.ContextMenuOpenCom1.QueryState(runningVm.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void ContextMenuOpenCom1_WhenVmStopped_ThenIsDisabled()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var commands = new SerialOutputCommands(toolWindowHost.Object);

            var stoppedVm = new Mock<IProjectModelInstanceNode>();
            stoppedVm.SetupGet(n => n.IsRunning).Returns(false);

            Assert.That(
                commands.ContextMenuOpenCom1.QueryState(stoppedVm.Object), Is.EqualTo(CommandState.Disabled));
        }

        [Test]
        public void ContextMenuOpenCom1_WhenNotApplicable_ThenIsUnavailable()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var commands = new SerialOutputCommands(toolWindowHost.Object);

            Assert.That(
                commands.ContextMenuOpenCom1.QueryState(new Mock<IProjectModelCloudNode>().Object), Is.EqualTo(CommandState.Unavailable));
            Assert.That(
                commands.ContextMenuOpenCom1.QueryState(new Mock<IProjectModelProjectNode>().Object), Is.EqualTo(CommandState.Unavailable));
            Assert.That(
                commands.ContextMenuOpenCom1.QueryState(new Mock<IProjectModelZoneNode>().Object), Is.EqualTo(CommandState.Unavailable));
        }

        //---------------------------------------------------------------------
        // WindowMenuOpenCom1.
        //---------------------------------------------------------------------

        [Test]
        public void WindowMenuOpenCom1_IsEnabled()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var context = new Mock<IMainWindow>();

            var commands = new SerialOutputCommands(toolWindowHost.Object);

            Assert.That(
                commands.WindowMenuOpenCom1.QueryState(context.Object), Is.EqualTo(CommandState.Enabled));
        }

        //---------------------------------------------------------------------
        // WindowMenuOpenCom3.
        //---------------------------------------------------------------------

        [Test]
        public void WindowMenuOpenCom3_IsEnabled()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var context = new Mock<IMainWindow>();

            var commands = new SerialOutputCommands(toolWindowHost.Object);

            Assert.That(
                commands.WindowMenuOpenCom3.QueryState(context.Object), Is.EqualTo(CommandState.Enabled));
        }

        //---------------------------------------------------------------------
        // WindowMenuOpenCom3.
        //---------------------------------------------------------------------

        [Test]
        public void WindowMenuOpenCom4_IsEnabled()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var context = new Mock<IMainWindow>();

            var commands = new SerialOutputCommands(toolWindowHost.Object);

            Assert.That(
                commands.WindowMenuOpenCom4.QueryState(context.Object), Is.EqualTo(CommandState.Enabled));
        }
    }
}
