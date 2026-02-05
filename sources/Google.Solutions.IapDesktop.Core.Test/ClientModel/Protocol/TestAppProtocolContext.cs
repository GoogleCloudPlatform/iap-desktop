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
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies;
using Google.Solutions.Platform.Dispatch;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestAppProtocolContext
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private static AppProtocol CreateProtocol(IAppProtocolClient? client)
        {
            return new AppProtocol(
                "Test",
                Enumerable.Empty<ITrait>(),
                8080,
                null,
                client);
        }

        //---------------------------------------------------------------------
        // CreateTransportPolicy.
        //---------------------------------------------------------------------

        [Test]
        public void CreateTransportPolicy_WhenProtocolHasNoClient()
        {
            var context = new AppProtocolContext(
                CreateProtocol(null),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                SampleLocator);

            Assert.IsInstanceOf<CurrentWtsSessionPolicy>(context.CreateTransportPolicy());
        }

        [Test]
        public void CreateTransportPolicy_WhenFactoryCannotTrackChildren()
        {
            var context = new AppProtocolContext(
                CreateProtocol(new Mock<IAppProtocolClient>().Object),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                SampleLocator);

            Assert.IsInstanceOf<CurrentWtsSessionPolicy>(context.CreateTransportPolicy());
        }

        [Test]
        public void CreateTransportPolicy_WhenFactoryCanTrackChildren()
        {
            var factory = (IWin32ProcessFactory)new Mock<IWin32ProcessFactory>()
                .As<IWin32ProcessSet>()
                .Object;

            var context = new AppProtocolContext(
                CreateProtocol(new Mock<IAppProtocolClient>().Object),
                new Mock<IIapTransportFactory>().Object,
                factory,
                SampleLocator);

            Assert.IsInstanceOf<ChildProcessPolicy>(context.CreateTransportPolicy());
        }

        //---------------------------------------------------------------------
        // CanLaunchClient.
        //---------------------------------------------------------------------

        [Test]
        public void CanLaunchClient_WhenClientIsNull()
        {
            var context = new AppProtocolContext(
                CreateProtocol(null),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                SampleLocator);

            Assert.That(context.CanLaunchClient, Is.False);
        }

        [Test]
        public void CanLaunchClient_WhenClientIsNotAvailable()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(false);

            var context = new AppProtocolContext(
                CreateProtocol(client.Object),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                SampleLocator);

            Assert.That(context.CanLaunchClient, Is.False);
        }

        [Test]
        public void CanLaunchClient_WhenClientIsAvailable()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);

            var context = new AppProtocolContext(
                CreateProtocol(client.Object),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                SampleLocator);

            Assert.That(context.CanLaunchClient, Is.True);
        }

        //---------------------------------------------------------------------
        // LaunchClient.
        //---------------------------------------------------------------------

        [Test]
        public void LaunchClient_WhenClientCannotBeLaunched()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(false);
            client.SetupGet(c => c.Executable).Returns("client.exe");

            var protocol = CreateProtocol(client.Object);
            var transport = new Mock<ITransport>();

            var context = new AppProtocolContext(
                protocol,
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                SampleLocator);

            Assert.Throws<InvalidOperationException>(
                () => context.LaunchClient(transport.Object));
        }

        [Test]
        public void LaunchClient_WhenCredentialIsNull()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.Executable).Returns("client.exe");
            client
                .Setup(c => c.FormatArguments(
                    It.IsAny<ITransport>(),
                    It.IsAny<AppProtocolParameters>()))
                .Returns("args");

            var processFactory = new Mock<IWin32ProcessFactory>();
            processFactory
                .Setup(f => f.CreateProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Mock<IWin32Process>().Object);

            var protocol = CreateProtocol(client.Object);
            var transport = new Mock<ITransport>();

            var context = new AppProtocolContext(
                protocol,
                new Mock<IIapTransportFactory>().Object,
                processFactory.Object,
                SampleLocator);

            using (var process = context.LaunchClient(transport.Object))
            {
                Assert.That(process, Is.Not.Null);

                processFactory.Verify(
                    f => f.CreateProcess("client.exe", "args"),
                    Times.Once);
            }
        }

        [Test]
        public void LaunchClient_WhenCredentialSet()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);
            client.SetupGet(c => c.Executable).Returns("client.exe");
            client
                .Setup(c => c.FormatArguments(
                    It.IsAny<ITransport>(),
                    It.IsAny<AppProtocolParameters>()))
                .Returns("args");

            var processFactory = new Mock<IWin32ProcessFactory>();
            processFactory
                .Setup(f => f.CreateProcessAsUser(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<LogonFlags>(),
                    It.IsAny<NetworkCredential>()))
                .Returns(new Mock<IWin32Process>().Object);

            var protocol = CreateProtocol(client.Object);
            var transport = new Mock<ITransport>();

            using (var context = new AppProtocolContext(
                protocol,
                new Mock<IIapTransportFactory>().Object,
                processFactory.Object,
                SampleLocator)
            {
                NetworkCredential = new NetworkCredential("user", "password", "domain")
            })
            using (var process = context.LaunchClient(transport.Object))
            {
                Assert.That(process, Is.Not.Null);

                processFactory.Verify(
                    f => f.CreateProcessAsUser(
                        "client.exe",
                        "args",
                        LogonFlags.NetCredentialsOnly,
                        context.NetworkCredential),
                    Times.Once);
            }
        }

        //---------------------------------------------------------------------
        // ConnectTransport.
        //---------------------------------------------------------------------

        [Test]
        public async Task ConnectTransport()
        {
            var client = new Mock<IAppProtocolClient>();
            var protocol = CreateProtocol(client.Object);

            var transportFactory = new Mock<IIapTransportFactory>();

            using (var context = new AppProtocolContext(
                protocol,
                transportFactory.Object,
                new Mock<IWin32ProcessFactory>().Object,
                SampleLocator))
            {
                var transport = await context.ConnectTransportAsync(CancellationToken.None);

                transportFactory.Verify(
                    t => t.CreateTransportAsync(
                        protocol,
                        It.IsAny<ITransportPolicy>(),
                        SampleLocator,
                        protocol.RemotePort,
                        protocol.LocalEndpoint,
                        context.Parameters.ConnectionTimeout,
                        CancellationToken.None),
                    Times.Once);
            }
        }
    }
}
