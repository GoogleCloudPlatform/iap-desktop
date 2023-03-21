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

using Moq;
using NUnit.Framework;
using System;
using Google.Solutions.Testing.Common.Mocks;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Session;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.RemoteDesktop;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.Session
{
    [TestFixture]
    public class TestConnectCommands_LaunchUrl
    {
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // ExecuteAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSessionFound_ThenExecuteDoesNotCreateNewSession()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var connectionService = serviceProvider.AddMock<IRdpConnectionService>();
            var sessionBroker = serviceProvider.AddMock<IGlobalSessionBroker>();

            var session = (ISession)new Mock<IRemoteDesktopSession>().Object;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out session))
                .Returns(true);

            var command = new ConnectCommands.LaunchRdpUrlCommand(
                new Service<IRdpConnectionService>(serviceProvider.Object),
                new Service<IGlobalSessionBroker>(serviceProvider.Object));

            var url = new IapRdpUrl(SampleLocator, new NameValueCollection());
            await command
                .ExecuteAsync(url)
                .ConfigureAwait(false);

            connectionService.Verify(
                s => s.ConnectInstanceAsync(It.IsAny<IapRdpUrl>()),
                Times.Never);
        }

        [Test]
        public async Task WhenNoSessionFound_ThenExecuteCreatesNewSession()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var connectionService = serviceProvider.AddMock<IRdpConnectionService>();
            var sessionBroker = serviceProvider.AddMock<IGlobalSessionBroker>();

            ISession nullSession;
            sessionBroker
                .Setup(s => s.TryActivate(SampleLocator, out nullSession))
                .Returns(false);

            var command = new ConnectCommands.LaunchRdpUrlCommand(
                new Service<IRdpConnectionService>(serviceProvider.Object),
                new Service<IGlobalSessionBroker>(serviceProvider.Object));

            var url = new IapRdpUrl(SampleLocator, new NameValueCollection());
            await command
                .ExecuteAsync(url)
                .ConfigureAwait(false);

            connectionService.Verify(
                s => s.ConnectInstanceAsync(url),
                Times.Once);
        }
    }
}
