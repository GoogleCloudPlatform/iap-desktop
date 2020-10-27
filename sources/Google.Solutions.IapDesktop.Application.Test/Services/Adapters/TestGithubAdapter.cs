//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Test.Net;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Adapters
{
    [TestFixture]
    public class TestGithubAdapter : FixtureBase
    {
        [Test]
        public async Task WhenFindingLatestRelease_OneReleaseIsReturned()
        {
            var adapter = new GithubAdapter();
            var release = await adapter.FindLatestReleaseAsync(CancellationToken.None);

            Assert.IsNotNull(release);
            Assert.IsTrue(release.TagVersion.Major >= 1);
        }

        //---------------------------------------------------------------------
        // Proxy.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProxyEnabledAndCredentialsCorrect_ThenRequestSucceeds()
        {
            var proxyCredentials = new NetworkCredential("proxyuser", "proxypass");
            using (var proxy = new InProcessAuthenticatingHttpProxy(
                proxyCredentials))
            {
                var adapter = new GithubAdapter();

                var proxyAdapter = new HttpProxyAdapter();
                proxyAdapter.UseCustomProxySettings(
                    new Uri($"http://localhost:{proxy.Port}"),
                    proxyCredentials);

                await adapter.FindLatestReleaseAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task WhenProxyEnabledAndCredentialsWrong_ThenRequestThrowsWebException()
        {
            var proxyCredentials = new NetworkCredential("proxyuser", "proxypass");
            using (var proxy = new InProcessAuthenticatingHttpProxy(
                proxyCredentials))
            {
                var adapter = new GithubAdapter();

                var proxyAdapter = new HttpProxyAdapter();
                proxyAdapter.UseCustomProxySettings(
                    new Uri($"http://localhost:{proxy.Port}"),
                    new NetworkCredential("proxyuser", "wrong"));

                try
                {
                    await adapter.FindLatestReleaseAsync(CancellationToken.None);
                    Assert.Fail("Exception expected");
                }
                catch (HttpRequestException e) when (e.InnerException is WebException exception)
                {
                    Assert.AreEqual(
                        HttpStatusCode.ProxyAuthenticationRequired,
                        ((HttpWebResponse)exception.Response).StatusCode);
                }
            }
        }
    }
}
