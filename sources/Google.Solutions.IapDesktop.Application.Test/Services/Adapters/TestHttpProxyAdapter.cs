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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Adapters
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestHttpProxyAdapter : FixtureBase
    {
        // Use a host name that is unlikely to be hit by any initialization code
        // which might be running in parallel with a test case.
        private static readonly Uri SampleHttpsUrl =
            new Uri("https://www.gstatic.com/ipranges/goog.json");

        // Bypass normal API requests so that we do not interfere with
        // VM or credential provisioning which might happen in parallel.
        private static readonly string[] ProxyBypassList = new[]
        {
            "(.*).googleapis.com"
        };

        [TearDown]
        public void RestoreProxySettings()
        {
            // Restore settings to not impact other tests.
            new HttpProxyAdapter().ActivateSystemProxySettings();
        }

        private static async Task<string> SendWebRequest(Uri url)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            using (var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                CancellationToken.None))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        [Test]
        public async Task WhenUsingCustomProxySettingsWithoutCredentials_ThenRequestsAreSentToProxy()
        {
            using (var proxy = new InProcessHttpProxy())
            {
                var adapter = new HttpProxyAdapter();
                adapter.ActivateCustomProxySettings(
                    new Uri($"http://localhost:{proxy.Port}"),
                    ProxyBypassList,
                    null);

                await SendWebRequest(SampleHttpsUrl);

                Assert.AreEqual(1, proxy.ConnectionTargets.Distinct().Count());
                CollectionAssert.Contains(proxy.ConnectionTargets, SampleHttpsUrl.Host);
            }
        }

        [Test]
        public async Task WhenUsingCustomProxySettingsWithCredentials_ThenRequestsAreSentToProxyWithCredentials()
        {
            var proxyCredentials = new NetworkCredential("proxyuser", "proxypass");
            using (var proxy = new InProcessAuthenticatingHttpProxy(
                proxyCredentials))
            {
                var adapter = new HttpProxyAdapter();
                adapter.ActivateCustomProxySettings(
                    new Uri($"http://localhost:{proxy.Port}"),
                    ProxyBypassList,
                    proxyCredentials);

                await SendWebRequest(SampleHttpsUrl);

                Assert.AreEqual(1, proxy.ConnectionTargets.Distinct().Count());
                CollectionAssert.Contains(proxy.ConnectionTargets, SampleHttpsUrl.Host);
            }
        }


        [Test]
        public async Task WhenUsingProxyAutoConfigWithoutCredentials_ThenRequestsAreSentToProxy()
        {
            NetTracing.Enabled = true;
            NetTracing.Web.Switch.Level = System.Diagnostics.SourceLevels.Verbose;
            NetTracing.Web.Listeners.Add(new ConsoleTraceListener());


            using (var proxy = new InProcessHttpProxy())
            {
                proxy.AddStaticFile(
                    "/proxy.pac",
                    "function FindProxyForURL(url, host) " +
                    "{ return \"PROXY localhost:" + proxy.Port + "; DIRECT\";}");

                var adapter = new HttpProxyAdapter();
                adapter.ActivateProxyAutoConfigSettings(
                    //new Uri($"http://localhost:{proxy.Port}/proxy.pac"),
                    new Uri("https://gist.githubusercontent.com/jpassing/74ef3acf00bde508d1bcf8e542eb54ad/raw/f0d759b3fd210c9396ddb9d5c6fd79c3317cdff8/gistfile1.txt"),
                    null);

                var proxiedUrl = WebRequest.DefaultWebProxy.GetProxy(SampleHttpsUrl);
                Assert.AreNotEqual(proxiedUrl, SampleHttpsUrl);

                await SendWebRequest(SampleHttpsUrl);

                Assert.AreEqual(1, proxy.ConnectionTargets.Distinct().Count());
                CollectionAssert.Contains(proxy.ConnectionTargets, SampleHttpsUrl.Host);
            }
        }

        [Test]
        public async Task WhenUsingProxyAutoConfigWithCredentials_ThenRequestsAreSentToProxyWithCredentials()
        {
            Assert.Fail();
        }

        [Test]
        public async Task WhenRevertedToSystemProxySettings_ThenRequestsAreNotSentToProxy()
        {
            var proxyCredentials = new NetworkCredential("proxyuser", "proxypass");
            using (var proxy = new InProcessAuthenticatingHttpProxy(
                proxyCredentials))
            {
                var adapter = new HttpProxyAdapter();
                adapter.ActivateCustomProxySettings(
                    new Uri($"http://localhost:{proxy.Port}"),
                    ProxyBypassList,
                    proxyCredentials);
                adapter.ActivateSystemProxySettings();

                await SendWebRequest(SampleHttpsUrl);

                CollectionAssert.DoesNotContain(proxy.ConnectionTargets, SampleHttpsUrl.Host);
            }
        }
    }
}
