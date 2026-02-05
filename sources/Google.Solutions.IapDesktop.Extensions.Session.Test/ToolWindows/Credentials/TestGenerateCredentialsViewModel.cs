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
        public void IsUsernameReserved_WhenUsernameIsEmpty()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = string.Empty
            };

            Assert.That(viewModel.IsUsernameReserved, Is.False);
        }

        [Test]
        public void IsUsernameReserved_WhenUsernameIsNotReserved()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = "i am bob"
            };

            Assert.That(viewModel.IsUsernameReserved, Is.False);
        }

        [Test]
        public void IsUsernameReserved_WhenUsernameIsReserved()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = "GUEST"
            };

            Assert.That(viewModel.IsUsernameReserved, Is.True);
        }

        //---------------------------------------------------------------------
        // IsOkButtonEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsOkButtonEnabled_WhenUsernameIsEmpty()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = string.Empty
            };

            Assert.That(viewModel.IsOkButtonEnabled, Is.False);
        }

        [Test]
        public void IsOkButtonEnabled_WhenUsernameIsNotReserved()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = "i am bob"
            };

            Assert.That(viewModel.IsOkButtonEnabled, Is.True);
        }

        [Test]
        public void IsOkButtonEnabled_WhenUsernameIsReserved()
        {
            var viewModel = new NewCredentialsViewModel()
            {
                Username = "GUEST"
            };

            Assert.That(viewModel.IsOkButtonEnabled, Is.False);
        }
    }
}
