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


using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.SqlServer;
using Moq;
using NUnit.Framework;
using System.Net;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.SqlServer
{
    [TestFixture]
    public class TestSsmsClient
    {
        //---------------------------------------------------------------------
        // FormatArguments.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNetworkCredentialTypeIsDefault_ThenFormatArgumentsReturnsString()
        {
            var transport = new Mock<ITransport>();
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 11443));
            var client = new SsmsClient(NetworkCredentialType.Default);

            Assert.AreEqual(
                "-S 127.0.0.2,11443",
                client.FormatArguments(transport.Object));
        }

        [Test]
        public void WhenNetworkCredentialTypeIsWindows_ThenFormatArgumentsReturnsString(
            [Values(
                NetworkCredentialType.Rdp, 
                NetworkCredentialType.Prompt)] NetworkCredentialType type)
        {
            var transport = new Mock<ITransport>();
            transport
                .SetupGet(t => t.Endpoint)
                .Returns(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 11443));
            var client = new SsmsClient(type);

            Assert.AreEqual(
                "-S 127.0.0.2,11443 -E",
                client.FormatArguments(transport.Object));
        }
    }
}
