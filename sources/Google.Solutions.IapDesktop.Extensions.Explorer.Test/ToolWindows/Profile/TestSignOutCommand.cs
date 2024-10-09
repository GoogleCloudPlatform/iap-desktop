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


using Google.Solutions.Apis.Auth;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Extensions.Explorer.ToolWindows.Profile;
using Moq;
using NUnit.Framework;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Explorer.Test.ToolWindows.Profile
{
    [TestFixture]
    public class TestSignOutCommand
    {
        //---------------------------------------------------------------------
        // Execute.
        //---------------------------------------------------------------------

        [Test]
        public void Execute_WhenProfileIsDefault_ThenExecuteTerminatesSessionAndClosesWindow()
        {
            var session = new Mock<IOidcSession>();
            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.Session).Returns(session.Object);

            var profile = new Mock<IUserProfile>();
            profile.SetupGet(p => p.IsDefault).Returns(true);

            var mainWindow = new Mock<IMainWindow>();

            var command = new SignOutCommand(
                new Mock<IInstall>().Object,
                authorization.Object,
                mainWindow.Object,
                new Mock<IConfirmationDialog>().Object);

            command.Execute(profile.Object);

            mainWindow.Verify(w => w.Close(), Times.Once);
            session.Verify(s => s.Terminate(), Times.Once);
        }

        [Test]
        public void Execute_WhenProfileIsNotDefaultAndUserDecidesToKeepProfile_ThenExecuteTerminatesSessionAndClosesWindow()
        {
            var session = new Mock<IOidcSession>();
            var authorization = new Mock<IAuthorization>();
            authorization.SetupGet(a => a.Session).Returns(session.Object);

            var profile = new Mock<IUserProfile>();
            profile.SetupGet(p => p.IsDefault).Returns(true);

            var mainWindow = new Mock<IMainWindow>();
            var dialog = new Mock<IConfirmationDialog>();

            dialog
                .Setup(d => d.Confirm(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(DialogResult.Yes); // Yes, keep profile
            var command = new SignOutCommand(
                new Mock<IInstall>().Object,
                authorization.Object,
                mainWindow.Object,
                dialog.Object);

            command.Execute(profile.Object);

            mainWindow.Verify(w => w.Close(), Times.Once);
            session.Verify(s => s.Terminate(), Times.Once);
        }
    }
}
