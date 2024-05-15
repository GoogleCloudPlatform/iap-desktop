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
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Settings;
using Google.Solutions.Mvvm.Binding.Commands;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Settings
{
    [TestFixture]
    public class TestConnectionSettingsCommands
    {
        //---------------------------------------------------------------------
        // ContextMenuOpen.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicable_ThenContextMenuOpenIsEnabled()
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.IsConnectionSettingsAvailable(It.IsAny<IProjectModelNode>()))
                .Returns(true);
            var context = new Mock<IProjectModelNode>();

            var commands = new ConnectionSettingsCommands(
                new Mock<IToolWindowHost>().Object,
                settingsService.Object);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuOpen.QueryState(context.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenContextMenuOpenIsUnavailable()
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.IsConnectionSettingsAvailable(It.IsAny<IProjectModelNode>()))
                .Returns(false);
            var context = new Mock<IProjectModelNode>();

            var commands = new ConnectionSettingsCommands(
                new Mock<IToolWindowHost>().Object,
                settingsService.Object);

            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuOpen.QueryState(context.Object));
        }

        //---------------------------------------------------------------------
        // ToolbarOpen.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicable_ThenToolbarOpenIsEnabled()
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.IsConnectionSettingsAvailable(It.IsAny<IProjectModelNode>()))
                .Returns(true);
            var context = new Mock<IProjectModelNode>();

            var commands = new ConnectionSettingsCommands(
                new Mock<IToolWindowHost>().Object,
                settingsService.Object);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ToolbarOpen.QueryState(context.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenToolbarOpenIsDisabled()
        {
            var settingsService = new Mock<IConnectionSettingsService>();
            settingsService
                .Setup(s => s.IsConnectionSettingsAvailable(It.IsAny<IProjectModelNode>()))
                .Returns(false);
            var context = new Mock<IProjectModelNode>();

            var commands = new ConnectionSettingsCommands(
                new Mock<IToolWindowHost>().Object,
                settingsService.Object);

            Assert.AreEqual(
                CommandState.Disabled,
                commands.ToolbarOpen.QueryState(context.Object));
        }
    }
}
