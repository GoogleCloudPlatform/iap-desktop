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

using Google.Solutions.IapDesktop.Application.Windows.Authorization;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Authorization
{
    [TestFixture]
    public class TestNewProfileViewModel : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // IsProfileNameInvalid.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileNameContainsUnsupportedCharacters_ThenIsProfileNameInvalidIsTrue()
        {
            var viewModel = new NewProfileViewModel()
            {
                ProfileName = "Föö"
            };

            Assert.IsTrue(viewModel.IsProfileNameInvalid);
        }

        [Test]
        public void WhenProfileNameIsEmpty_ThenIsProfileNameInvalidIsFalse()
        {
            var viewModel = new NewProfileViewModel();

            Assert.IsFalse(viewModel.IsProfileNameInvalid);
        }

        [Test]
        public void WhenProfileNameIsValid_ThenIsProfileNameInvalidIsFalse()
        {
            var viewModel = new NewProfileViewModel()
            {
                ProfileName = "Valid name"
            };

            Assert.IsFalse(viewModel.IsProfileNameInvalid);
        }

        //---------------------------------------------------------------------
        // IsOkButtonEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileNameContainsUnsupportedCharacters_ThenIsOkButtonEnabledIsFalse()
        {
            var viewModel = new NewProfileViewModel()
            {
                ProfileName = "Föö"
            };

            Assert.IsFalse(viewModel.IsOkButtonEnabled);
        }

        [Test]
        public void WhenProfileNameIsEmpty_ThenIsOkButtonEnabledIsFalse()
        {
            var viewModel = new NewProfileViewModel();

            Assert.IsFalse(viewModel.IsOkButtonEnabled);
        }

        [Test]
        public void WhenProfileNameIsValid_ThenIsIsOkButtonEnabledIsTrue()
        {
            var viewModel = new NewProfileViewModel()
            {
                ProfileName = "Valid name"
            };

            Assert.IsTrue(viewModel.IsOkButtonEnabled);
        }
    }
}
