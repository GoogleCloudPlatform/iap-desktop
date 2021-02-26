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
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Test.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Rdp.Test.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.Credentials;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Views.Credentials
{
    [TestFixture]
    public class TestCredentialsService : CommonFixtureBase
    {
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

            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);
            settings.Username.Value = "alice";

            var credentialsService = new CredentialsService(serviceRegistry);
            AssertEx.ThrowsAggregateException<TaskCanceledException>(
                () => credentialsService.GenerateCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    false).Wait());

            credDialog.Verify(d => d.PromptForUsername(
                It.IsAny<IWin32Window>(),
                It.Is<string>(u => u == "alice")), Times.Once);
        }

        [Test]
        public async Task WhenSuggestedUserNameProvidedAndSilentIsTrue_ThenSuggestionIsUsedWithoutPrompting()
        {
            var serviceRegistry = new ServiceRegistry();

            var auth = new Mock<IAuthorization>();
            auth.SetupGet(a => a.Email).Returns("bobsemail@gmail.com");
            serviceRegistry.AddMock<IAuthorizationAdapter>()
                .SetupGet(a => a.Authorization).Returns(auth.Object);

            serviceRegistry.AddSingleton<IJobService, SynchronousJobService>();
            serviceRegistry.AddMock<IComputeEngineAdapter>()
                .Setup(a => a.ResetWindowsUserAsync(
                    It.IsAny<InstanceLocator>(),
                    It.Is<string>(user => user == "alice"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NetworkCredential("alice", "password"));

            var credDialog = serviceRegistry.AddMock<IGenerateCredentialsDialog>();
            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);
            settings.Username.Value = "alice";

            var credentialsService = new CredentialsService(serviceRegistry);
            await credentialsService.GenerateCredentialsAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("alice", settings.Username.Value);
            Assert.AreEqual("password", settings.Password.ClearTextValue);
            credDialog.Verify(d => d.PromptForUsername(
                It.IsAny<IWin32Window>(),
                It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task WhenNoSuggestedUserNameProvidedAndSilentIsTrue_ThenSuggestionIsDerivedFromSigninNameWithoutPrompting()
        {
            var serviceRegistry = new ServiceRegistry();

            var auth = new Mock<IAuthorization>();
            auth.SetupGet(a => a.Email).Returns("bobsemail@gmail.com");
            serviceRegistry.AddMock<IAuthorizationAdapter>()
                .SetupGet(a => a.Authorization).Returns(auth.Object);

            serviceRegistry.AddSingleton<IJobService, SynchronousJobService>();
            serviceRegistry.AddMock<IComputeEngineAdapter>()
                .Setup(a => a.ResetWindowsUserAsync(
                    It.IsAny<InstanceLocator>(),
                    It.Is<string>(user => user == "bobsemail"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NetworkCredential("bobsemail", "password"));

            var credDialog = serviceRegistry.AddMock<IGenerateCredentialsDialog>();
            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);

            var credentialsService = new CredentialsService(serviceRegistry);
            await credentialsService.GenerateCredentialsAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("bobsemail", settings.Username.Value);
            Assert.AreEqual("password", settings.Password.ClearTextValue);
            credDialog.Verify(d => d.PromptForUsername(
                It.IsAny<IWin32Window>(),
                It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void WhenSuggestedUserNameIsEmpty_ThenSuggestionIsDerivedFromSigninName()
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


            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);
            settings.Username.Value = "";

            var credentialsService = new CredentialsService(serviceRegistry);
            AssertEx.ThrowsAggregateException<TaskCanceledException>(
                () => credentialsService.GenerateCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    false).Wait());

            credDialog.Verify(d => d.PromptForUsername(
                It.IsAny<IWin32Window>(),
                It.Is<string>(u => u == "bobsemail")), Times.Once);
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


            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);

            var credentialsService = new CredentialsService(serviceRegistry);
            AssertEx.ThrowsAggregateException<TaskCanceledException>(
                () => credentialsService.GenerateCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    false).Wait());

            credDialog.Verify(d => d.PromptForUsername(
                It.IsAny<IWin32Window>(),
                It.Is<string>(u => u == "bobsemail")), Times.Once);
        }
    }
}
