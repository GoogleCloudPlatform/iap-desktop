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
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        [Test]
        public void Executable_WhenSsmsNotAvailable()
        {
            var client = new SsmsClient(null);
            string path;
            Assert.Throws<InvalidOperationException>(
                () => path = client.Executable);
        }

        [Test]
        public void IsAvailable_WhenSsmsNotAvailable()
        {
            var client = new SsmsClient(null);
            Assert.IsFalse(client.IsAvailable);
        }

        //---------------------------------------------------------------------
        // FormatArguments.
        //---------------------------------------------------------------------

        [Test]
        public void FormatArguments_WhenNlaDisabledAndUsernameEmpty(
            [Values("", " ", null)] string emptyish)
        {
            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.Target).Returns(SampleInstance);
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 11443));
            var client = new SsmsClient();

            var parameters = new AppProtocolParameters()
            {
                PreferredUsername = emptyish,
                NetworkLevelAuthentication = AppNetworkLevelAuthenticationState.Disabled
            };

            Assert.That(
                client.FormatArguments(transport.Object, parameters), Is.EqualTo("-S 127.0.0.2\\instance-1.zone-1.c.project-1.internal,11443 -U sa"));
        }

        [Test]
        public void FormatArguments_WhenNlaDisabledAndUsernameSet()
        {
            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.Target).Returns(SampleInstance);
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 11443));
            var client = new SsmsClient();

            var parameters = new AppProtocolParameters()
            {
                PreferredUsername = "username",
                NetworkLevelAuthentication = AppNetworkLevelAuthenticationState.Disabled
            };

            Assert.That(
                client.FormatArguments(transport.Object, parameters), Is.EqualTo("-S 127.0.0.2\\instance-1.zone-1.c.project-1.internal,11443 -U \"username\""));
        }

        [Test]
        public void FormatArguments_WhenNlaDisabledAndUsernameInvalid(
            [Values("user\"", "''")] string username)
        {
            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.Target).Returns(SampleInstance);
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
        public void FormatArguments_WhenNlaEnabled()
        {
            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.Target).Returns(SampleInstance);
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 11443));
            var client = new SsmsClient();

            var parameters = new AppProtocolParameters()
            {
                NetworkLevelAuthentication = AppNetworkLevelAuthenticationState.Enabled
            };

            Assert.That(
                client.FormatArguments(transport.Object, parameters), Is.EqualTo("-S 127.0.0.2\\instance-1.zone-1.c.project-1.internal,11443 -E"));
        }
    }
}
