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

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace Google.Solutions.Apis
{
    /// <summary>
    /// ETW event source.
    /// </summary>
    [SuppressMessage("Style",
        "IDE0060:Remove unused parameter",
        Justification = "ETW parameters")]
    [EventSource(Name = ProviderName, Guid = ProviderGuid)]
    public sealed class ApiEventSource : EventSource
    {
        public const string ProviderName = "Google-Solutions-Apis";
        public const string ProviderGuid = "EC3585B8-5C28-42AE-8CE7-D76CB00303C6";

        public static ApiEventSource Log { get; } = new ApiEventSource();

        //---------------------------------------------------------------------
        // HTTP
        //---------------------------------------------------------------------

        [Event(1, Level = EventLevel.Verbose)]
        internal void HttpRequestInitiated(
            string method,
            string requestUri,
            string endpointType,
            string pscEndpoint)
            => WriteEvent(1, method, requestUri, endpointType, pscEndpoint);

        [Event(2, Level = EventLevel.Warning)]
        internal void HttpRequestFailed(
            string method,
            string requestUri,
            int statusCode)
            => WriteEvent(2, method, requestUri, statusCode);

        [Event(3, Level = EventLevel.Warning)]
        internal void HttpNtlmProxyRequestFailed(
            string requestUri,
            int attempt,
            string errorMessage)
            => WriteEvent(3, requestUri, errorMessage);

        //---------------------------------------------------------------------
        // Auth
        //---------------------------------------------------------------------

        public const int OfflineCredentialActivatedId = 100;
        public const int OfflineCredentialActivationFailedId = 101;
        public const int AuthorizedId = 102;

        [Event(OfflineCredentialActivatedId, Level = EventLevel.Informational)]
        internal void OfflineCredentialActivated(
            string issuer)
            => WriteEvent(OfflineCredentialActivatedId, issuer);

        [Event(OfflineCredentialActivationFailedId, Level = EventLevel.Warning)]
        internal void OfflineCredentialActivationFailed(
            string issuer,
            string error)
            => WriteEvent(OfflineCredentialActivationFailedId, issuer, error);

        [Event(AuthorizedId, Level = EventLevel.Informational)]
        internal void Authorized(string issuer)
            => WriteEvent(AuthorizedId, issuer);
    }
}
