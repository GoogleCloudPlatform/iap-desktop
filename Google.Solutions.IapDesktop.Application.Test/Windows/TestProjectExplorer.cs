using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.SettingsEditor;
using Google.Solutions.IapDesktop.Application.Windows;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Linq;
using System.ComponentModel;
using System.Windows.Forms;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.ProjectExplorer;
using Moq;
using Google.Solutions.IapDesktop.Application.Adapters;
using System.Threading.Tasks;
using Google.Apis.Compute.v1.Data;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    public class TestProjectExplorer : WindowTestFixtureBase
    {
        private static TreeNode GetRootNode(ProjectExplorerWindow window)
        {
            var tree = window.GetChild<TreeView>("treeView");
            return tree.Nodes.Cast<TreeNode>().First();
        }

        [Test]
        public void WhenNoProjectsLoaded_ThenRootNodeHasNoChildren()
        {
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            this.serviceRegistry.AddSingleton<IComputeEngineAdapter>(computeEngineAdapter.Object);

            // Open window.
            var window = new ProjectExplorerWindow(this.serviceProvider);
            window.ShowWindow();
            PumpWindowMessages();

            // Check tree.
            var rootNode = GetRootNode(window);
            Assert.IsInstanceOf(typeof(CloudNode), rootNode);
            Assert.AreEqual(0, rootNode.Nodes.Count);

            Assert.IsNull(this.exceptionDialog.ExceptionShown);
        }

        [Test]
        public void WhenProjectAdded_ThenWindowsInstancesAreListed()
        {
            // Add a project.
            this.serviceProvider.GetService<ProjectInventoryService>().AddProjectAsync("project-1").Wait();

            // Add some instances.
            var instances = new[]
            {
                new Instance()
                {
                    Id = 1,
                    Name = "instance-1a",
                    Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/antarctica1-a",
                    Disks = new [] {
                        new AttachedDisk()
                        {
                            GuestOsFeatures = new []
                            {
                                new GuestOsFeature()
                                {
                                    Type = "WINDOWS"
                                }
                            }
                        }
                    }
                },
                new Instance()
                {
                    Id = 1,
                    Name = "instance-1b",
                    Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/antarctica1-b",
                    Disks = new [] {
                        new AttachedDisk()
                        {
                            GuestOsFeatures = new []
                            {
                                new GuestOsFeature()
                                {
                                    Type = "WINDOWS"
                                }
                            }
                        }
                    }
                }
            };

            // Open window.
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            this.serviceRegistry.AddSingleton<IComputeEngineAdapter>(computeEngineAdapter.Object);
            computeEngineAdapter
                .Setup(o => o.QueryInstancesAsync("project-1"))
                .Returns(Task.FromResult<IEnumerable<Instance>>(instances));

            var window = new ProjectExplorerWindow(this.serviceProvider);
            window.ShowWindow();
            PumpWindowMessages();

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

            Assert.IsNull(this.exceptionDialog.ExceptionShown);
        }

        [Test]
        public void WhenProjectAdded_ThenLinuxInstancesAreIgnored()
        {
            // Add a project.
            this.serviceProvider.GetService<ProjectInventoryService>().AddProjectAsync("project-1").Wait();

            // Add some instances.
            var instances = new[]
            {
                new Instance()
                {
                    Id = 1,
                    Name = "windows",
                    Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/antarctica1-a",
                    Disks = new [] {
                        new AttachedDisk()
                        {
                            GuestOsFeatures = new []
                            {
                                new GuestOsFeature()
                                {
                                    Type = "WINDOWS"
                                }
                            }
                        }
                    }
                },
                new Instance()
                {
                    Id = 1,
                    Name = "linus",
                    Zone = "https://www.googleapis.com/compute/v1/projects/project-1/zones/antarctica1-a",
                    Disks = new [] {
                        new AttachedDisk()
                        {
                            GuestOsFeatures = new []
                            {
                                new GuestOsFeature()
                                {
                                    Type = "WHATEVER"
                                }
                            }
                        }
                    }
                }
            };

            // Open window.
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            this.serviceRegistry.AddSingleton<IComputeEngineAdapter>(computeEngineAdapter.Object);
            computeEngineAdapter
                .Setup(o => o.QueryInstancesAsync("project-1"))
                .Returns(Task.FromResult<IEnumerable<Instance>>(instances));

            var window = new ProjectExplorerWindow(this.serviceProvider);
            window.ShowWindow();
            PumpWindowMessages();

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
            Assert.AreEqual("instance-1a", vmNode.Text);
            Assert.AreEqual(0, vmNode.Nodes.Count);
            Assert.AreEqual("windows", vmNode.Text);

            Assert.IsNull(this.exceptionDialog.ExceptionShown);
        }
    }
}
