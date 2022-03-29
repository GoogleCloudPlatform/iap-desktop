//
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

        private static AuthorizedPublicKeysViewModel CreateViewModel()
        {
            var registry = new ServiceRegistry();
            registry.AddSingleton<IJobService>(new JobServiceMock());

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
            Assert.AreEqual("Installed packages", viewModel.WindowTitle);
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
            Assert.AreEqual("Installed packages", viewModel.WindowTitle);
            Assert.IsFalse(viewModel.AllKeys.Any());
            Assert.IsFalse(viewModel.FilteredKeys.Any());
        }

        [Test]
        public async Task WhenSwitchingToProjectNode_ThenListIsPopulated()
        {
            await Task.Yield();
            Assert.Inconclusive();
        }

        [Test]
        public async Task WhenSwitchingToInstanceNode_ThenListIsPopulated()
        {
            await Task.Yield();
            Assert.Inconclusive();
        }

        //---------------------------------------------------------------------
        // Filtering.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenLoaded_ThenFilteredPackagesContainsKeys()
        {
            await Task.Yield();
            Assert.Inconclusive();
        }

        [Test]
        public async Task WhenFilteSet_ThenFilteredPackagesContainsPackagesThatMatchTerm()
        {
            await Task.Yield();
            Assert.Inconclusive();
        }

        [Test]
        public async Task WhenFilterIsReset_ThenFilteredPackagesContainsAllPackages()
        {
            await Task.Yield();
            Assert.Inconclusive();
        }
    }
}
