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
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
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
        public void EnterFullScreen_WhenApplicable(
            [Values(
                FullScreenMode.SingleScreen,
                FullScreenMode.AllScreens)] FullScreenMode mode)
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedRdpSession = new Mock<IRdpSession>();
            connectedRdpSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedRdpSession.SetupGet(s => s.CanEnterFullScreen).Returns(true);

            Assert.That(
                GetFullScreenCommand(sessionCommands, mode).QueryState(connectedRdpSession.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void EnterFullScreen_WhenNotApplicable(
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

            var sshSession = new Mock<ISshSession>();

            Assert.That(
                GetFullScreenCommand(sessionCommands, mode).QueryState(connectedFullScreenRdpSession.Object), Is.EqualTo(CommandState.Disabled));
            Assert.That(
                GetFullScreenCommand(sessionCommands, mode).QueryState(closedRdpSession.Object), Is.EqualTo(CommandState.Disabled));
            Assert.That(
                GetFullScreenCommand(sessionCommands, mode).QueryState(sshSession.Object), Is.EqualTo(CommandState.Disabled));
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public void Close_WhenApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.Close.QueryState(connectedSession.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void Close_WhenNotApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<ISession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            Assert.That(
                sessionCommands.Close.QueryState(closedSession.Object), Is.EqualTo(CommandState.Disabled));
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
        public void CloseAll_WhenApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.CloseAll.QueryState(connectedSession.Object), Is.EqualTo(CommandState.Enabled));
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
        public void CloseAllButThis_WhenApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.CloseAllButThis.QueryState(connectedSession.Object), Is.EqualTo(CommandState.Enabled));
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
        public void ShowSecurityScreen_WhenApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<IRdpSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.ShowSecurityScreen.QueryState(connectedSession.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void ShowSecurityScreen_WhenNotApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<IRdpSession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            var sshSession = new Mock<ISshSession>();
            sshSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.ShowSecurityScreen.QueryState(closedSession.Object), Is.EqualTo(CommandState.Disabled));
            Assert.That(
                sessionCommands.ShowSecurityScreen.QueryState(sshSession.Object), Is.EqualTo(CommandState.Disabled));
        }

        //---------------------------------------------------------------------
        // ShowTaskManager.
        //---------------------------------------------------------------------

        [Test]
        public void ShowTaskManager_WhenApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<IRdpSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.ShowTaskManager.QueryState(connectedSession.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void ShowTaskManager_WhenNotApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<IRdpSession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            var sshSession = new Mock<ISshSession>();
            sshSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.ShowTaskManager.QueryState(closedSession.Object), Is.EqualTo(CommandState.Disabled));
            Assert.That(
                sessionCommands.ShowTaskManager.QueryState(sshSession.Object), Is.EqualTo(CommandState.Disabled));
        }

        //---------------------------------------------------------------------
        // Logoff.
        //---------------------------------------------------------------------

        [Test]
        public void Logoff_WhenApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<IRdpSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.Logoff.QueryState(connectedSession.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void Logoff_WhenNotApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<IRdpSession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            var sshSession = new Mock<ISshSession>();
            sshSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.Logoff.QueryState(closedSession.Object), Is.EqualTo(CommandState.Disabled));
            Assert.That(
                sessionCommands.Logoff.QueryState(sshSession.Object), Is.EqualTo(CommandState.Disabled));
        }

        //---------------------------------------------------------------------
        // TypeClipboardText.
        //---------------------------------------------------------------------

        [Test]
        public void TypeClipboardText_WhenApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<IRdpSession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.TypeClipboardText.QueryState(connectedSession.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void TypeClipboardText_WhenNotApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<IRdpSession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            var sshSession = new Mock<ISshSession>();
            sshSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.TypeClipboardText.QueryState(closedSession.Object), Is.EqualTo(CommandState.Disabled));
            Assert.That(
                sessionCommands.TypeClipboardText.QueryState(sshSession.Object), Is.EqualTo(CommandState.Disabled));
        }

        //---------------------------------------------------------------------
        // TransferFiles.
        //---------------------------------------------------------------------

        [Test]
        public void TransferFiles_WhenApplicable()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedSession.SetupGet(s => s.CanTransferFiles).Returns(true);

            Assert.That(
                sessionCommands.TransferFiles.QueryState(connectedSession.Object), Is.EqualTo(CommandState.Enabled));
        }

        [Test]
        public void TransferFiles_WhenNotConnected()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var closedSession = new Mock<ISession>();
            closedSession.SetupGet(s => s.IsConnected).Returns(false);

            var rdpSession = new Mock<IRdpSession>();
            rdpSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.TransferFiles.QueryState(closedSession.Object), Is.EqualTo(CommandState.Disabled));
            Assert.That(
                sessionCommands.TransferFiles.QueryState(rdpSession.Object), Is.EqualTo(CommandState.Disabled));
        }

        [Test]
        public void TransferFiles_WhenFileTransferNotSupported()
        {
            var sessionCommands = new SessionCommands(
                new Mock<ISessionBroker>().Object);

            var connectedSession = new Mock<ISession>();
            connectedSession.SetupGet(s => s.IsConnected).Returns(true);
            connectedSession.SetupGet(s => s.CanTransferFiles).Returns(false);

            var rdpSession = new Mock<IRdpSession>();
            rdpSession.SetupGet(s => s.IsConnected).Returns(true);

            Assert.That(
                sessionCommands.TransferFiles.QueryState(connectedSession.Object), Is.EqualTo(CommandState.Disabled));
            Assert.That(
                sessionCommands.TransferFiles.QueryState(rdpSession.Object), Is.EqualTo(CommandState.Disabled));
        }
    }
}
