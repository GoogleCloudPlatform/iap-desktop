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
        public void WhenExecutableNotFound_ThenIsAvailableReturnsFalse()
        {
            Assert.IsFalse(new AppProtocolClient("x:\\doesnotexist.exe", null).IsAvailable);
            Assert.IsFalse(new AppProtocolClient("NUL.exe", null).IsAvailable);
        }

        [Test]
        public void WhenExecutableExists_ThenIsAvailableReturnsTrue()
        {
            Assert.IsTrue(new AppProtocolClient(CmdExe, null).IsAvailable);
        }

        //---------------------------------------------------------------------
        // FormatArguments.
        //---------------------------------------------------------------------

        [Test]
        public void WhenArgumentsNull_ThenFormatArgumentsReturnsNull()
        {
            var transport = new Mock<ITransport>();
            var client = new AppProtocolClient("doesnotexist.exe", null);

            Assert.IsNull(client.FormatArguments(
                transport.Object,
                new AppProtocolParameters()));
        }

        [Test]
        public void WhenArgumentsContainsPlaceholdersButParametersAreEmpty_ThenFormatArgumentsResolvesPlaceholders()
        {
            var transport = new Mock<ITransport>();
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 8080));

            var client = new AppProtocolClient(
                "doesnotexist.exe",
                "/port {port} /host {host} /ignore {HOST}{Port}} {foo}}} /user {username}}");

            var parameters = new AppProtocolParameters();

            Assert.AreEqual(
                "/port 8080 /host 127.0.0.2 /ignore {HOST}{Port}} {foo}}} /user }",
                client.FormatArguments(transport.Object, parameters));
        }

        [Test]
        public void WhenArgumentsContainsPlaceholdersAndParametersSet_ThenFormatArgumentsResolvesPlaceholders()
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

            Assert.AreEqual(
                "/port 8080 /host 127.0.0.2 /ignore {HOST}{Port}} {foo}}} /user root}",
                client.FormatArguments(transport.Object, parameters));
        }

        [Test]
        public void WhenUsernameInvalid_ThenFormatArgumentsThrowsException(
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
        public void WhenArgumentsDoNotUseUsername_ThenIsUsernameRequiredReturnsFalse(
            [Values(null, " ", "{U}")] string arguments)
        {
            Assert.IsFalse(new AppProtocolClient("NUL.exe", arguments).IsUsernameRequired);
        }

        [Test]
        public void WhenArgumentsUseUsername_ThenIsUsernameRequiredReturnsTrue()
        {
            Assert.IsTrue(new AppProtocolClient("NUL.exe", "/u '{username}'").IsUsernameRequired);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToStringReturnsExecutableAndArguments()
        {
            Assert.AreEqual(
                "cmd.exe",
                new AppProtocolClient("cmd.exe", null).ToString());
            Assert.AreEqual(
                "cmd.exe args",
                new AppProtocolClient("cmd.exe", "args").ToString());
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOtherHasDifferentExecutable_ThenEqualsReturnsFalse()
        {
            var app1 = new AppProtocolClient("cmd1.exe", "args");
            var app2 = new AppProtocolClient("cmd2.exe", "args");

            Assert.IsFalse(app1.Equals(app2));
            Assert.IsTrue(app1 != app2);
            Assert.AreNotEqual(app1.GetHashCode(), app2.GetHashCode());
        }

        [Test]
        public void WhenOtherHasDifferentArguments_ThenEqualsReturnsFalse()
        {
            var app1 = new AppProtocolClient("cmd.exe", "args");
            var app2 = new AppProtocolClient("cmd.exe", null);

            Assert.IsFalse(app1.Equals(app2));
            Assert.IsTrue(app1 != app2);
            Assert.AreNotEqual(app1.GetHashCode(), app2.GetHashCode());
        }
    }
}
