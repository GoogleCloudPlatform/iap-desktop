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

using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Views.ConnectionSettings;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.Credentials;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Services.Credentials
{
    [TestFixture]
    public class TestCredentialsService : FixtureBase
    {
        private readonly ServiceRegistry serviceRegistry = new ServiceRegistry();

        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");


        [Test]
        public void WhenSuggestedUserNameProvided_ThenSuggestionIsUsed()
        {
            var serviceRegistry = new ServiceRegistry();
            var credDialog = serviceRegistry.AddMock<IGenerateCredentialsDialog>();
            credDialog
                .Setup(d => d.PromptForUsername(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>()))
                .Returns<string>(null); // Cancel dialog

            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);
            settings.Username = "alice";

            var credentialsService = new CredentialsService(serviceRegistry);
            AssertEx.ThrowsAggregateException<TaskCanceledException>(
                () => credentialsService.GenerateCredentialsAsync(
                    null,
                    SampleInstance,
                    settings).Wait());

            credDialog.Verify(d => d.PromptForUsername(
                It.IsAny<IWin32Window>(),
                It.Is<string>(u => u == "alice")), Times.Once);
        }

        [Test]
        public void WhenNoSuggestedUserNameProvided_ThenSuggestionIsDerivedFromSigninName()
        {
            var serviceRegistry = new ServiceRegistry();

            var auth = new Mock<IAuthorization>();
            auth.SetupGet(a => a.Email).Returns("bobsemail@gmail.com");

            serviceRegistry.AddMock<IAuthorizationAdapter>()
                .SetupGet(a => a.Authorization).Returns(auth.Object);

            var credDialog = serviceRegistry.AddMock<IGenerateCredentialsDialog>();
            credDialog
                .Setup(d => d.PromptForUsername(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>()))
                .Returns<string>(null); // Cancel dialog

            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            var credentialsService = new CredentialsService(serviceRegistry);
            AssertEx.ThrowsAggregateException<TaskCanceledException>(
                () => credentialsService.GenerateCredentialsAsync(
                    null,
                    SampleInstance,
                    settings).Wait());

            credDialog.Verify(d => d.PromptForUsername(
                It.IsAny<IWin32Window>(),
                It.Is<string>(u => u == "bobsemail")), Times.Once);
        }
    }
}
