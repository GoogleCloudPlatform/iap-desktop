﻿//
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

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Common.Net
{
    public static class HttpClientHandlerExtensions
    {
        //
        // NB. The ClientCertificate property is only available as of v4.7.1. 
        //
        // In older framework versions, WebRequestHandler can be used for mTLS,
        // but this is incompatible with the Google Http library.
        //
        internal static readonly PropertyInfo clientCertificatesProperty =
            typeof(HttpClientHandler).GetProperty(
                "ClientCertificates",
                BindingFlags.Instance | BindingFlags.Public);

        public static bool IsClientCertificateSupported
            => clientCertificatesProperty != null;

        public static bool TryAddClientCertificate(
            this HttpClientHandler handler,
            X509Certificate2 certificate)
        {
            if (IsClientCertificateSupported)
            {
                var clientCertificates = (X509CertificateCollection)
                    clientCertificatesProperty.GetValue(handler);
                clientCertificates.Add(certificate);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static IEnumerable<X509Certificate2> GetClientCertificates(
            this HttpClientHandler handler)
        {
            if (IsClientCertificateSupported)
            {
                var certificates = (X509CertificateCollection)
                    clientCertificatesProperty.GetValue(handler);
                return certificates.Cast<X509Certificate2>();
            }
            else
            {
                return Enumerable.Empty<X509Certificate2>();
            }
        }
    }
}
