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
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ConnectionSettings;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Services.Workflows;
using Google.Solutions.IapDesktop.Application.Util;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Workflows
{
    [TestFixture]
    public class TestCredentialsService : FixtureBase
    {
        private readonly ServiceRegistry serviceRegistry = new ServiceRegistry();

        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private ICredentialsService CreateCredentialsService(Mock<ITaskDialog> taskDialogMock)
        {
            this.serviceRegistry.AddTransient<IJobService, SynchronousJobService>();
            this.serviceRegistry.AddMock<IConnectionSettingsWindow>();
            this.serviceRegistry.AddMock<IShowCredentialsDialog>();

            this.serviceRegistry.AddMock<IComputeEngineAdapter>()
                .Setup(a => a.ResetWindowsUserAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new System.Net.NetworkCredential("bob", "secret"));

            this.serviceRegistry.AddMock<IGenerateCredentialsDialog>()
                .Setup(d => d.PromptForUsername(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>()))
                .Returns("bob");

            var auth = new Mock<IAuthorization>();
            auth.SetupGet(a => a.Email).Returns("bob@gmail.com");
            
            var authAdapter = serviceRegistry.AddMock<IAuthorizationAdapter>();
            authAdapter.SetupGet(a => a.Authorization).Returns(auth.Object);

            this.serviceRegistry.AddSingleton<ITaskDialog>(taskDialogMock.Object);
            return new CredentialsService(serviceRegistry);
        }

        //---------------------------------------------------------------------
        // Behavior = Always.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNoCredentialsExistAndBehaviorSetToAlways_ThenGenerateOptionIsShown()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialService = CreateCredentialsService(taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Always;

            await credentialService.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("bob", settings.Username);
            Assert.AreEqual("secret", settings.Password.AsClearText());
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IList<string>>(options => options.Count == 3),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Once);
        }

        [Test]
        public async Task WhenCredentialsExistAndBehaviorSetToAlways_ThenGenerateOptionIsShown()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialService = CreateCredentialsService(taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Always;
            settings.Username = "alice";
            settings.CleartextPassword = "alicespassword";

            await credentialService.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("bob", settings.Username);
            Assert.AreEqual("secret", settings.Password.AsClearText());
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IList<string>>(options => options.Count == 2),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Once);
        }

        //---------------------------------------------------------------------
        // Behavior = Prompt.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNoCredentialsExistAndBehaviorSetToPrompt_ThenGenerateOptionIsShown()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialService = CreateCredentialsService(taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Prompt;

            await credentialService.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("bob", settings.Username);
            Assert.AreEqual("secret", settings.Password.AsClearText());
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IList<string>>(options => options.Count == 3),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Once);
        }

        [Test]
        public async Task WhenCredentialsExistAndBehaviorSetToPrompt_ThenDialogIsSkipped()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialService = CreateCredentialsService(taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Prompt;
            settings.Username = "alice";
            settings.CleartextPassword = "alicespassword";

            await credentialService.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("alice", settings.Username);
            Assert.AreEqual("alicespassword", settings.Password.AsClearText());
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Never);
        }

        //---------------------------------------------------------------------
        // Behavior = Disabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoCredentialsExistAndBehaviorSetToDisabledAndJumpToSettingsAllowed_ThenJumpToSettingsOptionIsShown()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialService = CreateCredentialsService(taskDialog);
            var window = this.serviceRegistry.AddMock<IConnectionSettingsWindow>();
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Disable;

            AssertEx.ThrowsAggregateException<TaskCanceledException>(
                () => credentialService.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true).Wait());

            window.Verify(w => w.ShowWindow(), Times.Once);
            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IList<string>>(options => options.Count == 2),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Once);
        }

        [Test]
        public async Task WhenNoCredentialsExistAndBehaviorSetToDisabledAndJumpToSettingsNotAllowed_ThenDialogIsSkipped()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(1);

            var credentialService = CreateCredentialsService(taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Disable;

            await credentialService.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                false);

            Assert.IsNull(settings.Username);
            Assert.IsNull(settings.Password);
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Never);
        }

        [Test]
        public async Task WhenCredentialsExistAndBehaviorSetToDisabled_ThenDialogIsSkipped()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialService = CreateCredentialsService(taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Prompt;
            settings.Username = "alice";
            settings.CleartextPassword = "alicespassword";

            await credentialService.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("alice", settings.Username);
            Assert.AreEqual("alicespassword", settings.Password.AsClearText());
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Never);
        }
    }
}
