//
// Copyright 2021 Google LLC
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

using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Credentials
{
    [TestFixture]
    public class TestGenerateCredentialsViewModel
    {
        //---------------------------------------------------------------------
        // IsUsernameReserved.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUsernameIsEmpty_ThenIsUsernameReservedReturnsFalse()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = string.Empty
            };

            Assert.IsFalse(viewModel.IsUsernameReserved);
        }

        [Test]
        public void WhenUsernameIsNotReserved_ThenIsUsernameReservedReturnsFalse()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = "i am bob"
            };

            Assert.IsFalse(viewModel.IsUsernameReserved);
        }

        [Test]
        public void WhenUsernameIsReserved_ThenIsUsernameReservedReturnsTrue()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = "GUEST"
            };

            Assert.IsTrue(viewModel.IsUsernameReserved);
        }

        //---------------------------------------------------------------------
        // IsOkButtonEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUsernameIsEmpty_ThenIsOkButtonEnabledReturnsFalse()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = string.Empty
            };

            Assert.IsFalse(viewModel.IsOkButtonEnabled);
        }

        [Test]
        public void WhenUsernameIsNotReserved_ThenIsOkButtonEnabledReturnsTrue()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = "i am bob"
            };

            Assert.IsTrue(viewModel.IsOkButtonEnabled);
        }

        [Test]
        public void WhenUsernameIsReserved_ThenIsOkButtonEnabledReturnsFalse()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = "GUEST"
            };

            Assert.IsFalse(viewModel.IsOkButtonEnabled);
        }
    }
}
