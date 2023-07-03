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


using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Moq;
using NUnit.Framework;
using System;
using System.Net;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.App
{
    [TestFixture]
    public class TestSsmsClient
    {
        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSsmsNotAvailable_ThenExecutableIsNull()
        {
            var client = new SsmsClient(null);
            Assert.IsNull(client.Executable);
        }

        [Test]
        public void WhenSsmsNotAvailable_ThenIsAvailableIsNull()
        {
            var client = new SsmsClient(null);
            Assert.IsFalse(client.IsAvailable);
        }

        //---------------------------------------------------------------------
        // FormatArguments.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNlaDisabledAndUsernameEmpty_ThenFormatArgumentsReturnsStringForSqlAuth(
            [Values("", " ", null)] string emptyish)
        {
            var transport = new Mock<ITransport>();
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 11443));
            var client = new SsmsClient();

            var parameters = new AppProtocolParameters()
            {
                PreferredUsername = emptyish,
                NetworkLevelAuthentication = AppNetworkLevelAuthenticationState.Disabled
            };

            Assert.AreEqual(
                "-S 127.0.0.2,11443 -U sa",
                client.FormatArguments(transport.Object, parameters));
        }

        [Test]
        public void WhenNlaDisabledAndUsernameSet_ThenFormatArgumentsReturnsStringForSqlAuth()
        {
            var transport = new Mock<ITransport>();
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 11443));
            var client = new SsmsClient();

            var parameters = new AppProtocolParameters()
            {
                PreferredUsername = "username",
                NetworkLevelAuthentication = AppNetworkLevelAuthenticationState.Disabled
            };

            Assert.AreEqual(
                "-S 127.0.0.2,11443 -U \"username\"",
                client.FormatArguments(transport.Object, parameters));
        }

        [Test]
        public void WhenNlaDisabledAndUsernameInvalid_ThenFormatArgumentsThrowsException(
            [Values("user\"", "''")] string username)
        {
            var transport = new Mock<ITransport>();
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 11443));
            var client = new SsmsClient();

            var parameters = new AppProtocolParameters()
            {
                PreferredUsername = username,
                NetworkLevelAuthentication = AppNetworkLevelAuthenticationState.Disabled
            };

            Assert.Throws<ArgumentException>(() =>
                client.FormatArguments(transport.Object, parameters));
        }

        [Test]
        public void WhenNlaEnabled_ThenFormatArgumentsReturnsStringForWindowsAuth()
        {
            var transport = new Mock<ITransport>();
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 11443));
            var client = new SsmsClient();

            var parameters = new AppProtocolParameters()
            {
                NetworkLevelAuthentication = AppNetworkLevelAuthenticationState.Enabled
            };

            Assert.AreEqual(
                "-S 127.0.0.2,11443 -E",
                client.FormatArguments(transport.Object, parameters));
        }
    }
}
