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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Rdp;
using Google.Solutions.IapDesktop.Extensions.Shell.Views;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Testing.Common.Mocks;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views
{
    [TestFixture]
    public class TestConnectCommands
    {
        //---------------------------------------------------------------------
        // LaunchRdpUrl.
        //---------------------------------------------------------------------

        [Test]
        public void LaunchRdpUrlIsEnabled()
        {
            var urlCommands = new UrlCommands();
            var sessionCommands = new ConnectCommands(
                urlCommands,
                new Service<IRdpConnectionService>(new Mock<IServiceProvider>().Object));

            var url = new IapRdpUrl(
                new InstanceLocator("project", "zone", "name"),
                new NameValueCollection());

            Assert.AreEqual(
                CommandState.Enabled,
                urlCommands.LaunchRdpUrl.QueryState(url));
        }

        [Test]
        public async Task LaunchRdpUrlCommandActivatesInstance()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var connectionService = serviceProvider.AddMock<IRdpConnectionService>();

            var urlCommands = new UrlCommands();
            var sessionCommands = new ConnectCommands(
                urlCommands,
                new Service<IRdpConnectionService>(serviceProvider.Object));

            var url = new IapRdpUrl(
                new InstanceLocator("project", "zone", "name"),
                new NameValueCollection());

            await urlCommands.LaunchRdpUrl
                .ExecuteAsync(url)
                .ConfigureAwait(false);

            connectionService.Verify(
                s => s.ActivateOrConnectInstanceAsync(url),
                Times.Once);
        }
    }
}
