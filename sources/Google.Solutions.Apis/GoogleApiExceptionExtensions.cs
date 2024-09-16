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

using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Json;
using Google.Solutions.Apis.Diagnostics;
using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Apis
{
    public static class GoogleApiExceptionExtensions
    {
        public static bool IsConstraintViolation(this GoogleApiException e)
            => e.Error != null && e.Error.Code == 412;

        public static bool IsAccessDenied(this GoogleApiException e)
            => e.Error != null && e.Error.Code == 403;

        public static bool IsNotFound(this GoogleApiException e)
            => e.Error != null && e.Error.Code == 404;

        public static bool IsBadRequest(this GoogleApiException e)
            => e.Error != null && e.Error.Code == 400 && e.Error.Message == "BAD REQUEST";

        public static bool IsReauthError(this Exception e)
        {
            // The TokenResponseException might be hiding in an AggregateException
            e = e.Unwrap();

            if (e is TokenResponseException tokenException)
            {
                return tokenException.Error.Error == "invalid_grant";
            }
            else
            {
                return false;
            }
        }


        public static bool IsAccessDeniedByVpcServiceControlPolicy(this GoogleApiException e)
        {
            return e.IsAccessDenied() &&
                !string.IsNullOrEmpty(e.Message) &&
                e.Message.Contains("vpcServiceControlsUniqueIdentifier");
        }

        /// <summary>
        /// Extract VPC SC troubleshooting ID.
        /// </summary>
        /// <returns>ID or null</returns>
        public static string? VpcServiceControlTroubleshootingId(this GoogleApiException e)
        {
            var rawJson = e.Error?.ErrorResponseContent;
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return null;
            }

            try
            {
                return NewtonsoftJsonSerializer.Instance
                    .Deserialize<ErrorEnvelope>(rawJson)?
                    .Error?
                    .Details?
                    .EnsureNotNull()
                    .Where(d => d.Violations != null)
                    .SelectMany(d => d.Violations)
                    .EnsureNotNull()
                    .FirstOrDefault(v => v.Type == "VPC_SERVICE_CONTROLS")?
                    .Description;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static HelpTopic? VpcServiceControlTroubleshootingLink(this GoogleApiException e)
        {
            if (e.VpcServiceControlTroubleshootingId() is var id && id != null)
            {
                return new HelpTopic(
                    "VPC Service Controls Troubleshooter",
                    $"https://console.cloud.google.com/security/service-perimeter/troubleshoot;uniqueId={id}");
            }
            else
            {
                return null;
            }
        }

        public static bool IsAccessDeniedError(this Exception e)
        {
            return e.Unwrap() is GoogleApiException apiEx && apiEx.Error.Code == 403;
        }

        //---------------------------------------------------------------------
        // JSON deserialization.
        //---------------------------------------------------------------------

        public class ErrorEnvelope
        {
            [JsonConstructor]
            public ErrorEnvelope([JsonProperty("error")] ErrorSection error)
            {
                this.Error = error;
            }

            [JsonProperty("error")]
            public ErrorSection Error { get; }
        }

        public class DetailSection
        {
            [JsonConstructor]
            public DetailSection(
                [JsonProperty("@type")] string type,
                [JsonProperty("violations")] List<ViolationSection> violations)
            {
                this.Type = type;
                this.Violations = violations;
            }

            [JsonProperty("@type")]
            public string Type { get; }

            [JsonProperty("violations")]
            public IReadOnlyList<ViolationSection> Violations { get; }
        }

        public class ErrorSection
        {
            [JsonConstructor]
            public ErrorSection(
                [JsonProperty("details")] List<DetailSection> details)
            {
                this.Details = details;
            }

            [JsonProperty("details")]
            public IReadOnlyList<DetailSection> Details { get; }
        }

        public class ViolationSection
        {
            [JsonConstructor]
            public ViolationSection(
                [JsonProperty("type")] string type,
                [JsonProperty("description")] string description)
            {
                this.Type = type;
                this.Description = description;
            }

            [JsonProperty("type")]
            public string Type { get; }

            [JsonProperty("description")]
            public string Description { get; }
        }
    }
}
