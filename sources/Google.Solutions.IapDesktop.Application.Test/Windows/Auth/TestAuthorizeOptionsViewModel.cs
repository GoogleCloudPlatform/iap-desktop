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
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows.Auth;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Auth
{
    [TestFixture]
    public class TestAuthorizeOptionsViewModel
    {
        private static readonly WorkforcePoolProviderLocator SampleProviderLocator
            = new WorkforcePoolProviderLocator("global", "pool-1", "provider-1");


        private static Mock<IRepository<IAccessSettings>> CreateSettingsRepository(
            WorkforcePoolProviderLocator? provider)
        {
            var setting = new Mock<ISetting<string?>>();
            setting.SetupGet(s => s.Value).Returns(provider?.ToString());

            var settings = new Mock<IAccessSettings>();
            settings.SetupGet(s => s.WorkforcePoolProvider).Returns(setting.Object);

            var repository = new Mock<IRepository<IAccessSettings>>();
            repository.Setup(r => r.GetSettings()).Returns(settings.Object);

            return repository;
        }

        //---------------------------------------------------------------------
        // IsOkButtonEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenGaiaOptionChecked_ThenIsOkButtonEnabledReturnsTrue()
        {
            var repository = CreateSettingsRepository(null);
            var viewModel = new AuthorizeOptionsViewModel(repository.Object);

            viewModel.IsGaiaOptionChecked.Value = true;

            Assert.IsTrue(viewModel.IsOkButtonEnabled.Value);
        }

        [Test]
        public void WhenWorkforcePoolOptionChecked_ThenIsOkButtonEnabledReturnsFalse()
        {
            var repository = CreateSettingsRepository(null);
            var viewModel = new AuthorizeOptionsViewModel(repository.Object);

            viewModel.IsWorkforcePoolOptionChecked.Value = true;

            Assert.IsFalse(viewModel.IsOkButtonEnabled.Value);
        }

        [Test]
        public void WhenWorkforcePoolOptionCheckedAndDetailsProvided_ThenIsOkButtonEnabledReturnsTrue()
        {
            var repository = CreateSettingsRepository(null);
            var viewModel = new AuthorizeOptionsViewModel(repository.Object);

            viewModel.IsWorkforcePoolOptionChecked.Value = true;
            viewModel.WorkforcePoolLocationId.Value = "global";
            viewModel.WorkforcePoolId.Value = "pool-1";
            viewModel.WorkforcePoolProviderId.Value = "provider-1";

            Assert.IsTrue(viewModel.IsOkButtonEnabled.Value);
        }

        //---------------------------------------------------------------------
        // Radio buttons: IsGaiaOptionChecked, IsWorkforcePoolOptionChecked.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingsDoNotContainWorkforcePoolProvider_ThenRadioButtonsAreSet()
        {
            var repository = CreateSettingsRepository(null);
            var viewModel = new AuthorizeOptionsViewModel(repository.Object);

            Assert.IsTrue(viewModel.IsGaiaOptionChecked.Value);
            Assert.IsFalse(viewModel.IsWorkforcePoolOptionChecked.Value);
        }

        [Test]
        public void WhenSettingsContainWorkforcePoolProvider_ThenRadioButtonsAreSet()
        {
            var repository = CreateSettingsRepository(SampleProviderLocator);
            var viewModel = new AuthorizeOptionsViewModel(repository.Object)
            {
                WorkforcePoolProvider = SampleProviderLocator
            };

            Assert.IsFalse(viewModel.IsGaiaOptionChecked.Value);
            Assert.IsTrue(viewModel.IsWorkforcePoolOptionChecked.Value);
        }

        //---------------------------------------------------------------------
        // Text boxes.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingsDoNotContainWorkforcePoolProvider_ThenTextBoxesAreEmpty()
        {
            var repository = CreateSettingsRepository(null);
            var viewModel = new AuthorizeOptionsViewModel(repository.Object);

            Assert.IsFalse(viewModel.IsWorkforcePoolOptionChecked.Value);
            Assert.IsNull(viewModel.WorkforcePoolLocationId.Value);
            Assert.IsNull(viewModel.WorkforcePoolId.Value);
            Assert.IsNull(viewModel.WorkforcePoolProviderId.Value);
        }

        [Test]
        public void WhenLocatorSet_ThenIsWorkforcePoolOptionCheckedReturnsTrue()
        {
            var repository = CreateSettingsRepository(SampleProviderLocator);
            var viewModel = new AuthorizeOptionsViewModel(repository.Object);

            Assert.IsTrue(viewModel.IsWorkforcePoolOptionChecked.Value);

            Assert.AreEqual(SampleProviderLocator.Location, viewModel.WorkforcePoolLocationId.Value);
            Assert.AreEqual(SampleProviderLocator.Pool, viewModel.WorkforcePoolId.Value);
            Assert.AreEqual(SampleProviderLocator.Provider, viewModel.WorkforcePoolProviderId.Value);
        }
    }
}
