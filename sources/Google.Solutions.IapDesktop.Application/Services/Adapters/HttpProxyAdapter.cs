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

using Google.Solutions.IapDesktop.Application.Services.Settings;
using System;
using System.Diagnostics;
using System.Net;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface IHttpProxyAdapter
    {
        /// <summary>
        /// Set a custom HTTP proxy for the current process. Settings
        /// are not persisted.
        /// </summary>
        void ActivateCustomProxySettings(Uri proxyAddress, ICredentials credetial);

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

        public void ActivateCustomProxySettings(Uri proxyAddress, ICredentials credentials)
        {
            lock (this.configLock)
            {
                WebRequest.DefaultWebProxy = new WebProxy(proxyAddress)
                {
                    Credentials = credentials
                };
            }
        }

        public void ActivateSystemProxySettings()
        {
            lock (this.configLock)
            {
                WebRequest.DefaultWebProxy = defaultProxy;
            }
        }

        public void ActivateSettings(ApplicationSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.ProxyUrl.StringValue))
            {
                ActivateCustomProxySettings(new Uri(settings.ProxyUrl.StringValue), null);
            }
            else
            {
                // No proxy set -> use system settings.
                ActivateSystemProxySettings();
            }
        }
    }
}
