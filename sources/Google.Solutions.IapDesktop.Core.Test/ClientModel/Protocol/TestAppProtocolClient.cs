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

using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Moq;
using NUnit.Framework;
using System;
using System.Net;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestAppProtocolClient
    {
        private static readonly string CmdExe
            = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\cmd.exe";

        //---------------------------------------------------------------------
        // IsAvailable.
        //---------------------------------------------------------------------

        [Test]
        public void IsAvailable_WhenExecutableNotFound()
        {
            Assert.That(new AppProtocolClient("x:\\doesnotexist.exe", null).IsAvailable, Is.False);
            Assert.That(new AppProtocolClient("NUL.exe", null).IsAvailable, Is.False);
        }

        [Test]
        public void IsAvailable_WhenExecutableExists()
        {
            Assert.That(new AppProtocolClient(CmdExe, null).IsAvailable, Is.True);
        }

        //---------------------------------------------------------------------
        // FormatArguments.
        //---------------------------------------------------------------------

        [Test]
        public void FormatArguments_WhenArgumentsNull()
        {
            var transport = new Mock<ITransport>();
            var client = new AppProtocolClient("doesnotexist.exe", null);

            Assert.IsNull(client.FormatArguments(
                transport.Object,
                new AppProtocolParameters()));
        }

        [Test]
        public void FormatArguments_WhenArgumentsContainsPlaceholdersButParametersAreEmpty()
        {
            var transport = new Mock<ITransport>();
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 8080));

            var client = new AppProtocolClient(
                "doesnotexist.exe",
                "/port {port} /host {host} /ignore {HOST}{Port}} {foo}}} /user {username}}");

            var parameters = new AppProtocolParameters();

            Assert.That(
                client.FormatArguments(transport.Object, parameters), Is.EqualTo("/port 8080 /host 127.0.0.2 /ignore {HOST}{Port}} {foo}}} /user }"));
        }

        [Test]
        public void FormatArguments_WhenArgumentsContainsPlaceholdersAndParametersSet()
        {
            var transport = new Mock<ITransport>();
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 8080));

            var client = new AppProtocolClient(
                "doesnotexist.exe",
                "/port {port} /host {host} /ignore {HOST}{Port}} {foo}}} /user {username}}");

            var parameters = new AppProtocolParameters()
            {
                PreferredUsername = "root",
            };

            Assert.That(
                client.FormatArguments(transport.Object, parameters), Is.EqualTo("/port 8080 /host 127.0.0.2 /ignore {HOST}{Port}} {foo}}} /user root}"));
        }

        [Test]
        public void FormatArguments_WhenUsernameInvalid(
            [Values("user\"", "''")] string username)
        {
            var transport = new Mock<ITransport>();
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 8080));

            var client = new AppProtocolClient(
                "doesnotexist.exe",
                "/port {port} /host {host} /ignore {HOST}{Port}} {foo}}} /user {username}}");

            var parameters = new AppProtocolParameters()
            {
                PreferredUsername = username,
            };

            Assert.Throws<ArgumentException>(() =>
                client.FormatArguments(transport.Object, parameters));
        }

        //---------------------------------------------------------------------
        // IsUsernameRequired.
        //---------------------------------------------------------------------

        [Test]
        public void IsUsernameRequired_WhenArgumentsDoNotUseUsername(
            [Values(null, " ", "{U}")] string? arguments)
        {
            Assert.That(new AppProtocolClient("NUL.exe", arguments).IsUsernameRequired, Is.False);
        }

        [Test]
        public void IsUsernameRequiredWhenArgumentsUseUsername()
        {
            Assert.That(new AppProtocolClient("NUL.exe", "/u '{username}'").IsUsernameRequired, Is.True);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_ReturnsExecutableAndArguments()
        {
            Assert.That(
                new AppProtocolClient("cmd.exe", null).ToString(), Is.EqualTo("cmd.exe"));
            Assert.That(
                new AppProtocolClient("cmd.exe", "args").ToString(), Is.EqualTo("cmd.exe args"));
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void Equals_WhenOtherHasDifferentExecutable()
        {
            var app1 = new AppProtocolClient("cmd1.exe", "args");
            var app2 = new AppProtocolClient("cmd2.exe", "args");

            Assert.That(app1.Equals(app2), Is.False);
            Assert.That(app1 != app2, Is.True);
            Assert.That(app2.GetHashCode(), Is.Not.EqualTo(app1.GetHashCode()));
        }

        [Test]
        public void Equals_WhenOtherHasDifferentArguments()
        {
            var app1 = new AppProtocolClient("cmd.exe", "args");
            var app2 = new AppProtocolClient("cmd.exe", null);

            Assert.That(app1.Equals(app2), Is.False);
            Assert.That(app1 != app2, Is.True);
            Assert.That(app2.GetHashCode(), Is.Not.EqualTo(app1.GetHashCode()));
        }
    }
}
