﻿//
// Copyright 2023 Google LLC
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

using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Extensions.Shell.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Mvvm.Binding.Commands;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Session
{
    [TestFixture]
    public class TestSessionCommands
    {
        private static IContextCommand<ISession> GetFullScreenCommand(
            SessionCommands commands,
            FullScreenMode mode)
        {
            return mode == FullScreenMode.SingleScreen
                ? commands.EnterFullScreenOnSingleScreen
                : commands.EnterFullScreenOnAllScreens;
        }

        //---------------------------------------------------------------------
        // EnterFullScreen.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicable_ThenEnterFullScreenIsEnabled(
            [Values(
                FullScreenMode.SingleScreen,
                FullScreenMode.AllScreens)] FullScreenMode mode)
        {
            var sessionCommands = new SessionCommands();

            var connectedRdpSession = new Mock<IRemoteDesktopSession>();
            connectedRdpSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedRdpSession.SetupGet(s => s.CanEnterFullScreen).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                GetFullScreenCommand(sessionCommands, mode).QueryState(connectedRdpSession.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenEnterFullScreenIsDisabled(
            [Values(
                FullScreenMode.SingleScreen,
                FullScreenMode.AllScreens)] FullScreenMode mode)
        {
            var sessionCommands = new SessionCommands();

            var connectedFullScreenRdpSession = new Mock<IRemoteDesktopSession>();
            connectedFullScreenRdpSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedFullScreenRdpSession.SetupGet(s => s.CanEnterFullScreen).Returns(false);

            var disconnectedRdpSession = new Mock<IRemoteDesktopSession>();
            disconnectedRdpSession.SetupGet(s => s.IsConnected).Returns(false);

            var sshSession = new Mock<ISshTerminalSession>();

            Assert.AreEqual(
                CommandState.Disabled,
                GetFullScreenCommand(sessionCommands, mode).QueryState(connectedFullScreenRdpSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                GetFullScreenCommand(sessionCommands, mode).QueryState(disconnectedRdpSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                GetFullScreenCommand(sessionCommands, mode).QueryState(sshSession.Object));
        }

        //---------------------------------------------------------------------
        // Disconnect.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicable_ThenDisconnectIsEnabled()
        {
            var sessionCommands = new SessionCommands();

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.Disconnect.QueryState(connectedSession.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenDisconnectIsDisabled()
        {
            var sessionCommands = new SessionCommands();

            var disconnectedSession = new Mock<ISession>();
            disconnectedSession.SetupGet(s => s.IsConnected).Returns(false);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.Disconnect.QueryState(disconnectedSession.Object));
        }

        //---------------------------------------------------------------------
        // ShowSecurityScreen.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicable_ThenShowSecurityScreenIsEnabled()
        {
            var sessionCommands = new SessionCommands();

            var connectedSession = new Mock<IRemoteDesktopSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.ShowSecurityScreen.QueryState(connectedSession.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenShowSecurityScreenIsDisabled()
        {
            var sessionCommands = new SessionCommands();

            var disconnectedSession = new Mock<IRemoteDesktopSession>();
            disconnectedSession.SetupGet(s => s.IsConnected).Returns(false);

            var sshSession = new Mock<ISshTerminalSession>();
            sshSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.ShowSecurityScreen.QueryState(disconnectedSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.ShowSecurityScreen.QueryState(sshSession.Object));
        }

        //---------------------------------------------------------------------
        // ShowTaskManager.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicable_ThenShowTaskManagerIsEnabled()
        {
            var sessionCommands = new SessionCommands();

            var connectedSession = new Mock<IRemoteDesktopSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.ShowTaskManager.QueryState(connectedSession.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenShowTaskManagerIsDisabled()
        {
            var sessionCommands = new SessionCommands();

            var disconnectedSession = new Mock<IRemoteDesktopSession>();
            disconnectedSession.SetupGet(s => s.IsConnected).Returns(false);

            var sshSession = new Mock<ISshTerminalSession>();
            sshSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.ShowTaskManager.QueryState(disconnectedSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.ShowTaskManager.QueryState(sshSession.Object));
        }

        //---------------------------------------------------------------------
        // DownloadFiles.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicable_ThenDownloadFilesIsEnabled()
        {
            var sessionCommands = new SessionCommands();

            var connectedSession = new Mock<ISshTerminalSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.DownloadFiles.QueryState(connectedSession.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenDownloadFilesIsDisabled()
        {
            var sessionCommands = new SessionCommands();

            var disconnectedSession = new Mock<ISshTerminalSession>();
            disconnectedSession.SetupGet(s => s.IsConnected).Returns(false);

            var rdpSession = new Mock<IRemoteDesktopSession>();
            rdpSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.DownloadFiles.QueryState(disconnectedSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.DownloadFiles.QueryState(rdpSession.Object));
        }
    }
}
