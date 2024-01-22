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

using Google.Apis.Json;
using Google.Apis.Services;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Analytics
{
    internal class MeasurementService : BaseClientService
    {
        private readonly string apiSecret;
        private readonly string measurementId;

        internal static readonly Uri PublicEndpoint =
            new Uri("https://www.google-analytics.com/");

        public MeasurementService(Initializer initializer)
            : base(initializer)
        {
            this.apiSecret = initializer.ApiKey
                .ExpectNotEmpty(nameof(initializer.ApiKey));
            this.measurementId = initializer.MeasurementId
                .ExpectNotEmpty(nameof(initializer.MeasurementId));
        }

        //---------------------------------------------------------------------
        // BaseClientService.
        //---------------------------------------------------------------------

        public override string Name => "sts";

        public override string BaseUri => base.BaseUriOverride ?? PublicEndpoint.ToString();

        public override string BasePath => "/";

        public override IList<string> Features => Array.Empty<string>();

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public async Task CollectAsync(
            MeasurementRequest request,
            CancellationToken cancellationToken)
        {
            request.ExpectNotNull(nameof(request));

            var prefix = request.DebugMode ? "debug/" : string.Empty;
            var path = $"{prefix}mp/collect?" +
                $"api_secret={this.apiSecret}&measurement_id={this.measurementId}";

            using (ApiTraceSource.Log.TraceMethod().WithoutParameters())
            using (var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(new Uri(this.BaseUri), path))
            {
                Content = new StringContent(NewtonsoftJsonSerializer.Instance.Serialize(request))
            })
            using (var response = await this.HttpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false))
            {
                var stream = await response.Content
                    .ReadAsStreamAsync()
                    .ConfigureAwait(false);

                //
                // Unlike other APIs, this API returns errors in
                // the same format as successes.
                //
                if (NewtonsoftJsonSerializer
                        .Instance
                        .Deserialize<MeasurementResponse>(stream) is var body &&
                    body != null &&
                    body.ValidationMessages.EnsureNotNull().Any())
                {
                    throw new GoogleApiException(
                        "The request failed validation: " + string.Join(
                            ", ",
                            body
                                .ValidationMessages
                                .Select(m => $"[{m.ValidationCode}] {m.Description}")));
                }
            }
        }

        //---------------------------------------------------------------------
        // Initializer.
        //---------------------------------------------------------------------

        public new class Initializer : BaseClientService.Initializer
        {
            /// <summary>
            /// Measurement ID for stream.
            /// </summary>
            public string? MeasurementId { get; set; }
        }

        //---------------------------------------------------------------------
        // Request/response classes.
        //---------------------------------------------------------------------

        public class MeasurementRequest
        {
            [JsonProperty("client_id")]
            public string? ClientId { get; set; }

            [JsonProperty("user_id")]
            public string? UserId { get; set; }

            [JsonProperty("user_properties")]
            public IDictionary<string, PropertySection>? UserProperties { get; set; }

            [JsonProperty("events")]
            public IList<EventSection>? Events { get; set; }

            [JsonIgnore]
            public bool DebugMode { get; set; }
        }

        public class MeasurementResponse
        {
            [JsonConstructor]
            public MeasurementResponse(
                [JsonProperty("validationMessages")] List<ValidationMessage> validationMessages
            )
            {
                this.ValidationMessages = validationMessages;
            }

            [JsonProperty("validationMessages")]
            public IReadOnlyList<ValidationMessage> ValidationMessages { get; }
        }

        public class ValidationMessage
        {
            [JsonConstructor]
            public ValidationMessage(
                [JsonProperty("description")] string description,
                [JsonProperty("validationCode")] string validationCode
            )
            {
                this.Description = description;
                this.ValidationCode = validationCode;
            }

            [JsonProperty("description")]
            public string Description { get; }

            [JsonProperty("validationCode")]
            public string ValidationCode { get; }
        }

        public class PropertySection
        {
            [JsonProperty("value")]
            public string Value { get; }

            public PropertySection(string value)
            {
                this.Value = value;
            }
        }

        public class EventSection
        {
            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("params")]
            public IDictionary<string, string>? Parameters { get; set; }
        }
    }
}
