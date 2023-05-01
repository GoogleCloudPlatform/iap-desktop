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

using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.Testing.Common;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Session
{
    [TestFixture]
    public class TestRdpCredentialCallbackService
    {
        private static readonly Uri SampleCallbackUrl = new Uri("http://example.com/callback");

        [Test]
        public async Task WhenServerReturnsEmptyResult_ThenGetCredentialsReturnsEmptyCredentials()
        {
            var adapter = new Mock<IExternalRestAdapter>();
            adapter
                .Setup(a => a.GetAsync<RdpCredentialCallbackService.CredentialCallbackResponse>(
                    SampleCallbackUrl,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((RdpCredentialCallbackService.CredentialCallbackResponse)null);

            var service = new RdpCredentialCallbackService(adapter.Object);
            var credentials = await service
                .GetCredentialsAsync(SampleCallbackUrl, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(credentials.User);
            Assert.IsNull(credentials.Domain);
            Assert.IsNull(credentials.Password);
        }

        [Test]
        public void WhenServerRequestFails_ThenGetCredentialsThrowsException()
        {
            var adapter = new Mock<IExternalRestAdapter>();
            adapter
                .Setup(a => a.GetAsync<RdpCredentialCallbackService.CredentialCallbackResponse>(
                    SampleCallbackUrl,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException());

            var service = new RdpCredentialCallbackService(adapter.Object);

            ExceptionAssert.ThrowsAggregateException<CredentialCallbackException>(
                () => service.GetCredentialsAsync(SampleCallbackUrl, CancellationToken.None).Wait());
        }

        [Test]
        public void WhenServerReturnsInvalidResponse_ThenGetCredentialsThrowsException()
        {
            var adapter = new Mock<IExternalRestAdapter>();
            adapter
                .Setup(a => a.GetAsync<RdpCredentialCallbackService.CredentialCallbackResponse>(
                    SampleCallbackUrl,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonReaderException());

            var service = new RdpCredentialCallbackService(adapter.Object);

            ExceptionAssert.ThrowsAggregateException<CredentialCallbackException>(
                () => service.GetCredentialsAsync(SampleCallbackUrl, CancellationToken.None).Wait());
        }

        [Test]
        public void WhenServerRequestTimesOut_ThenGetCredentialsThrowsException()
        {
            var adapter = new Mock<IExternalRestAdapter>();
            adapter
                .Setup(a => a.GetAsync<RdpCredentialCallbackService.CredentialCallbackResponse>(
                    SampleCallbackUrl,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException());

            var service = new RdpCredentialCallbackService(adapter.Object);

            ExceptionAssert.ThrowsAggregateException<CredentialCallbackException>(
                () => service.GetCredentialsAsync(SampleCallbackUrl, CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenServerReturnsResult_ThenGetCredentialsReturnsCredentials()
        {
            var adapter = new Mock<IExternalRestAdapter>();
            adapter
                .Setup(a => a.GetAsync<RdpCredentialCallbackService.CredentialCallbackResponse>(
                    SampleCallbackUrl,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RdpCredentialCallbackService.CredentialCallbackResponse()
                {
                    User = "user",
                    Domain = "domain",
                    Password = "password"
                });

            var service = new RdpCredentialCallbackService(adapter.Object);
            var credentials = await service
                .GetCredentialsAsync(SampleCallbackUrl, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("user", credentials.User);
            Assert.AreEqual("domain", credentials.Domain);
            Assert.AreEqual("password", credentials.Password.AsClearText());
        }
    }
}
