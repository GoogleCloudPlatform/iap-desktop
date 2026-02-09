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
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Moq;
using NUnit.Framework;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Session
{
    [TestFixture]
    public class TestConnectRdpUrlCommand
    {
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private static readonly IapRdpUrl SampleUrl = new IapRdpUrl(
            SampleLocator,
            new NameValueCollection());

        //---------------------------------------------------------------------
        // Execute.
        //---------------------------------------------------------------------

        [Test]
        public async Task Execute_WhenSessionFound()
        {
            var sessionBroker = new Mock<ISessionBroker>();
            var session = (ISession)new Mock<IRdpSession>().Object;
            sessionBroker
                .Setup(s => s.TryActivateSession(SampleLocator, out session))
                .Returns(true);

            var contextFactory = new Mock<ISessionContextFactory>();

            var command = new ConnectRdpUrlCommand(
                contextFactory.Object,
                new Mock<ISessionFactory>().Object,
                sessionBroker.Object);

            var url = new IapRdpUrl(SampleLocator, new NameValueCollection());
            await command
                .ExecuteAsync(url)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateRdpSessionContextAsync(It.IsAny<IapRdpUrl>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task Execute_WhenNoSessionFound()
        {
            var context = new Mock<ISessionContext<RdpCredential, RdpParameters>>();
            var contextFactory = new Mock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateRdpSessionContextAsync(SampleUrl, CancellationToken.None))
                .ReturnsAsync(context.Object);

            var sessionFactory = new Mock<ISessionFactory>();
            sessionFactory
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);

            var sessionBroker = new Mock<ISessionBroker>();
            ISession? nullSession;
            sessionBroker
                .Setup(s => s.TryActivateSession(SampleLocator, out nullSession))
                .Returns(false);

            var command = new ConnectRdpUrlCommand(
                contextFactory.Object,
                sessionFactory.Object,
                sessionBroker.Object);

            await command
                .ExecuteAsync(SampleUrl)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateRdpSessionContextAsync(SampleUrl, CancellationToken.None),
                Times.Once);
        }
    }
}
