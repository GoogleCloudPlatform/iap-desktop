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

using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Util;
using Google.Solutions.Testing.Apis;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Client
{
    [TestFixture]
    public class TestRestClient : CommonFixtureBase
    {
        private const string SampleRestUrl = "https://accounts.google.com/.well-known/openid-configuration";
        private const string NotFoundUrl = "https://gstatic.com/generate_404";
        private const string NoContentUrl = "https://gstatic.com/generate_204";
        private static readonly UserAgent userAgent = new UserAgent(
            "test",
            new Version(1, 0),
            Environment.OSVersion.VersionString);

        public class SampleResource
        {
            [JsonProperty("issuer")]
            public string? Issuer { get; set; }
        }

        [Test]
        public async Task Get_WhenUrlPointsToJson()
        {
            var client = new RestClient(userAgent, null);
            var result = await client.GetAsync<SampleResource>(
                    SampleRestUrl,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(result?.Issuer, Is.Not.Null);
        }

        [Test]
        public async Task Get_WhenUrlPointsToNoContent()
        {
            var client = new RestClient(userAgent, null);
            var result = await client.GetAsync<SampleResource>(
                    NoContentUrl,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Get_WhenUrlReturns404()
        {
            var client = new RestClient(userAgent, null);

            var e = ExceptionAssert.ThrowsAggregateException<RestClientException>(
                () => client.GetAsync<SampleResource>(
                        NotFoundUrl,
                        CancellationToken.None)
                    .Wait());
            Assert.That(e!.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
}
