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


using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Theme;
using Google.Solutions.Terminal.Controls;
using Google.Solutions.Testing.Apis.Platform;
using Moq;
using NUnit.Framework;
using System;
using System.Management;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Session
{
    [TestFixture]
    public class TestSessionViewBase2
    {
        private class SampleClient : ClientBase
        {
            public override void Connect()
            {
                OnAfterConnect();
            }

            public void Login()
            {
                OnAfterLogin();
            }

            public override void SendText(string text)
            {
            }

            public void FailConnection(Exception e)
            {
                OnConnectionFailed(e);
            }

            public void CloseConnection(RdpClient.DisconnectReason reason)
            {
                OnConnectionClosed(reason);
            }
        }

        private class SampleSessionView : SessionViewBase2<SampleClient>
        {
            public SampleSessionView(
                IEventQueue eventQueue,
                IExceptionDialog exceptionDialog) 
                : base(
                    new Mock<IMainWindow>().Object, 
                    new ToolWindowStateRepository(
                        RegistryKeyPath.ForCurrentTest().CreateKey()),
                    eventQueue, 
                    exceptionDialog,
                    new Mock<IControlTheme>().Object,
                    new Mock<IBindingContext>().Object)
            {
            }

            public override InstanceLocator Instance 
                => new InstanceLocator("project-1", "zone-1", "instance-1");

            public new SampleClient Client => base.Client!;

            protected override void ConnectCore()
            {
                this.Client!.Connect();
            }
        }

        //----------------------------------------------------------------------
        // IsConnected.
        //----------------------------------------------------------------------

        [Test]
        public void IsConnected()
        {
            var view = new SampleSessionView(
                new Mock<IEventQueue>().Object,
                new Mock<IExceptionDialog>().Object);

            Assert.IsFalse(view.IsConnected);

            view.Connect();
            Assert.IsTrue(view.IsConnected);

            view.Client.Login();
            Assert.IsTrue(view.IsConnected);

            view.Client.FailConnection(new Exception());
            Assert.IsFalse(view.IsConnected);
        }

        //----------------------------------------------------------------------
        // Client events.
        //----------------------------------------------------------------------

        [Test]
        public void Client_WhenConnected_ThenPublishesEvent()
        {
            var eventQueue = new Mock<IEventQueue>();

            var view = new SampleSessionView(
                eventQueue.Object,
                new Mock<IExceptionDialog>().Object);
            view.Connect();
            view.Client.Login();

            eventQueue.Verify(
                q => q.PublishAsync(It.IsAny<SessionStartedEvent>()),
                Times.Once);
        }

        [Test]
        public void Client_WhenConnectionFailed_ThenPublishesEvent()
        {
            var eventQueue = new Mock<IEventQueue>();

            var view = new SampleSessionView(
                eventQueue.Object,
                new Mock<IExceptionDialog>().Object);
            view.Connect();
            view.Client.FailConnection(new Exception());

            eventQueue.Verify(
                q => q.PublishAsync(It.IsAny<SessionAbortedEvent>()),
                Times.Once);
        }

        [Test]
        public void Client_WhenReconnectInitiatedOrFormClosed(
            [Values(
            ClientBase.DisconnectReason.ReconnectInitiatedByUser,
            ClientBase.DisconnectReason.FormClosed)]
            ClientBase.DisconnectReason reason)
        {
            var eventQueue = new Mock<IEventQueue>();

            var view = new SampleSessionView(
                eventQueue.Object,
                new Mock<IExceptionDialog>().Object);
            view.Connect();
            view.Client.CloseConnection(reason);

            Assert.IsFalse(view.IsClosing);

            eventQueue.Verify(
                q => q.PublishAsync(It.IsAny<SessionEndedEvent>()),
                Times.Never);
            eventQueue.Verify(
                q => q.PublishAsync(It.IsAny<SessionAbortedEvent>()),
                Times.Never);
        }

        [Test]
        public void Client_WhenConnectionFailed()
        {
            var eventQueue = new Mock<IEventQueue>();
            var dialog = new Mock<IExceptionDialog>();

            var view = new SampleSessionView(
                eventQueue.Object,
                dialog.Object);
            view.Connect();

            var exception = new Exception();
            view.Client.FailConnection(exception);

            dialog.Verify(
                d => d.Show(view, It.IsAny<string>(), exception),
                Times.Once);
            eventQueue.Verify(
                q => q.PublishAsync(It.IsAny<SessionEndedEvent>()),
                Times.Never);
            eventQueue.Verify(
                q => q.PublishAsync(It.IsAny<SessionAbortedEvent>()),
                Times.Once);
        }
    }
}
