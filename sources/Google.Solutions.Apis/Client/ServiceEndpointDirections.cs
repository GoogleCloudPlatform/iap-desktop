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

using System;

namespace Google.Solutions.Apis.Client
{
    /// <summary>
    /// Directions for connecting to a particular endpoint of the
    /// Google API.
    /// </summary>
    public readonly struct ServiceEndpointDirections
    {
        /// <summary>
        /// Type of endpoint.
        /// </summary>
        public ServiceEndpointType Type { get; }

        /// <summary>
        /// Base URI to use for sending requests. When using PSC,
        /// the base URI uses the PSC endpoint as host.
        /// </summary>
        public Uri BaseUri { get; }

        /// <summary>
        /// Host header to inject. Only applicable if Type
        /// is set to PrivateServiceConnect.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Determines whether a client certificate must be used.
        /// </summary>
        public bool UseClientCertificate => this.Type == ServiceEndpointType.MutualTls;

        internal ServiceEndpointDirections(ServiceEndpointType type, Uri baseUri, string host)
        {
            this.Type = type;
            this.BaseUri = baseUri;
            this.Host = host;
        }

        public override string ToString()
        {
            return $"{this.BaseUri} (Type: {this.Type}, Host: {this.Host}, " +
                $"Cert: {this.UseClientCertificate})";
        }
    }

    public enum ServiceEndpointType
    {
        Tls,
        MutualTls,
        PrivateServiceConnect
    }
}
