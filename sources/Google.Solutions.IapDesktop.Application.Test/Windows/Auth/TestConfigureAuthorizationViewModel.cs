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

using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.IapDesktop.Application.Windows.Auth;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Auth
{
    [TestFixture]
    public class TestConfigureAuthorizationViewModel
    {
        private static readonly WorkforcePoolProviderLocator SampleProviderLocator
            = new WorkforcePoolProviderLocator("global", "pool-1", "provider-1");

        //---------------------------------------------------------------------
        // IsOkButtonEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenGaiaOptionChecked_ThenIsOkButtonEnabledReturnsTrue()
        {
            var viewModel = new ConfigureAuthorizationViewModel();

            viewModel.IsGaiaOptionChecked.Value = true;

            Assert.IsTrue(viewModel.IsOkButtonEnabled.Value);
        }

        [Test]
        public void WhenWorkforcePoolOptionChecked_ThenIsOkButtonEnabledReturnsFalse()
        {
            var viewModel = new ConfigureAuthorizationViewModel();

            viewModel.IsWorkforcePoolOptionChecked.Value = true;

            Assert.IsFalse(viewModel.IsOkButtonEnabled.Value);
        }

        [Test]
        public void WhenWorkforcePoolOptionCheckedAndDetailsProvided_ThenIsOkButtonEnabledReturnsTrue()
        {
            var viewModel = new ConfigureAuthorizationViewModel();

            viewModel.IsWorkforcePoolOptionChecked.Value = true;
            viewModel.WorkforcePoolLocationId.Value = "global";
            viewModel.WorkforcePoolId.Value = "pool-1";
            viewModel.WorkforcePoolProviderId.Value = "provider-1";

            Assert.IsTrue(viewModel.IsOkButtonEnabled.Value);
        }

        //---------------------------------------------------------------------
        // IsGaiaOptionChecked.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLocatorIsNull_ThenIsGaiaOptionCheckedReturnsTrue()
        {
            var viewModel = new ConfigureAuthorizationViewModel()
            {
                WorkforcePoolProvider = null
            };

            Assert.IsTrue(viewModel.IsGaiaOptionChecked.Value);
        }

        [Test]
        public void WhenLocatorSet_ThenIsGaiaOptionCheckedReturnsFalse()
        {
            var viewModel = new ConfigureAuthorizationViewModel()
            {
                WorkforcePoolProvider = SampleProviderLocator
            };

            Assert.IsFalse(viewModel.IsGaiaOptionChecked.Value);
        }

        //---------------------------------------------------------------------
        // IsWorkforcePoolOptionChecked.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLocatorIsNull_ThenIsWorkforcePoolOptionCheckedReturnsFalse()
        {
            var viewModel = new ConfigureAuthorizationViewModel()
            {
                WorkforcePoolProvider = null
            };

            Assert.IsFalse(viewModel.IsWorkforcePoolOptionChecked.Value);
        }

        [Test]
        public void WhenLocatorSet_ThenIsWorkforcePoolOptionCheckedReturnsTrue()
        {
            var viewModel = new ConfigureAuthorizationViewModel()
            {
                WorkforcePoolProvider = SampleProviderLocator
            };

            Assert.IsTrue(viewModel.IsWorkforcePoolOptionChecked.Value);

            Assert.AreEqual(SampleProviderLocator.Location, viewModel.WorkforcePoolLocationId.Value);
            Assert.AreEqual(SampleProviderLocator.Pool, viewModel.WorkforcePoolId.Value);
            Assert.AreEqual(SampleProviderLocator.Provider, viewModel.WorkforcePoolProviderId.Value);
        }
    }
}
