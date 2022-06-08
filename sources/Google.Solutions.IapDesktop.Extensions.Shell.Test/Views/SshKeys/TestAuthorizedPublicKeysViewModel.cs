﻿//
// Copyright 2022 Google LLC
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
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Application.Test.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshKeys;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.SshKeys
{
    [TestFixture]
    public class TestAuthorizedPublicKeysViewModel : ApplicationFixtureBase
    {
        private class JobServiceMock : IJobService
        {
            public Task<T> RunInBackground<T>(
                JobDescription jobDescription,
                Func<CancellationToken, Task<T>> jobFunc)
                => jobFunc(CancellationToken.None);
        }

        private static AuthorizedPublicKeysViewModel CreateViewModel(
            IConfirmationDialog confirmationDialog = null)
        {
            var registry = new ServiceRegistry();
            registry.AddSingleton<IJobService>(new JobServiceMock());
            registry.AddMock<IResourceManagerAdapter>();
            
            if (confirmationDialog != null)
            {
                registry.AddSingleton(confirmationDialog);
            }

            var gceAdapter = registry.AddMock<IComputeEngineAdapter>();
            gceAdapter.Setup(a => a.GetProjectAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Project()
                {
                    CommonInstanceMetadata = new Metadata()
                    {
                        Items = new []
                        {
                            new Metadata.ItemsData()
                            {
                                Key = MetadataAuthorizedPublicKeyProcessor.EnableOsLoginFlag,
                                Value = "true"
                            }
                        }
                    }
                });
            gceAdapter.Setup(a => a.GetInstanceAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Instance());


            var alicesKey = new Mock<IAuthorizedPublicKey>();
            alicesKey.SetupGet(k => k.Email).Returns("alice@gmail.com");
            alicesKey.SetupGet(k => k.KeyType).Returns("ssh-rsa");

            var bobsKey = new Mock<IAuthorizedPublicKey>();
            bobsKey.SetupGet(k => k.Email).Returns("bob@gmail.com");
            bobsKey.SetupGet(k => k.KeyType).Returns("ssh-rsa");

            var osLoginService = registry.AddMock<IOsLoginService>();
            osLoginService
                .Setup(s => s.ListAuthorizedKeysAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    alicesKey.Object, 
                    bobsKey.Object
                });

            return new AuthorizedPublicKeysViewModel(registry);
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingToCloudNode_ThenListIsDisabled()
        {
            var viewModel = CreateViewModel();

            var node = new Mock<IProjectModelCloudNode>();
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.AreEqual(CommandState.Unavailable, AuthorizedPublicKeysViewModel.GetCommandState(node.Object));
            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.AreEqual("Authorized SSH keys", viewModel.WindowTitle);
            Assert.IsFalse(viewModel.AllKeys.Any());
            Assert.IsFalse(viewModel.FilteredKeys.Any());
        }

        [Test]
        public async Task WhenSwitchingToZoneNode_ThenListIsDisabled()
        {
            var viewModel = CreateViewModel();

            var node = new Mock<IProjectModelZoneNode>();
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.AreEqual(CommandState.Unavailable, AuthorizedPublicKeysViewModel.GetCommandState(node.Object));
            Assert.IsFalse(viewModel.IsInformationBarVisible);
            Assert.AreEqual("Authorized SSH keys", viewModel.WindowTitle);
            Assert.IsFalse(viewModel.AllKeys.Any());
            Assert.IsFalse(viewModel.FilteredKeys.Any());
        }

        [Test]
        public async Task WhenSwitchingToProjectNode_ThenListIsPopulated()
        {
            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator("project-1"));
            node.SetupGet(n => n.DisplayName).Returns("project-1");

            var viewModel = CreateViewModel();

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.AreEqual(CommandState.Enabled, AuthorizedPublicKeysViewModel.GetCommandState(node.Object));
            Assert.IsTrue(viewModel.IsListEnabled);
            Assert.IsTrue(viewModel.IsInformationBarVisible);
            StringAssert.Contains("project-1", viewModel.WindowTitle);

            Assert.AreEqual(2, viewModel.AllKeys.Count);
            Assert.AreEqual(2, viewModel.FilteredKeys.Count);
        }

        [Test]
        public async Task WhenSwitchingToInstanceNode_ThenListIsPopulated()
        {
            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.OperatingSystem).Returns(OperatingSystems.Linux);
            node.SetupGet(n => n.DisplayName).Returns("instance-1");
            node.SetupGet(n => n.Instance).Returns(
                new InstanceLocator("project-1", "zone-1", "instance-1"));

            var viewModel = CreateViewModel();

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.AreEqual(CommandState.Enabled, AuthorizedPublicKeysViewModel.GetCommandState(node.Object));
            Assert.IsTrue(viewModel.IsListEnabled);
            Assert.IsTrue(viewModel.IsInformationBarVisible);
            StringAssert.Contains("instance-1", viewModel.WindowTitle);

            Assert.AreEqual(2, viewModel.AllKeys.Count);
            Assert.AreEqual(2, viewModel.FilteredKeys.Count);
        }

        //---------------------------------------------------------------------
        // Filtering.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFilterSet_ThenFilteredPackagesContainsPackagesThatMatchTerm()
        {
            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator("project-1"));
            node.SetupGet(n => n.DisplayName).Returns("project-1");

            var viewModel = CreateViewModel();

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            viewModel.Filter = "ice";
            Assert.AreEqual(2, viewModel.AllKeys.Count);
            Assert.AreEqual(1, viewModel.FilteredKeys.Count);

            viewModel.Filter = null;
            Assert.AreEqual(2, viewModel.FilteredKeys.Count);
        }

        //---------------------------------------------------------------------
        // SelectedItem.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingNodes_ThenSelectedItemisCleared()
        {
            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator("project-1"));
            node.SetupGet(n => n.DisplayName).Returns("project-1");

            var viewModel = CreateViewModel();

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            viewModel.SelectedItem = viewModel.AllKeys.FirstOrDefault();
            Assert.IsTrue(viewModel.IsDeleteButtonEnabled);

            // Switch again.
            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsFalse(viewModel.IsDeleteButtonEnabled);
        }

        //---------------------------------------------------------------------
        // IsDeleteButtonEnabled.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenItemSelected_ThenDeleteButtonIsEnabled()
        {
            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator("project-1"));
            node.SetupGet(n => n.DisplayName).Returns("project-1");

            var viewModel = CreateViewModel();

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            Assert.IsFalse(viewModel.IsDeleteButtonEnabled);

            viewModel.SelectedItem = viewModel.AllKeys.FirstOrDefault();
            Assert.IsTrue(viewModel.IsDeleteButtonEnabled);
        }

        //---------------------------------------------------------------------
        // DeleteSelectedItemAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConfirmationIsNo_ThenDeleteSelectedItemAsyncDoesNothing()
        {
            var confirmationMock = new Mock<IConfirmationDialog>();
            confirmationMock.Setup(d => d.Confirm(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(DialogResult.Cancel);

            var node = new Mock<IProjectModelProjectNode>();
            node.SetupGet(n => n.Project).Returns(new ProjectLocator("project-1"));
            node.SetupGet(n => n.DisplayName).Returns("project-1");

            var viewModel = CreateViewModel(confirmationMock.Object);
            viewModel.View = new Mock<IWin32Window>().Object;

            await viewModel
                .SwitchToModelAsync(node.Object)
                .ConfigureAwait(true);

            viewModel.SelectedItem = viewModel.AllKeys.FirstOrDefault();

            await viewModel.DeleteSelectedItemAsync(CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}
