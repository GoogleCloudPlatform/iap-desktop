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
using Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.PackageInventory;
using Google.Solutions.Mvvm.Binding.Commands;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.ToolWindows.PackageInventory
{
    [TestFixture]
    public class TestPackageInventoryCommands
    {
        //---------------------------------------------------------------------
        // ContextMenuOpenInstalledPackages.
        //---------------------------------------------------------------------

        [Test]
        public void ContextMenuOpenInstalledPackages_WhenApplicable()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var commands = new PackageInventoryCommands(toolWindowHost.Object);

            Assert.That(
                commands.ContextMenuOpenInstalledPackages.QueryState(new Mock<IProjectModelProjectNode>().Object), Is.EqualTo(CommandState.Enabled));
            Assert.That(
                commands.ContextMenuOpenInstalledPackages.QueryState(new Mock<IProjectModelZoneNode>().Object), Is.EqualTo(CommandState.Enabled));
            Assert.That(
                commands.ContextMenuOpenInstalledPackages.QueryState(new Mock<IProjectModelInstanceNode>().Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void ContextMenuOpenInstalledPackages_WhenNotApplicable()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var commands = new PackageInventoryCommands(toolWindowHost.Object);

            Assert.That(
                commands.ContextMenuOpenInstalledPackages.QueryState(new Mock<IProjectModelCloudNode>().Object), Is.EqualTo(CommandState.Unavailable));
        }

        //---------------------------------------------------------------------
        // ContextMenuOpenAvailablePackages.
        //---------------------------------------------------------------------

        [Test]
        public void ContextMenuOpenAvailablePackages_WhenApplicable()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var commands = new PackageInventoryCommands(toolWindowHost.Object);

            Assert.That(
                commands.ContextMenuOpenAvailablePackages.QueryState(new Mock<IProjectModelProjectNode>().Object), Is.EqualTo(CommandState.Enabled));
            Assert.That(
                commands.ContextMenuOpenAvailablePackages.QueryState(new Mock<IProjectModelZoneNode>().Object), Is.EqualTo(CommandState.Enabled));
            Assert.That(
                commands.ContextMenuOpenAvailablePackages.QueryState(new Mock<IProjectModelInstanceNode>().Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void ContextMenuOpenAvailablePackages_WhenNotApplicable()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var commands = new PackageInventoryCommands(toolWindowHost.Object);

            Assert.That(
                commands.ContextMenuOpenAvailablePackages.QueryState(new Mock<IProjectModelCloudNode>().Object), Is.EqualTo(CommandState.Unavailable));
        }

        //---------------------------------------------------------------------
        // WindowMenuOpenInstalledPackages.
        //---------------------------------------------------------------------

        [Test]
        public void WindowMenuOpenInstalledPackages()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var context = new Mock<IMainWindow>();

            var commands = new PackageInventoryCommands(toolWindowHost.Object);

            Assert.That(
                commands.WindowMenuOpenInstalledPackages.QueryState(context.Object), Is.EqualTo(CommandState.Enabled));
        }

        //---------------------------------------------------------------------
        // WindowMenuOpenAvailablePackages.
        //---------------------------------------------------------------------

        [Test]
        public void WindowMenuOpenAvailablePackages()
        {
            var toolWindowHost = new Mock<IToolWindowHost>();
            var context = new Mock<IMainWindow>();

            var commands = new PackageInventoryCommands(toolWindowHost.Object);

            Assert.That(
                commands.WindowMenuOpenAvailablePackages.QueryState(context.Object), Is.EqualTo(CommandState.Enabled));
        }
    }
}
