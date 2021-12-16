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

using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.SshTerminal
{
    [TestFixture]
    public class TestSshAuthenticationPromptViewModel : ApplicationFixtureBase
    {
        [Test]
        public void WhenTitleSet_ThenNotificationIsRaised()
        {
            var viewModel = new SshAuthenticationPromptViewModel();
            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.Title = "test",
                v => v.Title);
        }

        [Test]
        public void WhenDescriptionSet_ThenNotificationIsRaised()
        {
            var viewModel = new SshAuthenticationPromptViewModel();
            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.Description = "test",
                v => v.Description);
        }

        [Test]
        public void WhenDescriptionSetToMultipleSentences_ThenLineBreaksAreAdded()
        {
            var viewModel = new SshAuthenticationPromptViewModel();
            viewModel.Description = "first. second. third.";

            Assert.AreEqual("first.\nsecond.\nthird.", viewModel.Description);
        }

        [Test]
        public void WhenInputSet_ThenNotificationIsRaised()
        {
            var viewModel = new SshAuthenticationPromptViewModel();
            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.Input = "test",
                v => v.Input);

            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.Input = "test",
                v => v.IsOkButtonEnabled);
        }

        [Test]
        public void WhenIsPasswordMaskedSet_ThenNotificationIsRaised()
        {
            var viewModel = new SshAuthenticationPromptViewModel();
            PropertyAssert.RaisesPropertyChangedNotification(
                viewModel,
                () => viewModel.IsPasswordMasked = true,
                v => v.IsPasswordMasked);
        }
    }
}
