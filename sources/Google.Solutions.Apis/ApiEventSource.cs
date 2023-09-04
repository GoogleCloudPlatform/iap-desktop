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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using System;
using System.Diagnostics.Tracing;
using System.Net;
using System.Net.Http;

namespace Google.Solutions.Apis
{
    /// <summary>
    /// ETW event source.
    /// </summary>
    [EventSource(Name = "Google-Solutions-Api", Guid = "EC3585B8-5C28-42AE-8CE7-D76CB00303C6")]
    public sealed class ApiEventSource : EventSource
    {
        public static ApiEventSource Log { get; } = new ApiEventSource();

        //---------------------------------------------------------------------
        // HTTP
        //---------------------------------------------------------------------

        [Event(1, Level = EventLevel.Verbose)]
        public void HttpRequestInitiated(
            string method,
            string requestUri,
            string endpointType,
            string pscEndpoint)
            => WriteEvent(1, method, requestUri, endpointType, pscEndpoint);

        [Event(2, Level = EventLevel.Warning)]
        public void HttpRequestFailed(
            string method,
            string requestUri,
            int statusCode)
            => WriteEvent(
                2, 
                method, 
                requestUri, 
                statusCode);

        //---------------------------------------------------------------------
        // Auth
        //---------------------------------------------------------------------

        [Event(100, Level = EventLevel.Informational)]
        public void OfflineCredentialActivated(
            string issuer)
            => WriteEvent(100, issuer);

        [Event(101, Level = EventLevel.Warning)]
        public void OfflineCredentialActivationFailed(
            string issuer,
            string error)
            => WriteEvent(101, issuer, error);

        [Event(102, Level = EventLevel.Informational)]
        public void Authorized(string issuer)
            => WriteEvent(102, issuer);
    }
}
