//
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

using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh;
using Google.Solutions.Mvvm.Binding.Commands;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Session
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
        public void EnterFullScreen_WhenApplicable_ThenEnterFullScreenIsEnabled(
            [Values(
                FullScreenMode.SingleScreen,
                FullScreenMode.AllScreens)] FullScreenMode mode)
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedRdpSession = new Mock<IRdpSession>();
            connectedRdpSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedRdpSession.SetupGet(s => s.CanEnterFullScreen).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                GetFullScreenCommand(sessionCommands, mode).QueryState(connectedRdpSession.Object));
        }

        [Test]
        public void EnterFullScreen_WhenNotApplicable_ThenEnterFullScreenIsDisabled(
            [Values(
                FullScreenMode.SingleScreen,
                FullScreenMode.AllScreens)] FullScreenMode mode)
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedFullScreenRdpSession = new Mock<IRdpSession>();
            connectedFullScreenRdpSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedFullScreenRdpSession.SetupGet(s => s.CanEnterFullScreen).Returns(false);

            var closedRdpSession = new Mock<IRdpSession>();
            closedRdpSession.SetupGet(s => s.IsConnected).Returns(false);

            var sshSession = new Mock<ISshTerminalSession>();

            Assert.AreEqual(
                CommandState.Disabled,
                GetFullScreenCommand(sessionCommands, mode).QueryState(connectedFullScreenRdpSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                GetFullScreenCommand(sessionCommands, mode).QueryState(closedRdpSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                GetFullScreenCommand(sessionCommands, mode).QueryState(sshSession.Object));
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public void Close_WhenApplicable_ThenCloseIsEnabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.Close.QueryState(connectedSession.Object));
        }

        [Test]
        public void Close_WhenNotApplicable_ThenCloseIsDisabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<ISession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.Close.QueryState(closedSession.Object));
        }

        [Test]
        public async Task Close()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var session = new Mock<ISession>();
            await sessionCommands.Close
                .ExecuteAsync(session.Object)
                .ConfigureAwait(true);

            session.Verify(s => s.Close(), Times.Once);
        }

        //---------------------------------------------------------------------
        // CloseAll.
        //---------------------------------------------------------------------

        [Test]
        public void CloseAll_WhenApplicable_ThenCloseAllIsEnabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.CloseAll.QueryState(connectedSession.Object));
        }

        [Test]
        public async Task CloseAll()
        {
            var session1 = new Mock<ISession>();
            var session2 = new Mock<ISession>();

            var sessionBroker = new Mock<ISessionBroker>();
            sessionBroker
                .SetupGet(b => b.Sessions)
                .Returns(new[] { session1.Object, session2.Object });

            var sessionCommands = new SessionCommands(sessionBroker.Object);

            await sessionCommands.CloseAll
                .ExecuteAsync(session1.Object)
                .ConfigureAwait(true);

            session1.Verify(s => s.Close(), Times.Once);
            session2.Verify(s => s.Close(), Times.Once);
        }

        //---------------------------------------------------------------------
        // CloseAllButThis.
        //---------------------------------------------------------------------

        [Test]
        public void CloseAllButThis_WhenApplicable_ThenCloseAllButThisIsEnabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.CloseAllButThis.QueryState(connectedSession.Object));
        }

        [Test]
        public async Task CloseAllButThis()
        {
            var session1 = new Mock<ISession>();
            var session2 = new Mock<ISession>();
            var session3 = new Mock<ISession>();

            var sessionBroker = new Mock<ISessionBroker>();
            sessionBroker
                .SetupGet(b => b.Sessions)
                .Returns(new[] { session1.Object, session2.Object, session3.Object });

            var sessionCommands = new SessionCommands(sessionBroker.Object);

            await sessionCommands.CloseAllButThis
                .ExecuteAsync(session2.Object)
                .ConfigureAwait(true);

            session1.Verify(s => s.Close(), Times.Once);
            session2.Verify(s => s.Close(), Times.Never);
            session3.Verify(s => s.Close(), Times.Once);
        }

        //---------------------------------------------------------------------
        // ShowSecurityScreen.
        //---------------------------------------------------------------------

        [Test]
        public void ShowSecurityScreen_WhenApplicable_ThenShowSecurityScreenIsEnabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<IRdpSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.ShowSecurityScreen.QueryState(connectedSession.Object));
        }

        [Test]
        public void ShowSecurityScreen_WhenNotApplicable_ThenShowSecurityScreenIsDisabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<IRdpSession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            var sshSession = new Mock<ISshTerminalSession>();
            sshSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.ShowSecurityScreen.QueryState(closedSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.ShowSecurityScreen.QueryState(sshSession.Object));
        }

        //---------------------------------------------------------------------
        // ShowTaskManager.
        //---------------------------------------------------------------------

        [Test]
        public void ShowTaskManager_WhenApplicable_ThenShowTaskManagerIsEnabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<IRdpSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.ShowTaskManager.QueryState(connectedSession.Object));
        }

        [Test]
        public void ShowTaskManager_WhenNotApplicable_ThenShowTaskManagerIsDisabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<IRdpSession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            var sshSession = new Mock<ISshTerminalSession>();
            sshSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.ShowTaskManager.QueryState(closedSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.ShowTaskManager.QueryState(sshSession.Object));
        }

        //---------------------------------------------------------------------
        // TypeClipboardText.
        //---------------------------------------------------------------------

        [Test]
        public void TypeClipboardText_WhenApplicable_ThenTypeClipboardTextIsEnabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<IRdpSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.TypeClipboardText.QueryState(connectedSession.Object));
        }

        [Test]
        public void TypeClipboardText_WhenNotApplicable_ThenTypeClipboardTextIsDisabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<IRdpSession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            var sshSession = new Mock<ISshTerminalSession>();
            sshSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.TypeClipboardText.QueryState(closedSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.TypeClipboardText.QueryState(sshSession.Object));
        }

        //---------------------------------------------------------------------
        // DownloadFiles.
        //---------------------------------------------------------------------

        [Test]
        public void DownloadFiles_WhenApplicable_ThenDownloadFilesIsEnabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedSession.SetupGet(s => s.CanTransferFiles).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.DownloadFiles.QueryState(connectedSession.Object));
        }

        [Test]
        public void DownloadFiles_WhenNotConnected_ThenDownloadFilesIsDisabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<ISession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            var rdpSession = new Mock<IRdpSession>();
            rdpSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.DownloadFiles.QueryState(closedSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.DownloadFiles.QueryState(rdpSession.Object));
        }

        [Test]
        public void DownloadFiles_WhenFileTransferNotSupported_ThenDownloadFilesIsDisabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedSession.SetupGet(s => s.CanTransferFiles).Returns(false);

            var rdpSession = new Mock<IRdpSession>();
            rdpSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.DownloadFiles.QueryState(connectedSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.DownloadFiles.QueryState(rdpSession.Object));
        }

        //---------------------------------------------------------------------
        // UploadFiles.
        //---------------------------------------------------------------------

        [Test]
        public void UploadFiles_WhenApplicable_ThenUploadFilesIsEnabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedSession.SetupGet(s => s.CanTransferFiles).Returns(true);

            Assert.AreEqual(
                CommandState.Enabled,
                sessionCommands.UploadFiles.QueryState(connectedSession.Object));
        }

        [Test]
        public void UploadFiles_WhenNotConnected_ThenUploadFilesIsDisabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<ISession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            var rdpSession = new Mock<IRdpSession>();
            rdpSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.UploadFiles.QueryState(closedSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.UploadFiles.QueryState(rdpSession.Object));
        }

        [Test]
        public void UploadFiles_WhenFileTransferNotSupported_ThenUploadFilesIsDisabled()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedSession.SetupGet(s => s.CanTransferFiles).Returns(false);

            var rdpSession = new Mock<IRdpSession>();
            rdpSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.UploadFiles.QueryState(connectedSession.Object));
            Assert.AreEqual(
                CommandState.Disabled,
                sessionCommands.UploadFiles.QueryState(rdpSession.Object));
        }
    }
}
