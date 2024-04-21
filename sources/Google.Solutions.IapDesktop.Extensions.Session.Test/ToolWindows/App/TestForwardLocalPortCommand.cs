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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App;
using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.App
{
    [TestFixture]
    public class TestForwardLocalPortCommand
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // Id.
        //---------------------------------------------------------------------

        [Test]
        public void Id()
        {
            var command = new ForwardLocalPortCommand(
                new Mock<IWin32Window>().Object,
                "&test",
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                new Mock<IJobService>().Object,
                 new Mock<IInputDialog>().Object,
                new Mock<INotifyDialog>().Object);

            Assert.AreEqual(command.GetType().Name, command.Id);
        }

        //---------------------------------------------------------------------
        // CreateContext.
        //---------------------------------------------------------------------

        [Test]
        public void WhenInputDialogCancelled_ThenCreateContextThrowsException()
        {
            string? input = null;
            var inputDialog = new Mock<IInputDialog>();
            inputDialog.Setup(d => d.Prompt(
                It.IsAny<IWin32Window>(),
                It.IsAny<InputDialogParameters>(),
                out input))
                .Returns(DialogResult.Cancel);

            var command = new ForwardLocalPortCommand(
                new Mock<IWin32Window>().Object,
                "&test",
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                new Mock<IJobService>().Object,
                inputDialog.Object,
                new Mock<INotifyDialog>().Object);

            var node = new Mock<IProjectModelInstanceNode>();

            ExceptionAssert.ThrowsAggregateException<TaskCanceledException>(
                () => command.CreateContextAsync(
                    node.Object,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenPortProvided_ThenCreateContextReturnsContext()
        {
            var input = "8080";
            var inputDialog = new Mock<IInputDialog>();
            inputDialog.Setup(d => d.Prompt(
                It.IsAny<IWin32Window>(),
                It.IsAny<InputDialogParameters>(),
                out input))
                .Returns(DialogResult.OK);

            var command = new ForwardLocalPortCommand(
                new Mock<IWin32Window>().Object,
                "&test",
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                new Mock<IJobService>().Object,
                inputDialog.Object,
                new Mock<INotifyDialog>().Object);

            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(SampleInstance);

            var context = (AppProtocolContext)await command
                .CreateContextAsync(
                    node.Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(context.NetworkCredential);
            Assert.IsFalse(context.CanLaunchClient);
            Assert.IsInstanceOf<CurrentWtsSessionPolicy>(context.CreateTransportPolicy());
        }

        //---------------------------------------------------------------------
        // Execute.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPortProvided_ThenExecuteShowsBalloon()
        {
            var input = "8080";
            var inputDialog = new Mock<IInputDialog>();
            inputDialog.Setup(d => d.Prompt(
                It.IsAny<IWin32Window>(),
                It.IsAny<InputDialogParameters>(),
                out input))
                .Returns(DialogResult.OK);

            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.Target).Returns(SampleInstance);
            transport.SetupGet(t => t.Endpoint).Returns(new IPEndPoint(IPAddress.Loopback, 1));

            var transportFactory = new Mock<IIapTransportFactory>();
            transportFactory
                .Setup(t => t.CreateTransportAsync(
                    It.IsAny<IProtocol>(),
                    It.IsAny<ITransportPolicy>(),
                    SampleInstance,
                    8080,
                    null,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(transport.Object);

            var notifyDialog = new Mock<INotifyDialog>();

            var command = new ForwardLocalPortCommand(
                new Mock<IWin32Window>().Object,
                "&test",
                transportFactory.Object,
                new Mock<IWin32ProcessFactory>().Object,
                new SynchronousJobService(),
                inputDialog.Object,
                notifyDialog.Object);

            var node = new Mock<IProjectModelInstanceNode>();
            node.SetupGet(n => n.Instance).Returns(SampleInstance);

            await command
                .ExecuteAsync(node.Object)
                .ConfigureAwait(false);

            notifyDialog.Verify(
                d => d.ShowBalloon(It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }
    }
}
