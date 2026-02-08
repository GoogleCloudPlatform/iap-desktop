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

using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.Testing.Apis;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Rdp
{
    [TestFixture]
    public class TestRdpCredentialCallback
    {
        private static readonly Uri SampleCallbackUrl = new Uri("http://example.com/callback");

        //----------------------------------------------------------------------
        // GetCredentials.
        //----------------------------------------------------------------------

        [Test]
        public async Task GetCredentials_WhenServerReturnsEmptyResult()
        {
            var adapter = new Mock<IExternalRestClient>();
            adapter
                .Setup(a => a.GetAsync<RdpCredentialCallback.CredentialCallbackResponse>(
                    SampleCallbackUrl,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((RdpCredentialCallback.CredentialCallbackResponse?)null);

            var service = new RdpCredentialCallback(adapter.Object);
            var credentials = await service
                .GetCredentialsAsync(SampleCallbackUrl, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(credentials.User, Is.Null);
            Assert.That(credentials.Domain, Is.Null);
            Assert.That(credentials.Password, Is.Null);
        }

        [Test]
        public void GetCredentials_WhenServerRequestFails()
        {
            var adapter = new Mock<IExternalRestClient>();
            adapter
                .Setup(a => a.GetAsync<RdpCredentialCallback.CredentialCallbackResponse>(
                    SampleCallbackUrl,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException());

            var service = new RdpCredentialCallback(adapter.Object);

            ExceptionAssert.ThrowsAggregateException<CredentialCallbackException>(
                () => service.GetCredentialsAsync(SampleCallbackUrl, CancellationToken.None).Wait());
        }

        [Test]
        public void GetCredentials_WhenServerReturnsInvalidResponse()
        {
            var adapter = new Mock<IExternalRestClient>();
            adapter
                .Setup(a => a.GetAsync<RdpCredentialCallback.CredentialCallbackResponse>(
                    SampleCallbackUrl,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonReaderException());

            var service = new RdpCredentialCallback(adapter.Object);

            ExceptionAssert.ThrowsAggregateException<CredentialCallbackException>(
                () => service.GetCredentialsAsync(SampleCallbackUrl, CancellationToken.None).Wait());
        }

        [Test]
        public void GetCredentials_WhenServerRequestTimesOut()
        {
            var adapter = new Mock<IExternalRestClient>();
            adapter
                .Setup(a => a.GetAsync<RdpCredentialCallback.CredentialCallbackResponse>(
                    SampleCallbackUrl,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException());

            var service = new RdpCredentialCallback(adapter.Object);

            ExceptionAssert.ThrowsAggregateException<CredentialCallbackException>(
                () => service.GetCredentialsAsync(SampleCallbackUrl, CancellationToken.None).Wait());
        }

        [Test]
        public async Task GetCredentials_WhenServerReturnsResult()
        {
            var adapter = new Mock<IExternalRestClient>();
            adapter
                .Setup(a => a.GetAsync<RdpCredentialCallback.CredentialCallbackResponse>(
                    SampleCallbackUrl,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RdpCredentialCallback.CredentialCallbackResponse()
                {
                    User = "user",
                    Domain = "domain",
                    Password = "password"
                });

            var service = new RdpCredentialCallback(adapter.Object);
            var credentials = await service
                .GetCredentialsAsync(SampleCallbackUrl, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(credentials.User, Is.EqualTo("user"));
            Assert.That(credentials.Domain, Is.EqualTo("domain"));
            Assert.That(credentials.Password.ToClearText(), Is.EqualTo("password"));
        }
    }
}
