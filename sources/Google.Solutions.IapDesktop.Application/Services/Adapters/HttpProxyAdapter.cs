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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface IHttpProxyAdapter
    {
        /// <summary>
        /// Set a custom HTTP proxy for the current process. Settings
        /// are not persisted.
        /// </summary>
        void ActivateCustomProxySettings(
            Uri proxyAddress,
            IEnumerable<string> bypassList,
            ICredentials credentials);

        /// <summary>
        /// Reset current process to use system-defined proxy settings.
        /// </summary>
        void ActivateSystemProxySettings();

        /// <summary>
        /// Read and activate settings.
        /// </summary>
        /// <param name="settings"></param>
        void ActivateSettings(ApplicationSettings settings);
    }

    public class HttpProxyAdapter : IHttpProxyAdapter
    {
        private readonly object configLock = new object();
        private static readonly IWebProxy defaultProxy;

        static HttpProxyAdapter()
        {
            // Obtain default settings in case we need to revert to those later.
            defaultProxy = WebRequest.DefaultWebProxy;
            Debug.Assert(defaultProxy != null);
        }

        public void ActivateCustomProxySettings(
            Uri proxyAddress,
            IEnumerable<string> bypassList,
            ICredentials credentials)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(
                proxyAddress,
                credentials != null ? "(credentials)" : null))
            {
                lock (this.configLock)
                {
                    WebRequest.DefaultWebProxy = new WebProxy(proxyAddress)
                    {
                        Credentials = credentials,
                        BypassList = bypassList
                            .EnsureNotNull()
                            .ToArray()
                    };
                }
            }
        }

        public void ActivateSystemProxySettings()
        {
            using (TraceSources.IapDesktop.TraceMethod().WithoutParameters())
            {
                lock (this.configLock)
                {
                    WebRequest.DefaultWebProxy = defaultProxy;
                }
            }
        }

        public void ActivateSettings(ApplicationSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.ProxyUrl.StringValue))
            {
                NetworkCredential credential = null;
                if (!string.IsNullOrEmpty(settings.ProxyUsername.StringValue))
                {
                    credential = new NetworkCredential(
                        settings.ProxyUsername.StringValue,
                        (SecureString)settings.ProxyPassword.Value);
                }

                ActivateCustomProxySettings(
                    new Uri(settings.ProxyUrl.StringValue),
                    null,
                    credential);
            }
            else
            {
                // No proxy set -> use system settings.
                ActivateSystemProxySettings();
            }
        }
    }
}
