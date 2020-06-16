//
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

using Google.Apis.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.ObjectModel
{
    [TestFixture]
    public class TestModelCachingViewModelBase : FixtureBase
    {
        private class SampleViewModel : ModelCachingViewModelBase<string, string>
        {
            public int ApplyCalls = 0;
            public int LoadModelCalls = 0;

            public SampleViewModel() : base(2)
            {
            }

            protected override void ApplyModel(bool cached)
            {
                this.ApplyCalls++;
            }

            protected override Task<string> LoadModelAsync(string key, CancellationToken token)
            {
                this.LoadModelCalls++;
                return Task.FromResult("test");
            }

            public Task Reload() => base.InvalidateAsync();
        }

        private class SlowViewModel : SampleViewModel
        {
            public int CancelCount = 0;
            protected override async Task<string> LoadModelAsync(string key, CancellationToken token)
            {
                try
                {
                    await Task.Delay(100, token);
                }
                catch (TaskCanceledException)
                {
                    this.CancelCount++;
                    throw;
                }

                return await base.LoadModelAsync(key, token);
            }
        }

        [Test]
        public async Task WhenSwitchToModelFirstTime_ThenPreviousSwitchIsCanceled()
        {
            var viewModel = new SlowViewModel();
            var t = viewModel.SwitchToModelAsync("one");
            await viewModel.SwitchToModelAsync("two");

            Assert.AreEqual(1, viewModel.LoadModelCalls);
            Assert.AreEqual(1, viewModel.ApplyCalls);
            Assert.AreEqual(1, viewModel.CancelCount);

            await t;
        }

        [Test]
        public async Task WhenSwitchToModelFirstTime_ThenLoadModelAsyncAndApplyModelCalled()
        {
            var viewModel = new SampleViewModel();
            await viewModel.SwitchToModelAsync("one");

            Assert.AreEqual(1, viewModel.LoadModelCalls);
            Assert.AreEqual(1, viewModel.ApplyCalls);
        }

        [Test]
        public async Task WhenSwitchToModelSecondTime_ThenOnlyApplyModelCalled()
        {
            var viewModel = new SampleViewModel();
            await viewModel.SwitchToModelAsync("one");

            Assert.AreEqual(1, viewModel.LoadModelCalls);
            Assert.AreEqual(1, viewModel.ApplyCalls);

            await viewModel.SwitchToModelAsync("one");

            Assert.AreEqual(1, viewModel.LoadModelCalls);
            Assert.AreEqual(2, viewModel.ApplyCalls);
        }

        [Test]
        public async Task WhenInvalidated_ThenLoadModelAsyncAndApplyModelCalled()
        {
            var viewModel = new SampleViewModel();
            await viewModel.SwitchToModelAsync("one");

            Assert.AreEqual(1, viewModel.LoadModelCalls);
            Assert.AreEqual(1, viewModel.ApplyCalls);

            await viewModel.Reload();

            Assert.AreEqual(2, viewModel.LoadModelCalls);
            Assert.AreEqual(2, viewModel.ApplyCalls);
        }
    }
}
