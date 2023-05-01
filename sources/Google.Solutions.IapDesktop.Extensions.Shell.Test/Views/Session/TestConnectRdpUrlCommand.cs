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
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Session;
using Google.Solutions.Testing.Common.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Session
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
        // ExecuteAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSessionFound_ThenExecuteDoesNotCreateNewSession()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var contextFactory = serviceProvider.AddMock<ISessionContextFactory>();
            var sessionBroker = serviceProvider.AddMock<IInstanceSessionBroker>();

            var session = (ISession)new Mock<IRemoteDesktopSession>().Object;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out session))
                .Returns(true);

            var command = new ConnectRdpUrlCommand(
                new Service<ISessionContextFactory>(serviceProvider.Object),
                new Service<IInstanceSessionBroker>(serviceProvider.Object));

            var url = new IapRdpUrl(SampleLocator, new NameValueCollection());
            await command
                .ExecuteAsync(url)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateRdpSessionContextAsync(It.IsAny<IapRdpUrl>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task WhenNoSessionFound_ThenExecuteCreatesNewSession()
        {
            var serviceProvider = new Mock<IServiceProvider>();

            var context = new Mock<ISessionContext<RdpCredential, RdpSessionParameters>>();
            var contextFactory = serviceProvider.AddMock<ISessionContextFactory>();
            contextFactory
                .Setup(s => s.CreateRdpSessionContextAsync(SampleUrl, CancellationToken.None))
                .ReturnsAsync(context.Object);

            ISession nullSession;
            var rdpSessionBroker = serviceProvider.AddMock<IInstanceSessionBroker>();
            rdpSessionBroker
                .Setup(s => s.CreateSessionAsync(context.Object))
                .ReturnsAsync(new Mock<ISession>().Object);
            rdpSessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out nullSession))
                .Returns(false);

            var command = new ConnectRdpUrlCommand(
                new Service<ISessionContextFactory>(serviceProvider.Object),
                new Service<IInstanceSessionBroker>(serviceProvider.Object));

            await command
                .ExecuteAsync(SampleUrl)
                .ConfigureAwait(false);

            contextFactory.Verify(
                s => s.CreateRdpSessionContextAsync(SampleUrl, CancellationToken.None),
                Times.Once);
        }
    }
}
