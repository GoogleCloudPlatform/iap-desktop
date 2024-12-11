//
// Copyright 2024 Google LLC
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

using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Settings;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.Mocks;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Session
{
    [TestFixture]
    public class TestRdpCredentialEditor
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static Mock<IAuthorization> CreateAuthorizationMock(string username)
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

        private static Mock<IWindowsCredentialGenerator> CreateCredentialGeneratorMock(
            NetworkCredential credential)
        {
            var credentialGenerator = new Mock<IWindowsCredentialGenerator>();
            credentialGenerator
                .Setup(a => a.IsGrantedPermissionToCreateWindowsCredentialsAsync(It.IsAny<InstanceLocator>()))
                .ReturnsAsync(true);
            credentialGenerator
                .Setup(a => a.CreateWindowsCredentialsAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<string>(),
                    It.Is<UserFlags>(t => t == UserFlags.AddToAdministrators),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(credential);

            return credentialGenerator;
        }

        private static Mock<ICredentialDialog> CreateCredentialDialogMock(
            DialogResult dialogResult,
            NetworkCredential? credential)
        {
            var credentialDialog = new Mock<ICredentialDialog>();
            var allowSave = true;
            credentialDialog
                .Setup(d => d.PromptForWindowsCredentials(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<CredentialDialogParameters>(),
                    out allowSave,
                    out credential))
                .Returns(dialogResult);
            return credentialDialog;
        }

        //---------------------------------------------------------------------
        // AreCredentialsComplete.
        //---------------------------------------------------------------------

        [Test]
        [TestCase("", "", false)]
        [TestCase(null, "", false)]
        [TestCase("", null, false)]
        [TestCase("user", "", false)]
        [TestCase("", "pwd", false)]
        [TestCase("user", "pwd", true)]
        public void AreCredentialsComplete(
            string username,
            string password,
            bool complete)
        {
            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new Mock<IJobService>().Object,
                new Mock<IWindowsCredentialGenerator>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<ICredentialDialog>().Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            editor.Settings.RdpUsername.Value = username;
            editor.Settings.RdpPassword.SetClearTextValue(password);

            Assert.AreEqual(complete, editor.AreCredentialsComplete);
        }

        //---------------------------------------------------------------------
        // PromptForCredentials.
        //---------------------------------------------------------------------

        [Test]
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("bob", true)]
        public void PromptForCredentials_WhenUsernameNotEmpty(
            string username,
            bool prefilled)
        {
            var credentialDialog = new Mock<ICredentialDialog>();
            var save = false;
            var credential = new NetworkCredential("username", "password");

            credentialDialog
                .Setup(d => d.PromptForWindowsCredentials(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<CredentialDialogParameters>(),
                    out save,
                    out credential))
                .Returns(DialogResult.OK);

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new Mock<IJobService>().Object,
                new Mock<IWindowsCredentialGenerator>().Object,
                new Mock<ITaskDialog>().Object,
                credentialDialog.Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            editor.Settings.RdpUsername.Value = username;
            editor.PromptForCredentials();

            credentialDialog.Verify(
                d => d.PromptForWindowsCredentials(
                    It.IsAny<IWin32Window>(),
                    It.Is<CredentialDialogParameters>(d => prefilled == (d.InputCredential != null)),
                    out save,
                    out credential),
                Times.Once);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void PromptForCredentials_WhenUserAllowsSaving(
            bool allowSave)
        {
            var credentialDialog = new Mock<ICredentialDialog>();
            var credential = new NetworkCredential("username", "password");

            credentialDialog
                .Setup(d => d.PromptForWindowsCredentials(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<CredentialDialogParameters>(),
                    out allowSave,
                    out credential))
                .Returns(DialogResult.OK);

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new Mock<IJobService>().Object,
                new Mock<IWindowsCredentialGenerator>().Object,
                new Mock<ITaskDialog>().Object,
                credentialDialog.Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            editor.PromptForCredentials();

            Assert.AreEqual(allowSave, editor.AllowSave);
        }

        //---------------------------------------------------------------------
        // IsGrantedPermissionToCreateWindowsCredentials.
        //---------------------------------------------------------------------

        [Test]
        public async Task IsGrantedPermissionToCreateWindowsCredentials_WhenReauthRequired()
        {
            var credentialGenerator = new Mock<IWindowsCredentialGenerator>();
            credentialGenerator
                .Setup(a => a.IsGrantedPermissionToCreateWindowsCredentialsAsync(SampleInstance))
                .ThrowsAsync(new TokenResponseException(new TokenErrorResponse()
                {
                    Error = "invalid_grant"
                }));

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new Mock<IJobService>().Object,
                credentialGenerator.Object,
                new Mock<ITaskDialog>().Object,
                new Mock<ICredentialDialog>().Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            Assert.IsTrue(await editor
                .IsGrantedPermissionToCreateWindowsCredentialsAsync()
                .ConfigureAwait(false));

            credentialGenerator
                .Verify(a => a.IsGrantedPermissionToCreateWindowsCredentialsAsync(SampleInstance), Times.Once);
        }

        //---------------------------------------------------------------------
        // CreateCredentials - non-silent.
        //---------------------------------------------------------------------

        [Test]
        public void CreateCredentials_WhenUsernameProvided_ThenCreateCredentialsShowsUsername()
        {
            var newCredentialViewModel = new NewCredentialsViewModel();
            var newCredentialDialogFactory =
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(
                    DialogResult.Cancel, // Cancel dialog.
                    newCredentialViewModel);

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new Mock<IJobService>().Object,
                new Mock<IWindowsCredentialGenerator>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<ICredentialDialog>().Object,
                newCredentialDialogFactory,
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => editor.CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    "alice",
                    false).Wait());

            Assert.AreEqual("alice", newCredentialViewModel.Username);
        }


        [Test]
        public void CreateCredentials_WhensUsernameIsNullOrEmpty_ThenCreateCredentialsShowsUsername(
            [Values("", null)] string username)
        {
            var newCredentialViewModel = new NewCredentialsViewModel();
            var newCredentialDialogFactory =
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(
                    DialogResult.Cancel, // Cancel dialog.
                    newCredentialViewModel);

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                CreateAuthorizationMock("bobsemail@gmail.com").Object,
                new Mock<IJobService>().Object,
                new Mock<IWindowsCredentialGenerator>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<ICredentialDialog>().Object,
                newCredentialDialogFactory,
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => editor.CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    username,
                    false).Wait());

            Assert.AreEqual("bobsemail", newCredentialViewModel.Username);
        }

        [Test]
        public async Task CreateCredentials_WhenDialogConfirmed_ThenCreateCredentialsReturnsCredentials()
        {
            var newCredentialViewModel = new NewCredentialsViewModel();
            var newCredentialDialogFactory =
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(
                    DialogResult.OK,
                    newCredentialViewModel);

            var showCredentialDialogFactory =
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>(
                    DialogResult.OK);

            var generatedCredentials = new NetworkCredential("generated", "password");

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                CreateCredentialGeneratorMock(generatedCredentials).Object,
                new Mock<ITaskDialog>().Object,
                new Mock<ICredentialDialog>().Object,
                newCredentialDialogFactory,
                showCredentialDialogFactory);

            var credentials = await editor
                .CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    "alice",
                    false)
                .ConfigureAwait(false);

            Assert.AreEqual(generatedCredentials.UserName, credentials.UserName);
            Assert.AreEqual(generatedCredentials.Password, credentials.Password);
        }

        //---------------------------------------------------------------------
        // CreateCredentials - silent.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateCredentials_WhenUsernameProvidedAndSilentIsTrue_ThenCreateCredentialsReturnsCredentials()
        {
            var generatedCredentials = new NetworkCredential("generated", "password");

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                CreateCredentialGeneratorMock(generatedCredentials).Object,
                new Mock<ITaskDialog>().Object,
                new Mock<ICredentialDialog>().Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            var credentials = await editor
                .CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    "alice",
                    true)
                .ConfigureAwait(false);

            Assert.AreEqual(generatedCredentials.UserName, credentials.UserName);
            Assert.AreEqual(generatedCredentials.Password, credentials.Password);
        }

        //---------------------------------------------------------------------
        // GenerateCredentials.
        //---------------------------------------------------------------------

        [Test]
        public async Task GenerateCredentials_UpdatesSettings()
        {
            var settings = new ConnectionSettings(SampleInstance);
            settings.RdpUsername.Value = "bob";

            var generatedCredentials = new NetworkCredential("bob", "password");
            var editor = new RdpCredentialEditor(
                null,
                settings,
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                CreateCredentialGeneratorMock(generatedCredentials).Object,
                new Mock<ITaskDialog>().Object,
                new Mock<ICredentialDialog>().Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            await editor
                .GenerateCredentialsAsync(true)
                .ConfigureAwait(false);

            Assert.AreEqual(generatedCredentials.UserName, settings.RdpUsername.Value);
            Assert.AreEqual(generatedCredentials.Password, settings.RdpPassword.Value.ToClearText());
            Assert.AreEqual(".", settings.RdpDomain.Value);
        }

        //---------------------------------------------------------------------
        // AmendCredentials - NLA.
        //---------------------------------------------------------------------

        [Test]
        public async Task AmendCredentials_WhenNlaDisabled_ThenAmendCredentialsReturns()
        {
            var settings = new ConnectionSettings(SampleInstance);
            settings.RdpNetworkLevelAuthentication.Value = RdpNetworkLevelAuthentication.Disabled;

            var editor = new RdpCredentialEditor(
                null,
                settings,
                new Mock<IAuthorization>().Object,
                new Mock<IJobService>().Object,
                new Mock<IWindowsCredentialGenerator>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<ICredentialDialog>().Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            Assert.IsNull(editor.Settings.RdpUsername.Value);

            await editor
                .AmendCredentialsAsync(RdpCredentialGenerationBehavior._Default)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task AmendCredentials_WhenAutomaticLogonDisabled_ThenAmendCredentialsReturns()
        {
            var settings = new ConnectionSettings(SampleInstance);
            settings.RdpAutomaticLogon.Value = RdpAutomaticLogon.Disabled;

            var editor = new RdpCredentialEditor(
                null,
                settings,
                new Mock<IAuthorization>().Object,
                new Mock<IJobService>().Object,
                new Mock<IWindowsCredentialGenerator>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<ICredentialDialog>().Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            Assert.IsNull(editor.Settings.RdpUsername.Value);

            await editor
                .AmendCredentialsAsync(RdpCredentialGenerationBehavior._Default)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // AmendCredentials - Force.
        //---------------------------------------------------------------------

        [Test]
        public async Task AmendCredentials_WhenCredentialGenerationForcedAndUserAllowedToGenerateCredentials_ThenAmendCredentialsReplacesCredentials()
        {
            var generatedCredentials = new NetworkCredential("generated", "password");

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                CreateAuthorizationMock("bob@example.com").Object,
                new SynchronousJobService(),
                CreateCredentialGeneratorMock(generatedCredentials).Object,
                new Mock<ITaskDialog>().Object,
                new Mock<ICredentialDialog>().Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            await editor
                .AmendCredentialsAsync(RdpCredentialGenerationBehavior.Force)
                .ConfigureAwait(false);

            Assert.AreEqual(generatedCredentials.UserName, editor.Settings.RdpUsername.Value);
            Assert.AreEqual(generatedCredentials.Password, editor.Settings.RdpPassword.GetClearTextValue());
        }

        [Test]
        public async Task AmendCredentials_WhenCredentialGenerationForcedAndUserNotAllowedToGenerateCredentials_ThenAmendCredentialsShowsCredentialPrompt()
        {
            var promptedCredentials = new NetworkCredential("user", "password");

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                new Mock<IWindowsCredentialGenerator>().Object,
                new Mock<ITaskDialog>().Object,
                CreateCredentialDialogMock(
                    DialogResult.OK,
                    promptedCredentials).Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            await editor
                .AmendCredentialsAsync(RdpCredentialGenerationBehavior.Force)
                .ConfigureAwait(false);

            Assert.AreEqual(promptedCredentials.UserName, editor.Settings.RdpUsername.Value);
            Assert.AreEqual(promptedCredentials.Password, editor.Settings.RdpPassword.GetClearTextValue());
        }

        //---------------------------------------------------------------------
        // AmendCredentials - Allow.
        //---------------------------------------------------------------------

        [Test]
        [TestCase(RdpCredentialGenerationBehavior.Allow)]
        [TestCase(RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound)]
        public async Task AmendCredentials_WhenCredentialGenerationAllowedAndUserAllowedToGenerateCredentials_ThenAmendCredentialsShowsTaskDialog(
            RdpCredentialGenerationBehavior behavior)
        {
            var taskDialog = new Mock<ITaskDialog>();

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                CreateCredentialGeneratorMock(new NetworkCredential()).Object,
                taskDialog.Object,
                new Mock<ICredentialDialog>().Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            await editor
                .AmendCredentialsAsync(behavior)
                .ConfigureAwait(false);

            taskDialog.Verify(d => d.ShowDialog(
                null,
                It.IsAny<TaskDialogParameters>()), Times.Once());
        }

        [Test]
        [TestCase(RdpCredentialGenerationBehavior.Allow)]
        [TestCase(RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound)]
        public async Task AmendCredentials_WhenCredentialGenerationAllowedAndUserNotAllowedToGenerateCredentials_ThenAmendCredentialsShowsCredentialPrompt(
            RdpCredentialGenerationBehavior behavior)
        {
            var taskDialog = new Mock<ITaskDialog>();

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                new Mock<IWindowsCredentialGenerator>().Object,
                taskDialog.Object,
                CreateCredentialDialogMock(
                    DialogResult.OK,
                    new NetworkCredential()).Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            await editor
                .AmendCredentialsAsync(behavior)
                .ConfigureAwait(false);

            taskDialog.Verify(d => d.ShowDialog(
                null,
                It.IsAny<TaskDialogParameters>()), Times.Never());
        }

        [Test]
        [TestCase(RdpCredentialGenerationBehavior.Allow)]
        [TestCase(RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound)]
        public async Task AmendCredentials_WhenCredentialGenerationAllowedAndCredentialsComplete_ThenAmendCredentialsReturns(
            RdpCredentialGenerationBehavior behavior)
        {
            var settings = new ConnectionSettings(SampleInstance);
            settings.RdpUsername.Value = "user";
            settings.RdpPassword.SetClearTextValue("password");

            var taskDialog = new Mock<ITaskDialog>();
            var credentialDialog = new Mock<ICredentialDialog>();

            var editor = new RdpCredentialEditor(
                null,
                settings,
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                new Mock<IWindowsCredentialGenerator>().Object,
                taskDialog.Object,
                credentialDialog.Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            await editor
                .AmendCredentialsAsync(behavior)
                .ConfigureAwait(false);

            bool save;
            NetworkCredential? credential;
            credentialDialog.Verify(d => d.PromptForWindowsCredentials(
                It.IsAny<IWin32Window>(),
                It.IsAny<CredentialDialogParameters>(),
                out save,
                out credential), Times.Never());
            taskDialog.Verify(d => d.ShowDialog(
                null,
                It.IsAny<TaskDialogParameters>()), Times.Never());
        }

        //---------------------------------------------------------------------
        // AmendCredentials - Disallow.
        //---------------------------------------------------------------------

        [Test]
        public async Task AmendCredentials_WhenCredentialGenerationDisallowedAndUserAllowedToGenerateCredentials_ThenAmendCredentialsShowsCredentialPrompt()
        {
            var taskDialog = new Mock<ITaskDialog>();

            var editor = new RdpCredentialEditor(
                null,
                new ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                CreateCredentialGeneratorMock(new NetworkCredential()).Object,
                taskDialog.Object,
                CreateCredentialDialogMock(
                    DialogResult.OK,
                    new NetworkCredential()).Object,
                new MockWindowActivator<NewCredentialsView, NewCredentialsViewModel, IDialogTheme>(),
                new MockWindowActivator<ShowCredentialsView, ShowCredentialsViewModel, IDialogTheme>());

            await editor
                .AmendCredentialsAsync(RdpCredentialGenerationBehavior.Disallow)
                .ConfigureAwait(false);

            taskDialog.Verify(d => d.ShowDialog(
                null,
                It.IsAny<TaskDialogParameters>()), Times.Never());
        }
    }
}
