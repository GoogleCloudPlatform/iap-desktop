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

using Google.Solutions.IapDesktop.Application.Windows.Auth;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Auth
{
    [TestFixture]
    public class TestNewProfileViewModel : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // IsProfileNameInvalid.
        //---------------------------------------------------------------------

        [Test]
        public void IsProfileNameInvalid_WhenProfileNameContainsUnsupportedCharacters()
        {
            var viewModel = new NewProfileViewModel()
            {
                ProfileName = "Föö"
            };

            Assert.IsTrue(viewModel.IsProfileNameInvalid);
        }

        [Test]
        public void IsProfileNameInvalid_WhenProfileNameIsEmpty()
        {
            var viewModel = new NewProfileViewModel();

            Assert.IsFalse(viewModel.IsProfileNameInvalid);
        }

        [Test]
        public void IsProfileNameInvalid_WhenProfileNameIsValid()
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
        public void IsOkButtonEnabled_WhenProfileNameContainsUnsupportedCharacters()
        {
            var viewModel = new NewProfileViewModel()
            {
                ProfileName = "Föö"
            };

            Assert.IsFalse(viewModel.IsOkButtonEnabled);
        }

        [Test]
        public void IsOkButtonEnabled_WhenProfileNameIsEmpty()
        {
            var viewModel = new NewProfileViewModel();

            Assert.IsFalse(viewModel.IsOkButtonEnabled);
        }

        [Test]
        public void IsOkButtonEnabled_WhenProfileNameIsValid()
        {
            var viewModel = new NewProfileViewModel()
            {
                ProfileName = "Valid name"
            };

            Assert.IsTrue(viewModel.IsOkButtonEnabled);
        }
    }
}
