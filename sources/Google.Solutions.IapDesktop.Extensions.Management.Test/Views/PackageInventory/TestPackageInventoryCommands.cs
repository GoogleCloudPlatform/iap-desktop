﻿//
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

using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Management.Views.PackageInventory;
using Google.Solutions.Mvvm.Binding.Commands;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.Views.PackageInventory
{
    [TestFixture]
    public class TestPackageInventoryCommands
    {
        //---------------------------------------------------------------------
        // ContextMenuOpenInstalledPackages.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicable_ThenContextMenuOpenInstalledPackagesIsEnabled()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new PackageInventoryCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuOpenInstalledPackages.QueryState(new Mock<IProjectModelProjectNode>().Object));
            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuOpenInstalledPackages.QueryState(new Mock<IProjectModelZoneNode>().Object));
            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuOpenInstalledPackages.QueryState(new Mock<IProjectModelInstanceNode>().Object));
        }

        [Test]
        public void WhenNotApplicable_ThenContextMenuOpenInstalledPackagesIsUnavailable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new PackageInventoryCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuOpenInstalledPackages.QueryState(new Mock<IProjectModelCloudNode>().Object));
        }

        //---------------------------------------------------------------------
        // ContextMenuOpenAvailablePackages.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicable_ThenContextMenuOpenAvailablePackagesIsEnabled()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new PackageInventoryCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuOpenAvailablePackages.QueryState(new Mock<IProjectModelProjectNode>().Object));
            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuOpenAvailablePackages.QueryState(new Mock<IProjectModelZoneNode>().Object));
            Assert.AreEqual(
                CommandState.Enabled,
                commands.ContextMenuOpenAvailablePackages.QueryState(new Mock<IProjectModelInstanceNode>().Object));
        }

        [Test]
        public void WhenNotApplicable_ThenContextMenuOpenAvailablePackagesIsUnavailable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var commands = new PackageInventoryCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Unavailable,
                commands.ContextMenuOpenAvailablePackages.QueryState(new Mock<IProjectModelCloudNode>().Object));
        }

        //---------------------------------------------------------------------
        // WindowMenuOpenInstalledPackages.
        //---------------------------------------------------------------------

        [Test]
        public void WindowMenuOpenInstalledPackagesEnabled()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var context = new Mock<IMainWindow>();

            var commands = new PackageInventoryCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.WindowMenuOpenInstalledPackages.QueryState(context.Object));
        }

        //---------------------------------------------------------------------
        // WindowMenuOpenAvailablePackages.
        //---------------------------------------------------------------------

        [Test]
        public void WindowMenuOpenAvailablePackagesEnabled()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var context = new Mock<IMainWindow>();

            var commands = new PackageInventoryCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.WindowMenuOpenAvailablePackages.QueryState(context.Object));
        }
    }
}
