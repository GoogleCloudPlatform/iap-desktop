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

using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Test.Net
{
    public class RestClient
    {
        public string UserAgent { get; set; }
        public ICredential Credential { get; set; }

        public async Task<TModel> GetAsync<TModel>(
            string url,
            CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (this.UserAgent != null)
                {
                    request.Headers.UserAgent.ParseAdd(this.UserAgent);
                }

                if (this.Credential != null)
                {
                    var accessToken = await this.Credential.GetAccessTokenForRequestAsync(
                        null,
                        cancellationToken);
                    request.Headers.Authorization = new AuthenticationHeaderValue(
                        "Bearer",
                        accessToken);
                }

                using (var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    using (var reader = new StreamReader(stream))
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        return new JsonSerializer().Deserialize<TModel>(jsonReader);
                    }
                }
            }
        }
    }
}
