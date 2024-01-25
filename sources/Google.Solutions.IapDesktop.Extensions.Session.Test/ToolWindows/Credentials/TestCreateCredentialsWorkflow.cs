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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.Mocks;
using Google.Solutions.Testing.Application.ObjectModel;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Credentials
{
    [TestFixture]
    public class TestCreateCredentialsWorkflow
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private Mock<IAuthorization> CreateAuthorizationMock(string username)
        {
            var session = new Mock<IOidcSession>();
            session
                .SetupGet(a => a.Username)
                .Returns(username);

            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Session)
                .Returns(session.Object);

            return authorization;
        }

        //---------------------------------------------------------------------
        // Non-silent.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSuggestedUserNameProvidedAndDialogCancelled_ThenSuggestionIsUsed()
        {
            var serviceRegistry = new ServiceRegistry();
            var credDialog = serviceRegistry.AddMock<INewCredentialsDialog>();
            credDialog
                .Setup(d => d.ShowDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>()))
                .Returns(new GenerateCredentialsDialogResult(
                    DialogResult.Cancel,
                    null)); // Cancel dialog

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);
            settings.RdpUsername.Value = "alice";

            var credentialsService = new CreateCredentialsWorkflow(serviceRegistry);
            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => credentialsService.CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    false).Wait());

            credDialog.Verify(d => d.ShowDialog(
                It.IsAny<IWin32Window>(),
                It.Is<string>(u => u == "alice")), Times.Once);
        }

        [Test]
        public void WhenNoSuggestedUserNameProvidedAndDialogCancelled_ThenSuggestionIsDerivedFromSigninName()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddSingleton(
                CreateAuthorizationMock("bobsemail@gmail.com").Object);

            var credDialog = serviceRegistry.AddMock<INewCredentialsDialog>();
            credDialog
                .Setup(d => d.ShowDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>()))
                .Returns(new GenerateCredentialsDialogResult(
                    DialogResult.Cancel,
                    null)); // Cancel dialog

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);

            var credentialsService = new CreateCredentialsWorkflow(serviceRegistry);
            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => credentialsService.CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    false).Wait());

            credDialog.Verify(d => d.ShowDialog(
                It.IsAny<IWin32Window>(),
                It.Is<string>(u => u == "bobsemail")), Times.Once);
        }

        [Test]
        public void WhenSuggestedUserNameIsEmptyAndDialogCancelled_ThenSuggestionIsDerivedFromSigninName()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddSingleton(
                CreateAuthorizationMock("bobsemail@gmail.com").Object);

            var credDialog = serviceRegistry.AddMock<INewCredentialsDialog>();
            credDialog
                .Setup(d => d.ShowDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>()))
                .Returns(new GenerateCredentialsDialogResult(
                    DialogResult.Cancel,
                    null)); // Cancel dialog

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);
            settings.RdpUsername.Value = "";

            var credentialsService = new CreateCredentialsWorkflow(serviceRegistry);
            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => credentialsService.CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    false).Wait());

            credDialog.Verify(d => d.ShowDialog(
                It.IsAny<IWin32Window>(),
                It.Is<string>(u => u == "bobsemail")), Times.Once);
        }

        [Test]
        public async Task WhenPromptSucceeds_ThenCredentialIsGeneratedAndShown()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddSingleton(
                CreateAuthorizationMock("bobsemail@gmail.com").Object);

            serviceRegistry.AddSingleton<IJobService, SynchronousJobService>();
            serviceRegistry.AddMock<IWindowsCredentialGenerator>()
                .Setup(a => a.CreateWindowsCredentialsAsync(
                    It.IsAny<InstanceLocator>(),
                    It.Is<string>(user => user == "bob-admin"),
                    It.Is<UserFlags>(t => t == UserFlags.AddToAdministrators),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NetworkCredential("bob-admin", "password"));

            var showCredDialog = serviceRegistry.AddMock<IShowCredentialsDialog>();
            var credDialog = serviceRegistry.AddMock<INewCredentialsDialog>();
            credDialog
                .Setup(d => d.ShowDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>()))
                .Returns(new GenerateCredentialsDialogResult(
                    DialogResult.OK,
                    "bob-admin"));

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);
            settings.RdpUsername.Value = "";

            var credentialsService = new CreateCredentialsWorkflow(serviceRegistry);
            await credentialsService.CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    false)
                .ConfigureAwait(false);

            credDialog.Verify(d => d.ShowDialog(
                It.IsAny<IWin32Window>(),
                It.Is<string>(u => u == "bobsemail")), Times.Once);
            showCredDialog.Verify(d => d.ShowDialog(
                It.IsAny<IWin32Window>(),
                It.Is<string>(u => u == "bob-admin"),
                "password"), Times.Once);
        }

        //---------------------------------------------------------------------
        // Silent.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSuggestedUserNameProvidedAndSilentIsTrue_ThenSuggestionIsUsedWithoutPrompting()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddSingleton(
                CreateAuthorizationMock("bobsemail@gmail.com").Object);

            serviceRegistry.AddSingleton<IJobService, SynchronousJobService>();
            serviceRegistry.AddMock<IWindowsCredentialGenerator>()
                .Setup(a => a.CreateWindowsCredentialsAsync(
                    It.IsAny<InstanceLocator>(),
                    It.Is<string>(user => user == "alice"),
                    It.Is<UserFlags>(t => t == UserFlags.AddToAdministrators),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NetworkCredential("alice", "password"));

            var credDialog = serviceRegistry.AddMock<INewCredentialsDialog>();
            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);
            settings.RdpUsername.Value = "alice";

            var credentialsService = new CreateCredentialsWorkflow(serviceRegistry);
            await credentialsService.CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    true)
                .ConfigureAwait(false);

            Assert.AreEqual("alice", settings.RdpUsername.Value);
            Assert.AreEqual("password", settings.RdpPassword.ClearTextValue);
            credDialog.Verify(d => d.ShowDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task WhenNoSuggestedUserNameProvidedAndSilentIsTrue_ThenSuggestionIsDerivedFromSigninNameWithoutPrompting()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddSingleton(
                CreateAuthorizationMock("bobsemail@gmail.com").Object);

            serviceRegistry.AddSingleton<IJobService, SynchronousJobService>();
            serviceRegistry.AddMock<IWindowsCredentialGenerator>()
                .Setup(a => a.CreateWindowsCredentialsAsync(
                    It.IsAny<InstanceLocator>(),
                    It.Is<string>(user => user == "bobsemail"),
                    It.Is<UserFlags>(t => t == UserFlags.AddToAdministrators),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NetworkCredential("bobsemail", "password"));

            var credDialog = serviceRegistry.AddMock<INewCredentialsDialog>();
            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);

            var credentialsService = new CreateCredentialsWorkflow(serviceRegistry);
            await credentialsService.CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    true)
                .ConfigureAwait(false);

            Assert.AreEqual("bobsemail", settings.RdpUsername.Value);
            Assert.AreEqual("password", settings.RdpPassword.ClearTextValue);
            credDialog.Verify(d => d.ShowDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<string>()), Times.Never);
        }
    }
}
