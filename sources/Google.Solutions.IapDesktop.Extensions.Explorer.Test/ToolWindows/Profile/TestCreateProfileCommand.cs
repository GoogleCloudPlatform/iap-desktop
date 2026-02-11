//
// Copyright 2026 Google LLC
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

using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Auth;
using Google.Solutions.IapDesktop.Extensions.Explorer.ToolWindows.Profile;
using Google.Solutions.Testing.Application.Mocks;
using Moq;
using NUnit.Framework;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Explorer.Test.ToolWindows.Profile
{
    [TestFixture]
    public class TestCreateProfileCommand
    {
        //---------------------------------------------------------------------
        // Execute.
        //---------------------------------------------------------------------

        [Test]
        public void Execute_WhenCanceled()
        {
            var install = new Mock<IInstall>();
            var activator = new MockWindowActivator<NewProfileView, NewProfileViewModel, IDialogTheme>(
                DialogResult.Cancel,
                new NewProfileViewModel());

            var command = new CreateProfileCommand(
                install.Object,
                new Mock<IMainWindow>().Object,
                activator);

            command.Execute(new Mock<IUserProfile>().Object);

            install.Verify(p => p.CreateProfile(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Execute()
        {
            var viewModel = new NewProfileViewModel()
            {
                ProfileName = "New",
            };

            var profile = new Mock<IUserProfile>();
            var install = new Mock<IInstall>();
            install
                .Setup(i => i.CreateProfile(viewModel.ProfileName))
                .Returns(profile.Object);

            var activator = new MockWindowActivator<NewProfileView, NewProfileViewModel, IDialogTheme>(
                DialogResult.OK,
                viewModel);

            var command = new CreateProfileCommand(
                install.Object,
                new Mock<IMainWindow>().Object,
                activator);

            command.Execute(new Mock<IUserProfile>().Object);

            profile.Verify(p => p.Launch(), Times.Once);
        }
    }
}
