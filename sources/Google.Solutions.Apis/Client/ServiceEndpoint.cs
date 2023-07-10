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


using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;

namespace Google.Solutions.Apis.Client
{
    /// <summary>
    /// An endpoint for a Google API.
    /// </summary>
    public class ServiceEndpoint
    {
        /// <summary>
        /// Base URI to initialze the client library with.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Type of endpoint.
        /// </summary>
        public EndpointType Type { get; }

        internal ServiceEndpoint(
            Uri uri,
            EndpointType type)
        {
            this.Uri = uri.ExpectNotNull(nameof(uri));
            this.Type = type;

            Debug.Assert(type != EndpointType.MutualTls || uri.Host.Contains("mtls."));
        }

        public override string ToString()
        {
            return this.Uri.ToString();
        }
    }

    public enum EndpointType
    {
        Tls,
        MutualTls,
        PrivateServiceConnect
    }
}
