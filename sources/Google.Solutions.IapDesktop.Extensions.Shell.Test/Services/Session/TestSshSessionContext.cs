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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Ssh.Auth;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Session
{
    [TestFixture]
    public class TestSshSessionContext
    {
        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // AuthorizeCredential.
        //---------------------------------------------------------------------

        [Test]
        public void AuthorizeCredentialReturnsCredential()
        {
            var authorizedKey = AuthorizedKeyPair.ForMetadata(
                new Mock<ISshKeyPair>().Object,
                "username",
                false,
                new Mock<IAuthorization>().Object);

            var key = new Mock<ISshKeyPair>().Object;
            var keyAuthService = new Mock<IKeyAuthorizationService>();
            keyAuthService
                .Setup(s => s.AuthorizeKeyAsync(
                    SampleInstance,
                    key,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<string>(),
                    KeyAuthorizationMethods.All,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(authorizedKey);

            var context = new SshSessionContext(
                new Mock<IIapTransportFactory>().Object,
                new Mock<IDirectTransportFactory>().Object,
                keyAuthService.Object,
                new Mock<IAddressResolver>().Object,
                SampleInstance,
                key);

            Assert.AreSame(
                authorizedKey,
                context.AuthorizeCredentialAsync(CancellationToken.None)
                    .Result
                    .Key);
        }

        //---------------------------------------------------------------------
        // ConnectTransport.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenTransportTypeIsIap_ThenConnectTransportCreatesIapTransport()
        {
            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.Endpoint).Returns(new IPEndPoint(IPAddress.Loopback, 123));

            var factory = new Mock<IIapTransportFactory>();
            factory
                .Setup(b => b.CreateTransportAsync(
                    SshProtocol.Protocol,
                    It.IsAny<ITransportPolicy>(),
                    SampleInstance,
                    22,
                    It.IsAny<IPEndPoint>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(transport.Object);

            var context = new SshSessionContext(
                factory.Object,
                new Mock<IDirectTransportFactory>().Object,
                new Mock<IKeyAuthorizationService>().Object,
                new Mock<IAddressResolver>().Object,
                SampleInstance,
                new Mock<ISshKeyPair>().Object);
            context.Parameters.TransportType = SessionTransportType.IapTunnel;

            var sshTransport = await context
                .ConnectTransportAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreSame(transport.Object, sshTransport);
        }

        [Test]
        public async Task WhenTransportTypeIsVpcInternal_ThenConnectTransportCreatesDirectTransport()
        {
            var addressResolver = new Mock<IAddressResolver>();
            addressResolver.Setup(
                r => r.GetAddressAsync(
                    SampleInstance,
                    NetworkInterfaceType.PrimaryInternal,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(IPAddress.Parse("20.21.22.23"));

            var transport = new Mock<ITransport>();
            var factory = new Mock<IDirectTransportFactory>();
            factory
                .Setup(b => b.CreateTransportAsync(
                    SshProtocol.Protocol,
                    new IPEndPoint(IPAddress.Parse("20.21.22.23"), 22),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(transport.Object);

            var context = new SshSessionContext(
                new Mock<IIapTransportFactory>().Object,
                factory.Object,
                new Mock<IKeyAuthorizationService>().Object,
                addressResolver.Object,
                SampleInstance,
                new Mock<ISshKeyPair>().Object);
            context.Parameters.TransportType = SessionTransportType.Vpc;

            var sshTransport = await context
                .ConnectTransportAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreSame(transport.Object, sshTransport);
        }
    }
}
