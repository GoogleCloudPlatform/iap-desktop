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
        void UseCustomProxySettings(Uri proxyAddress, ICredentials credetial);

        /// <summary>
        /// Reset current process to use system-defined proxy settings.
        /// </summary>
        void UseSystemProxySettings();
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

        public void UseCustomProxySettings(Uri proxyAddress, ICredentials credentials)
        {
            lock (this.configLock)
            {
                WebRequest.DefaultWebProxy = new WebProxy(proxyAddress)
                {
                    Credentials = credentials
                };
            }
        }

        public void UseSystemProxySettings()
        {
            lock (this.configLock)
            {
                WebRequest.DefaultWebProxy = defaultProxy;
            }
        }
    }
}
