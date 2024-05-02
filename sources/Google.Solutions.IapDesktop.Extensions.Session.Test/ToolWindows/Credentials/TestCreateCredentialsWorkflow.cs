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
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Settings;
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
        public void WhenUsernameProvided_ThenCreateCredentialsShowsUsername()
        {
            var newCredentialViewModel = new NewCredentialsViewModel();
            var newCredentialDialogFactory = new MockDialogFactory<NewCredentialsView, NewCredentialsViewModel>(
                DialogResult.Cancel, // Cancel dialog.
                newCredentialViewModel);

            var workflow = new CreateCredentialsWorkflow(
                new Mock<IAuthorization>().Object,
                new Mock<IJobService>().Object,
                new Mock<IWindowsCredentialGenerator>().Object,
                newCredentialDialogFactory,
                new Mock<IDialogFactory<ShowCredentialsView, ShowCredentialsViewModel>>().Object);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => workflow.CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    "alice",
                    false).Wait());

            Assert.AreEqual("alice", newCredentialViewModel.Username);
        }

        [Test]
        public void WhensUsernameIsNullOrEmpty_ThenCreateCredentialsShowsUsername(
            [Values("", null)]string username)
        {
            var newCredentialViewModel = new NewCredentialsViewModel();
            var newCredentialDialogFactory = new MockDialogFactory<NewCredentialsView, NewCredentialsViewModel>(
                DialogResult.Cancel, // Cancel dialog.
                newCredentialViewModel);

            var workflow = new CreateCredentialsWorkflow(
                CreateAuthorizationMock("bobsemail@gmail.com").Object,
                new Mock<IJobService>().Object,
                new Mock<IWindowsCredentialGenerator>().Object,
                newCredentialDialogFactory,
                new Mock<IDialogFactory<ShowCredentialsView, ShowCredentialsViewModel>>().Object);

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => workflow.CreateCredentialsAsync(
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

            var credentialGenerator = new Mock<IWindowsCredentialGenerator>();
            credentialGenerator
                .Setup(a => a.CreateWindowsCredentialsAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<string>(),
                    It.Is<UserFlags>(t => t == UserFlags.AddToAdministrators),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NetworkCredential("generated", "password"));

            var workflow = new CreateCredentialsWorkflow(
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                credentialGenerator.Object,
                newCredentialDialogFactory,
                showCredentialDialogFactory);

            var credentials = await workflow
                .CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    "alice",
                    false)
                .ConfigureAwait(false);

            Assert.AreEqual("generated", credentials.UserName);
            Assert.AreEqual("password", credentials.Password);
        }

        //---------------------------------------------------------------------
        // Silent.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenUsernameProvidedAndSilentIsTrueThenCreateCredentialsReturnsCredentials()
        {
            var credentialGenerator = new Mock<IWindowsCredentialGenerator>();
            credentialGenerator
                .Setup(a => a.CreateWindowsCredentialsAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<string>(),
                    It.Is<UserFlags>(t => t == UserFlags.AddToAdministrators),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NetworkCredential("generated", "password"));

            var workflow = new CreateCredentialsWorkflow(
                new Mock<IAuthorization>().Object,
                new SynchronousJobService(),
                credentialGenerator.Object,
                new Mock<IDialogFactory<NewCredentialsView, NewCredentialsViewModel>>().Object,
                new Mock<IDialogFactory<ShowCredentialsView, ShowCredentialsViewModel>>().Object);

            var credentials = await workflow
                .CreateCredentialsAsync(
                    null,
                    SampleInstance,
                    "alice",
                    true)
                .ConfigureAwait(false);

            Assert.AreEqual("generated", credentials.UserName);
            Assert.AreEqual("password", credentials.Password);
        }
    }
}
