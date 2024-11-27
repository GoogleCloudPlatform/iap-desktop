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

using Google.Solutions.Mvvm.Cache;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Mvvm.Test.Cache
{
    [TestFixture]
    public class TestModelCachingViewModelBase
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

            protected override Task<string?> LoadModelAsync(string key, CancellationToken token)
            {
                this.LoadModelCalls++;
                return Task.FromResult<string?>("test");
            }

            public Task Reload() => base.InvalidateAsync();
        }

        private class SlowViewModel : SampleViewModel
        {
            public int CancelCount = 0;
            protected override async Task<string?> LoadModelAsync(string key, CancellationToken token)
            {
                try
                {
                    await Task.Delay(100, token)
                        .ConfigureAwait(true);
                }
                catch (TaskCanceledException)
                {
                    this.CancelCount++;
                    throw;
                }

                return await base.LoadModelAsync(key, token)
                    .ConfigureAwait(true);
            }
        }

        //---------------------------------------------------------------------
        // SwitchToModel.
        //---------------------------------------------------------------------

        [Test]
        public async Task SwitchToModel_WhenSwitchToModelFirstTime_ThenPreviousSwitchIsCanceled()
        {
            var viewModel = new SlowViewModel();
            var t = viewModel.SwitchToModelAsync("one");
            await viewModel.SwitchToModelAsync("two")
                .ConfigureAwait(false);

            Assert.AreEqual(1, viewModel.LoadModelCalls);
            Assert.AreEqual(1, viewModel.ApplyCalls);
            Assert.AreEqual(1, viewModel.CancelCount);

            await t.ConfigureAwait(true);
        }

        [Test]
        public async Task SwitchToModel_WhenSwitchToModelFirstTime_ThenLoadModelAsyncAndApplyModelCalled()
        {
            var viewModel = new SampleViewModel();
            await viewModel.SwitchToModelAsync("one")
                .ConfigureAwait(false);

            Assert.AreEqual(1, viewModel.LoadModelCalls);
            Assert.AreEqual(1, viewModel.ApplyCalls);
        }

        [Test]
        public async Task SwitchToModel_WhenSwitchToModelSecondTime_ThenOnlyApplyModelCalled()
        {
            var viewModel = new SampleViewModel();
            await viewModel.SwitchToModelAsync("one")
                .ConfigureAwait(false);

            Assert.AreEqual(1, viewModel.LoadModelCalls);
            Assert.AreEqual(1, viewModel.ApplyCalls);

            await viewModel.SwitchToModelAsync("one")
                .ConfigureAwait(false);

            Assert.AreEqual(1, viewModel.LoadModelCalls);

            // ApplyModel is called twice.
            Assert.AreEqual(3, viewModel.ApplyCalls);
        }

        //---------------------------------------------------------------------
        // Invalidating.
        //---------------------------------------------------------------------

        [Test]
        public async Task Reload_WhenInvalidated_ThenLoadModelAsyncAndApplyModelCalled()
        {
            var viewModel = new SampleViewModel();
            await viewModel.SwitchToModelAsync("one")
                .ConfigureAwait(false);

            Assert.AreEqual(1, viewModel.LoadModelCalls);
            Assert.AreEqual(1, viewModel.ApplyCalls);

            await viewModel.Reload()
                .ConfigureAwait(false);

            Assert.AreEqual(2, viewModel.LoadModelCalls);

            // ApplyModel is called twice.
            Assert.AreEqual(3, viewModel.ApplyCalls);
        }
    }
}
