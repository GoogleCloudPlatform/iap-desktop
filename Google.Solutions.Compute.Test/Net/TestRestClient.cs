//
// Copyright 2019 Google LLC
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

using Google.Solutions.Compute.Iap;
using Google.Solutions.Compute.Net;
using Google.Solutions.Compute.Test.Env;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Test.Net
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestRestClient
    {
        private string SampleRestUrl = "https://accounts.google.com/.well-known/openid-configuration";
        private string NotFoundUrl = "http://accounts.google.com/.well-known/openid-configuration";
        private string RedirectingUrl = "https://accounts.google.com/";

        public class SampleResource
        {
            [JsonProperty("issuer")]
            public string Issuer { get; set; }
        }

        [Test]
        public async Task WhenUrlPointsToJson_ThenGetAsyncReturnsObject()
        {
            var client = new RestClient();
            var result = await client.GetAsync<SampleResource>(
                SampleRestUrl, 
                CancellationToken.None);

            Assert.IsNotNull(result.Issuer);
        }

        [Test]
        public void WhenUrReturns404_ThenHttpRequestExceptionIsThrown()
        {
            var client = new RestClient();

            AssertEx.ThrowsAggregateException<HttpRequestException>(() =>
                {
                    client.GetAsync<SampleResource>(
                        NotFoundUrl + "-invalid",
                        CancellationToken.None).Wait();
                });
        }
    }
}
