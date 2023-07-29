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

using Google.Apis.Auth;
using Google.Apis.Json;
using Google.Solutions.Common.Format;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System.Text;

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// A decoded but unverified Google JWT.
    /// </summary>
    internal class UnverifiedGoogleJsonWebToken
    {
        public GoogleJsonWebSignature.Header Header;
        public GoogleJsonWebSignature.Payload Payload;

        private UnverifiedGoogleJsonWebToken(
            GoogleJsonWebSignature.Header header,
            GoogleJsonWebSignature.Payload payload)
        {
            this.Header = header;
            this.Payload = payload;
        }

        /// <summary>
        /// Decode, but don't verify, a JSON web token.
        /// </summary>
        public static UnverifiedGoogleJsonWebToken Decode(string token)
        {
            token.ExpectNotEmpty(nameof(token));

            var tokenParts = token.Split('.');
            if (tokenParts.Length != 3)
            {
                throw new InvalidJwtException(
                    "The JWT must consist of header, payload, and signature");
            }

            var encodedHeader = tokenParts[0];
            var encodedPayload = tokenParts[1];

            try
            {
                var header = NewtonsoftJsonSerializer.Instance.Deserialize<GoogleJsonWebSignature.Header>(
                    Encoding.UTF8.GetString(Base64UrlEncoding.Decode(encodedHeader)));
                var payload = NewtonsoftJsonSerializer.Instance.Deserialize<GoogleJsonWebSignature.Payload>(
                    Encoding.UTF8.GetString(Base64UrlEncoding.Decode(encodedPayload)));

                return new UnverifiedGoogleJsonWebToken(header, payload);
            } 
            catch (JsonException e)
            {
                throw new InvalidJwtException(
                    "The JWT contains malformed JSON data");
            }
        }
    }
}
