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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Json;
using Google.Apis.Requests.Parameters;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth.Iam
{
    /// <summary>
    /// The STS API follows different conventions than other APIs,
    /// and the generated client libraries (Google.Apis) don't properly
    /// reflect that. Therefore, use custom client implementation.
    /// </summary>
    internal class StsService : BaseClientService
    {
        public StsService(Initializer initializer)
            : base(initializer)
        {
        }

        //---------------------------------------------------------------------
        // BaseClientService.
        //---------------------------------------------------------------------

        public override string Name => "sts";

        public override string BaseUri
            => base.BaseUriOverride ?? "https://sts.googleapis.com/";

        public override string BasePath => "/";

        public override IList<string> Features => Array.Empty<string>();

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public async Task<IntrospectTokenResponse> IntrospectTokenAsync(
            IntrospectTokenRequest request,
            CancellationToken cancellationToken)
        {
            request.ExpectNotNull(nameof(request));

            using (ApiTraceSource.Log.TraceMethod().WithoutParameters())
            using (var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(new Uri(this.BaseUri), "/v1/introspect"))
            {
                Content = ParameterUtils.CreateFormUrlEncodedContent(request)
            })
            {
                if (request.ClientCredentials != null)
                {
                    var headerValue =
                        $"{request.ClientCredentials.ClientId}:{request.ClientCredentials.ClientSecret}";

                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
                        "Basic",
                         Convert.ToBase64String(
                             Encoding.UTF8.GetBytes(headerValue)));
                }

                using (var response = await this.HttpClient
                    .SendAsync(httpRequest, cancellationToken)
                    .ConfigureAwait(false))
                {
                    var stream = await response.Content
                        .ReadAsStreamAsync()
                        .ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        return NewtonsoftJsonSerializer.Instance
                            .Deserialize<IntrospectTokenResponse>(stream);
                    }
                    else
                    {
                        //
                        // Unlike other APIs, this API returns errors in
                        // OAuth format.
                        //
                        var error = NewtonsoftJsonSerializer.Instance
                            .Deserialize<TokenErrorResponse>(stream);

                        throw new TokenResponseException(error);
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Request/response classes.
        //---------------------------------------------------------------------

        /// <summary>
        /// Request message for IntrospectToken.
        /// </summary>
        public class IntrospectTokenRequest
        {
            public ClientSecrets? ClientCredentials { get; set; }

            /// <summary>
            /// Required. The OAuth 2.0 security token issued by the Security Token Service API.
            /// </summary>
            [RequestParameter("token")]
            public string? Token { get; set; }

            /// <summary>
            /// Optional. The type of the given token. 
            /// </summary>
            [RequestParameter("token_type_hint")]
            public string? TokenTypeHint { get; set; }
        }

        /// <summary>
        /// Response message for IntrospectToken.
        /// </summary>    
        public class IntrospectTokenResponse
        {
            /// <summary>
            /// A boolean value that indicates whether the provided access
            /// token is currently active.
            /// </summary>
            [JsonProperty("active")]
            public bool? Active { get; set; }

            /// <summary>
            /// The client identifier for the OAuth 2.0 client that requested 
            /// the provided token.
            /// </summary>
            [JsonProperty("client_id")]
            public string? ClientId { get; set; }

            /// <summary>
            /// The expiration timestamp.
            /// </summary>
            [JsonProperty("exp")]
            public long? Exp { get; set; }

            /// <summary>
            /// The issued timestamp.
            /// </summary>
            [JsonProperty("iat")]
            public long? Iat { get; set; }

            /// <summary>
            /// The issuer of the provided token.
            /// </summary>        
            [JsonProperty("iss")]
            public string? Iss { get; set; }

            /// <summary>
            /// A list of scopes associated with the provided token.
            /// </summary>
            [JsonProperty("scope")]
            public string? Scope { get; set; }

            /// <summary>
            /// The unique user ID associated with the provided token. 
            /// </summary>
            [JsonProperty("sub")]
            public string? Sub { get; set; }

            /// <summary>
            /// The human-readable identifier for the token principal subject.
            /// </summary>
            [JsonProperty("username")]
            public string? Username { get; set; }
        }

        public static class TokenTypes
        {
            public const string AccessToken = "urn:ietf:params:oauth:token-type:access_token";
        }
    }
}