﻿//
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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Test.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Views.ProjectPicker;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Views
{
    [TestFixture]
    public class TestProjectExplorer : WindowTestFixtureBase
    {
        private static TreeNode GetRootNode(ProjectExplorerWindow window)
        {
            var tree = window.GetChild<TreeView>("treeView");
            return tree.Nodes.Cast<TreeNode>().First();
        }

        [SetUp]
        public void RegisterServices()
        {
            this.serviceRegistry.AddSingleton<IGlobalSessionBroker, GlobalSessionBroker>();
        }

        [Test]
        public void WhenNoProjectsLoaded_ThenRootNodeIsEmptyAndProjectPickerOpens()
        {
            var projectPicker = new Mock<IProjectPickerWindow>();
            projectPicker.Setup(p => p.SelectProject(It.IsAny<IWin32Window>())).Returns((string)null);

            this.serviceRegistry.AddSingleton<IProjectPickerWindow>(projectPicker.Object);
            this.serviceRegistry.AddMock<IComputeEngineAdapter>();

            // Open window.
            var window = new ProjectExplorerWindow(this.serviceProvider);
            window.ShowWindow();
            Delay(TimeSpan.FromMilliseconds(100));

            // Check tree.
            var rootNode = GetRootNode(window);
            Assert.IsInstanceOf(typeof(CloudNode), rootNode);
            Assert.AreEqual(0, rootNode.Nodes.Count);

            // Check picker
            projectPicker.Verify(p => p.SelectProject(It.IsAny<IWin32Window>()), Times.Once);

            Assert.IsNull(this.ExceptionShown);
        }

        [Test]
        public void WhenProjectAdded_ThenWindowsInstancesAreListed()
        {
            // Add a project.
            this.serviceProvider.GetService<IProjectRepository>().AddProjectAsync("project-1").Wait();

            // Add some instances.
            var instances = new[]
            {
                CreateInstance("instance-1a", "antarctica1-a", true),
                CreateInstance("instance-1b", "antarctica1-b", true)
            };

            // Open window.
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            this.serviceRegistry.AddSingleton<IComputeEngineAdapter>(computeEngineAdapter.Object);
            computeEngineAdapter
                .Setup(o => o.ListInstancesAsync("project-1", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<Instance>>(instances));

            var window = new ProjectExplorerWindow(this.serviceProvider);
            window.ShowWindow();
            Delay(TimeSpan.FromMilliseconds(100));

            // Check tree.
            var rootNode = GetRootNode(window);
            Assert.IsInstanceOf(typeof(CloudNode), rootNode);
            Assert.AreEqual(1, rootNode.Nodes.Count);

            var projectNode = (ProjectNode)rootNode.FirstNode;
            Assert.AreEqual("project-1", projectNode.Text);
            Assert.AreEqual(2, projectNode.Nodes.Count);

            var zoneAnode = (ZoneNode)projectNode.FirstNode;
            Assert.AreEqual("antarctica1-a", zoneAnode.Text);
            Assert.AreEqual(1, zoneAnode.Nodes.Count);

            var vmNode = (VmInstanceNode)zoneAnode.FirstNode;
            Assert.AreEqual("instance-1a", vmNode.Text);
            Assert.AreEqual(0, vmNode.Nodes.Count);

            Assert.IsNull(this.ExceptionShown);
        }

        [Test]
        public void WhenProjectAdded_ThenLinuxInstancesAreListed()
        {
            // Add a project.
            this.serviceProvider.GetService<IProjectRepository>().AddProjectAsync("project-1").Wait();

            // Add some instances.
            var instances = new[]
            {
                CreateInstance("windows", "antarctica1-a", true),
                CreateInstance("linux", "antarctica1-b", false)
            };

            // Open window.
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            this.serviceRegistry.AddSingleton<IComputeEngineAdapter>(computeEngineAdapter.Object);
            computeEngineAdapter
                .Setup(o => o.ListInstancesAsync("project-1", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<Instance>>(instances));

            var window = new ProjectExplorerWindow(this.serviceProvider);
            window.ShowWindow();
            Delay(TimeSpan.FromMilliseconds(100));

            // Check tree.
            var rootNode = GetRootNode(window);
            Assert.IsInstanceOf(typeof(CloudNode), rootNode);
            Assert.AreEqual(1, rootNode.Nodes.Count);

            var projectNode = (ProjectNode)rootNode.FirstNode;
            Assert.AreEqual("project-1", projectNode.Text);
            Assert.AreEqual(2, projectNode.Nodes.Count);

            var zoneAnode = (ZoneNode)projectNode.FirstNode;
            Assert.AreEqual("antarctica1-a", zoneAnode.Text);
            Assert.AreEqual(1, zoneAnode.Nodes.Count);

            var zoneBnode = (ZoneNode)projectNode.LastNode;
            Assert.AreEqual("antarctica1-b", zoneBnode.Text);
            Assert.AreEqual(1, zoneBnode.Nodes.Count);

            Assert.AreEqual("windows", ((VmInstanceNode)zoneAnode.FirstNode).Text);
            Assert.AreEqual("linux", ((VmInstanceNode)zoneBnode.FirstNode).Text);

            Assert.IsNull(this.ExceptionShown);
        }

        [Test]
        public void WhenProjectAdded_ThenInstancesWithoutDiskAreIgnored()
        {
            // Add a project.
            this.serviceProvider.GetService<IProjectRepository>().AddProjectAsync("project-1").Wait();

            // Add some instances.
            var instances = new[]
            {
                CreateInstance("windows", "antarctica1-a", true),
                new Instance()
                {
                    Id = 1,
                    Name = "nodisk",
                    Zone = "projects/-/zones/antarctica1-a",
                    MachineType = "zones/-/machineTypes/n1-standard-1"
                }
            };

            // Open window.
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            this.serviceRegistry.AddSingleton<IComputeEngineAdapter>(computeEngineAdapter.Object);
            computeEngineAdapter
                .Setup(o => o.ListInstancesAsync("project-1", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<Instance>>(instances));

            var window = new ProjectExplorerWindow(this.serviceProvider);
            window.ShowWindow();
            Delay(TimeSpan.FromMilliseconds(100));

            // Check tree.
            var rootNode = GetRootNode(window);
            Assert.IsInstanceOf(typeof(CloudNode), rootNode);
            Assert.AreEqual(1, rootNode.Nodes.Count);

            var projectNode = (ProjectNode)rootNode.FirstNode;
            Assert.AreEqual("project-1", projectNode.Text);
            Assert.AreEqual(1, projectNode.Nodes.Count);

            var zoneAnode = (ZoneNode)projectNode.FirstNode;
            Assert.AreEqual("antarctica1-a", zoneAnode.Text);
            Assert.AreEqual(1, zoneAnode.Nodes.Count);

            var vmNode = (VmInstanceNode)zoneAnode.FirstNode;
            Assert.AreEqual(0, vmNode.Nodes.Count);
            Assert.AreEqual("windows", vmNode.Text);

            Assert.IsNull(this.ExceptionShown);
        }

        [Test]
        public void WhenQueryInstanceFails_ProjectIsShownAsEmpty()
        {
            // Add a project.
            this.serviceProvider.GetService<IProjectRepository>().AddProjectAsync("valid-project").Wait();
            this.serviceProvider.GetService<IProjectRepository>().AddProjectAsync("forbidden-project").Wait();

            // Add some instances to project-1.
            var instances = new[]
            {
                CreateInstance("windows", "antarctica1-a", true)
            };

            // Open window.
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            this.serviceRegistry.AddSingleton<IComputeEngineAdapter>(computeEngineAdapter.Object);

            computeEngineAdapter
                .Setup(o => o.ListInstancesAsync("valid-project", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<Instance>>(instances));
            computeEngineAdapter
                .Setup(o => o.ListInstancesAsync("forbidden-project", It.IsAny<CancellationToken>()))
                .Throws(new AdapterException("Access denied or something", null));

            var window = new ProjectExplorerWindow(this.serviceProvider);
            window.ShowWindow();
            Delay(TimeSpan.FromMilliseconds(100));

            // Check tree.
            var rootNode = GetRootNode(window);

            Assert.AreEqual(2, rootNode.Nodes.Cast<ProjectNode>().Count());

            var projectNode = rootNode.Nodes.Cast<ProjectNode>()
                .Where(n => n.Project.ProjectId == "forbidden-project")
                .FirstOrDefault();
            Assert.AreEqual(0, projectNode.Nodes.Count);
        }
    }
}
