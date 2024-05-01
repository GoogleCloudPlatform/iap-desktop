using Google.Apis.Auth.OAuth2;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
                .Setup(a => a.CreateWindowsCredentialsAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<string>(),
                    It.Is<UserFlags>(t => t == UserFlags.AddToAdministrators),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(credential);

            return credentialGenerator;
        }

        //---------------------------------------------------------------------
        // CreateCredentialsAsync - non-silent.
        //---------------------------------------------------------------------


        [Test]
        public void WhenUsernameProvided_ThenCreateCredentialsShowsUsername()
        {
            var newCredentialViewModel = new NewCredentialsViewModel();
            var newCredentialDialogFactory = new MockDialogFactory<NewCredentialsView, NewCredentialsViewModel>(
                DialogResult.Cancel, // Cancel dialog.
                newCredentialViewModel);

            var editor = new RdpCredentialEditor(
                null,
                new Extensions.Session.Settings.ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new Mock<IJobService>().Object,
                new Mock<IWindowsCredentialGenerator>().Object,
                newCredentialDialogFactory,
                new Mock<IDialogFactory<ShowCredentialsView, ShowCredentialsViewModel>>().Object);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => editor.CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    "alice",
                    false).Wait());

            Assert.AreEqual("alice", newCredentialViewModel.Username);
        }


        [Test]
        public void WhensUsernameIsNullOrEmpty_ThenCreateCredentialsShowsUsername(
            [Values("", null)] string username)
        {
            var newCredentialViewModel = new NewCredentialsViewModel();
            var newCredentialDialogFactory = new MockDialogFactory<NewCredentialsView, NewCredentialsViewModel>(
                DialogResult.Cancel, // Cancel dialog.
                newCredentialViewModel);

            var editor = new RdpCredentialEditor(
                null,
                new Extensions.Session.Settings.ConnectionSettings(SampleInstance),
                CreateAuthorizationMock("bobsemail@gmail.com").Object,
                new Mock<IJobService>().Object,
                new Mock<IWindowsCredentialGenerator>().Object,
                newCredentialDialogFactory,
                new Mock<IDialogFactory<ShowCredentialsView, ShowCredentialsViewModel>>().Object);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => editor.CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    username,
                    false).Wait());

            Assert.AreEqual("bobsemail", newCredentialViewModel.Username);
        }

        [Test]
        public async Task WhenDialogConfirmed_ThenCreateCredentialsReturnsCredentials()
        {
            var newCredentialViewModel = new NewCredentialsViewModel();
            var newCredentialDialogFactory = new MockDialogFactory<NewCredentialsView, NewCredentialsViewModel>(
                DialogResult.OK,
                newCredentialViewModel);

            var showCredentialDialogFactory = new MockDialogFactory<ShowCredentialsView, ShowCredentialsViewModel>(
                DialogResult.OK);

            var generatedCredentials = new NetworkCredential("generated", "password");

            var editor = new RdpCredentialEditor(
                null,
                new Extensions.Session.Settings.ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                CreateCredentialGeneratorMock(generatedCredentials).Object,
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
        // CreateCredentialsAsync - silent.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUsernameProvidedAndSilentIsTrue_ThenCreateCredentialsReturnsCredentials()
        {
            var generatedCredentials = new NetworkCredential("generated", "password");

            var editor = new RdpCredentialEditor(
                null,
                new Extensions.Session.Settings.ConnectionSettings(SampleInstance),
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                CreateCredentialGeneratorMock(generatedCredentials).Object,
                new Mock<IDialogFactory<NewCredentialsView, NewCredentialsViewModel>>().Object,
                new Mock<IDialogFactory<ShowCredentialsView, ShowCredentialsViewModel>>().Object);

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
        // ReplaceCredentialsAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task ReplaceCredentialsUpdatesSettings()
        {
            var settings = new Extensions.Session.Settings.ConnectionSettings(SampleInstance);
            settings.RdpUsername.Value = "bob";

            var generatedCredentials = new NetworkCredential("bob", "password");
            var editor = new RdpCredentialEditor(
                null,
                settings,
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                CreateCredentialGeneratorMock(generatedCredentials).Object,
                new Mock<IDialogFactory<NewCredentialsView, NewCredentialsViewModel>>().Object,
                new Mock<IDialogFactory<ShowCredentialsView, ShowCredentialsViewModel>>().Object);

            await editor
                .ReplaceCredentialsAsync(true)
                .ConfigureAwait(false);

            Assert.AreEqual(generatedCredentials.UserName, settings.RdpUsername.Value);
            Assert.AreEqual(generatedCredentials.Password, settings.RdpPassword.Value.AsClearText());
            Assert.AreEqual(".", settings.RdpDomain.Value);
        }
    }
}
