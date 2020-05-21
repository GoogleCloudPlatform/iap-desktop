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

using Google.Solutions.Common.Net;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1034 // Class nesting

namespace Google.Solutions.Common.Test.Net
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestRestClient : FixtureBase
    {
        private const string SampleRestUrl = "https://accounts.google.com/.well-known/openid-configuration";
        private const string NotFoundUrl = "http://accounts.google.com/.well-known/openid-configuration";

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
        [Ignore("Unreliable in CI")]
        public void WhenUrReturns404_ThenHttpExceptionIsThrown()
        {
            var client = new RestClient();

            try
            {
                client.GetAsync<SampleResource>(
                    NotFoundUrl + "-invalid",
                    CancellationToken.None).Wait();

                Assert.Fail("Expected call to fail");
            }
            catch (Exception e) when (e.Is<HttpRequestException>())
            {
            }
            catch (Exception e)
            {
                Assert.Fail($"Did not expect exception of type {e.GetType()}");
            }
        }
    }
}
