﻿//
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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Credentials
{
    [TestFixture]
    public class TestSelectCredentialsWorkflow : ShellFixtureBase
    {
        private readonly ServiceRegistry serviceRegistry = new ServiceRegistry();

        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private ISelectCredentialsDialog CreateCredentialsWorkflow(
            bool isGrantedPermissionToGenerateCredentials,
            bool expectSilentCredentialGeneration,
            Mock<ITaskDialog> taskDialogMock)
        {
            this.serviceRegistry.AddSingleton<ITaskDialog>(taskDialogMock.Object);
            this.serviceRegistry.AddMock<IConfigureCredentialsWorkflow>();
            this.serviceRegistry.AddMock<IShowCredentialsDialog>();

            var credentialsService = this.serviceRegistry.AddMock<ICreateCredentialsWorkflow>();
            credentialsService
                .Setup(s => s.CreateCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ConnectionSettingsBase>(),
                    It.Is<bool>(silent => silent == expectSilentCredentialGeneration)))
                .Callback(
                    (IWin32Window owner,
                    InstanceLocator instanceRef,
                    ConnectionSettingsBase settings,
                    bool silent) =>
                    {
                        settings.RdpUsername.Value = "bob";
                        settings.RdpPassword.ClearTextValue = "secret";
                    });
            credentialsService
                .Setup(s => s.IsGrantedPermissionToGenerateCredentials(
                    It.IsAny<InstanceLocator>()))
                .ReturnsAsync(isGrantedPermissionToGenerateCredentials);

            return new SelectCredentialsDialog(serviceRegistry);
        }

        //---------------------------------------------------------------------
        // Behavior = Allow.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNoCredentialsFoundAndBehaviorSetToAllow_ThenGenerateOptionIsShown(
            [Values(true, false)] bool isGrantedPermissionToGenerateCredentials)
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

            var credentialPrompt = CreateCredentialsWorkflow(
                isGrantedPermissionToGenerateCredentials,
                false,
                taskDialog);

            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    RdpCredentialGenerationBehavior.Allow,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("bob", settings.RdpUsername.Value);
            Assert.AreEqual("secret", settings.RdpPassword.ClearTextValue);
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

            var credentialPrompt = CreateCredentialsWorkflow(
                isGrantedPermissionToGenerateCredentials,
                false,
                taskDialog);
            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);
            settings.RdpUsername.StringValue = "alice";
            settings.RdpPassword.ClearTextValue = "alicespassword";

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                     RdpCredentialGenerationBehavior.Allow,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("bob", settings.RdpUsername.Value);
            Assert.AreEqual("secret", settings.RdpPassword.ClearTextValue);
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

            var credentialPrompt = CreateCredentialsWorkflow(true, false, taskDialog);

            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("bob", settings.RdpUsername.Value);
            Assert.AreEqual("secret", settings.RdpPassword.ClearTextValue);
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

            var credentialPrompt = CreateCredentialsWorkflow(false, false, taskDialog);
            var window = this.serviceRegistry.AddMock<IConfigureCredentialsWorkflow>();

            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);

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

            var credentialPrompt = CreateCredentialsWorkflow(
                isGrantedPermissionToGenerateCredentials,
                false,
                taskDialog);

            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);
            settings.RdpUsername.StringValue = "alice";
            settings.RdpPassword.ClearTextValue = "alicespassword";

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("alice", settings.RdpUsername.Value);
            Assert.AreEqual("alicespassword", settings.RdpPassword.ClearTextValue);
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

            var credentialPrompt = CreateCredentialsWorkflow(true, false, taskDialog);
            var window = this.serviceRegistry.AddMock<IConfigureCredentialsWorkflow>();

            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);

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

            var credentialPrompt = CreateCredentialsWorkflow(true, false, taskDialog);

            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);

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

            var credentialPrompt = CreateCredentialsWorkflow(true, false, taskDialog);

            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);
            settings.RdpUsername.StringValue = "alice";
            settings.RdpPassword.ClearTextValue = "alicespassword";

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    RdpCredentialGenerationBehavior.Disallow,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("alice", settings.RdpUsername.Value);
            Assert.AreEqual("alicespassword", settings.RdpPassword.ClearTextValue);
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
            var taskDialog = new Mock<ITaskDialog>();

            var credentialPrompt = CreateCredentialsWorkflow(true, true, taskDialog);

            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);

            await credentialPrompt
                .SelectCredentialsAsync(
                    null,
                    SampleInstance,
                    settings,
                    RdpCredentialGenerationBehavior.Force,
                    true)
                .ConfigureAwait(true);

            Assert.AreEqual("bob", settings.RdpUsername.StringValue);
            Assert.AreEqual("secret", settings.RdpPassword.ClearTextValue);
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

            var credentialPrompt = CreateCredentialsWorkflow(false, false, taskDialog);
            var window = this.serviceRegistry.AddMock<IConfigureCredentialsWorkflow>();

            var settings = InstanceConnectionSettings.CreateNew(SampleInstance);

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
