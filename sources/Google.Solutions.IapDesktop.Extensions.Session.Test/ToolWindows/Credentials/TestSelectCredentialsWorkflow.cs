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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using Google.Solutions.Settings;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.ObjectModel;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Credentials
{
    [TestFixture]
    public class TestSelectCredentialsWorkflow
    {
        private readonly ServiceRegistry serviceRegistry = new ServiceRegistry();

        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private ISelectCredentialsDialog CreateCredentialsWorkflow(
            bool isGrantedPermissionToGenerateCredentials,
            bool expectSilentCredentialGeneration,
            Mock<ILegacyTaskDialog> taskDialogMock)
        {
            this.serviceRegistry.AddSingleton<ILegacyTaskDialog>(taskDialogMock.Object);
            this.serviceRegistry.AddMock<IConfigureCredentialsWorkflow>();
            //this.serviceRegistry.AddMock<IShowCredentialsDialog>();

            var credentialsService = this.serviceRegistry.AddMock<ICreateCredentialsWorkflow>();
            credentialsService
                .Setup(s => s.CreateCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Extensions.Session.Settings.ConnectionSettings>(),
                    It.Is<bool>(silent => silent == expectSilentCredentialGeneration)))
                .Callback(
                    (IWin32Window owner,
                    InstanceLocator instanceRef,
                    Extensions.Session.Settings.ConnectionSettings settings,
                    bool silent) =>
                    {
                        settings.RdpUsername.Value = "bob";
                        settings.RdpPassword.SetClearTextValue("secret");
                    });
            credentialsService
                .Setup(s => s.IsGrantedPermissionToGenerateCredentials(
                    It.IsAny<InstanceLocator>()))
                .ReturnsAsync(isGrantedPermissionToGenerateCredentials);

            return new SelectCredentialsDialog(this.serviceRegistry);
        }

        //---------------------------------------------------------------------
        // Behavior = Allow.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNoCredentialsFoundAndBehaviorSetToAllow_ThenGenerateOptionIsShown(
            [Values(true, false)] bool isGrantedPermissionToGenerateCredentials)
        {
            var taskDialog = new Mock<ILegacyTaskDialog>();
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

            var credentialPrompt = CreateCredentialsWorkflow(
                isGrantedPermissionToGenerateCredentials,
                false,
                taskDialog);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    RdpCredentialGenerationBehavior.Allow,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("bob", settings.RdpUsername.Value);
            Assert.AreEqual("secret", settings.RdpPassword.GetClearTextValue());
            Assert.IsNull(settings.RdpDomain.Value);

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
        public async Task WhenCredentialsFoundAndBehaviorSetToAllow_ThenGenerateOptionIsShown(
            [Values(true, false)] bool isGrantedPermissionToGenerateCredentials)
        {
            var taskDialog = new Mock<ILegacyTaskDialog>();
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

            var credentialPrompt = CreateCredentialsWorkflow(
                isGrantedPermissionToGenerateCredentials,
                false,
                taskDialog);
            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);
            settings.RdpUsername.Value = "alice";
            settings.RdpPassword.SetClearTextValue("alicespassword");

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                     RdpCredentialGenerationBehavior.Allow,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("bob", settings.RdpUsername.Value);
            Assert.AreEqual("secret", settings.RdpPassword.GetClearTextValue());
            Assert.IsNull(settings.RdpDomain.Value);

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
        // Behavior = AllowIfNoCredentialsFound.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNoCredentialsFoundAndPermissionGrantedAndBehaviorSetToAllowIfNoCredentialsFound_ThenGenerateOptionIsShown()
        {
            var taskDialog = new Mock<ILegacyTaskDialog>();
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

            var credentialPrompt = CreateCredentialsWorkflow(true, false, taskDialog);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("bob", settings.RdpUsername.Value);
            Assert.AreEqual("secret", settings.RdpPassword.GetClearTextValue());
            Assert.IsNull(settings.RdpDomain.Value);

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
        public void WhenNoCredentialsFoundAndPermissionNotGrantedAndBehaviorSetToAllowIfNoCredentialsFound_ThenJumpToSettingsOptionIsShown()
        {
            var taskDialog = new Mock<ILegacyTaskDialog>();
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

            var credentialPrompt = CreateCredentialsWorkflow(false, false, taskDialog);
            var window = this.serviceRegistry.AddMock<IConfigureCredentialsWorkflow>();

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => credentialPrompt.SelectCredentialsAsync(
                null,
                SampleInstance,
                settings,
                RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound,
                true).Wait());

            window.Verify(w => w.ShowCredentialsDialog(), Times.Once);
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
        public async Task WhenCredentialsFoundAndBehaviorSetToAllowIfNoCredentialsFound_ThenDialogIsSkipped(
            [Values(true, false)] bool isGrantedPermissionToGenerateCredentials)
        {
            var taskDialog = new Mock<ILegacyTaskDialog>();
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

            var credentialPrompt = CreateCredentialsWorkflow(
                isGrantedPermissionToGenerateCredentials,
                false,
                taskDialog);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);
            settings.RdpUsername.Value = "alice";
            settings.RdpPassword.SetClearTextValue("alicespassword");

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("alice", settings.RdpUsername.Value);
            Assert.AreEqual("alicespassword", settings.RdpPassword.GetClearTextValue());
            Assert.IsNull(settings.RdpDomain.Value);

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
        // Behavior = Disallow.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoCredentialsFoundAndBehaviorSetToDisallowAndJumpToSettingsAllowed_ThenJumpToSettingsOptionIsShown()
        {
            var taskDialog = new Mock<ILegacyTaskDialog>();
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

            var credentialPrompt = CreateCredentialsWorkflow(true, false, taskDialog);
            var window = this.serviceRegistry.AddMock<IConfigureCredentialsWorkflow>();

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => credentialPrompt.SelectCredentialsAsync(
                null,
                SampleInstance,
                settings,
                RdpCredentialGenerationBehavior.Disallow,
                true).Wait());

            window.Verify(w => w.ShowCredentialsDialog(), Times.Once);
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
        public async Task WhenNoCredentialsFoundAndBehaviorSetToDisallowAndJumpToSettingsNotAllowed_ThenDialogIsSkipped()
        {
            var taskDialog = new Mock<ILegacyTaskDialog>();
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

            var credentialPrompt = CreateCredentialsWorkflow(true, false, taskDialog);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    RdpCredentialGenerationBehavior.Disallow,
                    false)
                .ConfigureAwait(true);

            Assert.IsNull(settings.RdpUsername.Value);
            Assert.IsNull(settings.RdpPassword.Value);
            Assert.IsNull(settings.RdpDomain.Value);

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
        public async Task WhenCredentialsFoundAndBehaviorSetToDisallow_ThenDialogIsSkipped()
        {
            var taskDialog = new Mock<ILegacyTaskDialog>();
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

            var credentialPrompt = CreateCredentialsWorkflow(true, false, taskDialog);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);
            settings.RdpUsername.Value = "alice";
            settings.RdpPassword.SetClearTextValue("alicespassword");

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    RdpCredentialGenerationBehavior.Disallow,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("alice", settings.RdpUsername.Value);
            Assert.AreEqual("alicespassword", settings.RdpPassword.GetClearTextValue());
            Assert.IsNull(settings.RdpDomain.Value);

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
        // Behavior = Force.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenBehaviorSetToForceAndPermissionsGranted_ThenDialogIsSkippedAndCredentialsAreGenerated()
        {
            var taskDialog = new Mock<ILegacyTaskDialog>();

            var credentialPrompt = CreateCredentialsWorkflow(true, true, taskDialog);

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    RdpCredentialGenerationBehavior.Force,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("bob", settings.RdpUsername.Value);
            Assert.AreEqual("secret", settings.RdpPassword.GetClearTextValue());
            Assert.IsNull(settings.RdpDomain.Value);

            // No dialog shown.
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
        public void WhenBehaviorSetToForceAndPermissionsNotGranted_ThenJumpToSettingsOptionIsShown()
        {
            var taskDialog = new Mock<ILegacyTaskDialog>();
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

            var credentialPrompt = CreateCredentialsWorkflow(false, false, taskDialog);
            var window = this.serviceRegistry.AddMock<IConfigureCredentialsWorkflow>();

            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => credentialPrompt.SelectCredentialsAsync(
                null,
                SampleInstance,
                settings,
                RdpCredentialGenerationBehavior.Force,
                true).Wait());

            window.Verify(w => w.ShowCredentialsDialog(), Times.Once);
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
    }
}
