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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.ConnectionSettings;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Testing.Common.Mocks;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.ConnectionSettings
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
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.AddMock<IConnectionSettingsService>()
                .Setup(s => s.IsConnectionSettingsAvailable(It.IsAny<IProjectModelNode>()))
                .Returns(true);
            var context = new Mock<IProjectModelNode>();

            var commands = new ConnectionSettingsCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Enabled, 
                commands.ContextMenuOpen.QueryState(context.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenContextMenuOpenIsUnavailable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.AddMock<IConnectionSettingsService>()
                .Setup(s => s.IsConnectionSettingsAvailable(It.IsAny<IProjectModelNode>()))
                .Returns(false);
            var context = new Mock<IProjectModelNode>();

            var commands = new ConnectionSettingsCommands(serviceProvider.Object);

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
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.AddMock<IConnectionSettingsService>()
                .Setup(s => s.IsConnectionSettingsAvailable(It.IsAny<IProjectModelNode>()))
                .Returns(true);
            var context = new Mock<IProjectModelNode>();

            var commands = new ConnectionSettingsCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Enabled, 
                commands.ToolbarOpen.QueryState(context.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenToolbarOpenIsDisabled()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.AddMock<IConnectionSettingsService>()
                .Setup(s => s.IsConnectionSettingsAvailable(It.IsAny<IProjectModelNode>()))
                .Returns(false);
            var context = new Mock<IProjectModelNode>();

            var commands = new ConnectionSettingsCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Disabled, 
                commands.ToolbarOpen.QueryState(context.Object));
        }
    }
}
