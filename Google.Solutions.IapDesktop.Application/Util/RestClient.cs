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

using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Util
{
    internal class RestClient
    {
        private readonly string userAgent;

        public RestClient(string userAgent)
        {
            this.userAgent = userAgent;
        }

        public async Task<TModel> GetAsync<TModel>(string url, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.UserAgent.ParseAdd(this.userAgent);
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

    [Serializable]
    public class RestClientException : Exception
    {
        protected RestClientException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public RestClientException(string message) : base(message)
        {
        }
    }
}
