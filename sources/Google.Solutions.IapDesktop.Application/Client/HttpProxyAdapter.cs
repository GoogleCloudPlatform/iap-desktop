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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;

#pragma warning disable CA1810 // Initialize reference type static fields inline

namespace Google.Solutions.IapDesktop.Application.Client
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
        /// Obtain HTTP proxy settings from a PAC URL. Settings
        /// are not persisted.
        /// </summary>
        void ActivateProxyAutoConfigSettings(
            Uri pacAddress,
            ICredentials credentials);

        /// <summary>
        /// Reset current process to use system-defined proxy settings.
        /// </summary>
        void ActivateSystemProxySettings();

        /// <summary>
        /// Read and activate settings.
        /// </summary>
        /// <param name="settings"></param>
        void ActivateSettings(IApplicationSettings settings);
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
            ICredentials? credentials)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(
                proxyAddress,
                credentials != null ? "(credentials)" : null))
            {
                lock (this.configLock)
                {
                    WebRequest.DefaultWebProxy = new WebProxy(proxyAddress)
                    {
                        Credentials = credentials,
                        BypassList = bypassList.ToArray()
                    };
                }
            }
        }

        public void ActivateProxyAutoConfigSettings(
            Uri pacAddress,
            ICredentials? credentials)
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(
                pacAddress,
                credentials != null ? "(credentials)" : null))
            {
                lock (this.configLock)
                {
                    //
                    // NB. WinHTTP requires that the PAC script is delivered using 
                    // content type "application/x-ns-proxy-autoconfig" or that the
                    // URL has a file extension of .js, .pac, or .dat. Otherwise,
                    // the proxy script will be silently ignored.
                    //
                    // If the system already has a PAC address configured by group
                    // policy, then the script might also be silently ignored
                    // (in the Net trace, you'll see WinHTTP 10107 errors, but the
                    // API won't reveal any errors).
                    //
                    // The necessary properties are internal only. The only
                    // "official" way to populate them is via a DefaultProxySection,
                    // which is impractical in our case.
                    //

                    var scriptLocation = typeof(WebProxy).GetProperty(
                        "ScriptLocation",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (scriptLocation == null)
                    {
                        throw new InvalidOperationException(
                            "Failed to set auto proxy settings because " +
                            "properties could not be accessed");
                    }

                    var proxy = new WebProxy();
                    scriptLocation.SetValue(proxy, pacAddress);
                    proxy.Credentials = credentials;

                    WebRequest.DefaultWebProxy = proxy;
                }
            }
        }

        public void ActivateSystemProxySettings()
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
            {
                lock (this.configLock)
                {
                    WebRequest.DefaultWebProxy = defaultProxy;
                }
            }
        }

        public void ActivateSettings(IApplicationSettings settings)
        {
            NetworkCredential? GetProxyCredential()
            {
                if (!string.IsNullOrEmpty(settings.ProxyUsername.StringValue))
                {
                    return new NetworkCredential(
                        settings.ProxyUsername.StringValue,
                        (SecureString)settings.ProxyPassword.Value);
                }
                else
                {
                    return null;
                }
            }

            if (!string.IsNullOrEmpty(settings.ProxyUrl.StringValue))
            {
                ActivateCustomProxySettings(
                    new Uri(settings.ProxyUrl.StringValue),
                    Enumerable.Empty<string>(),
                    GetProxyCredential());
            }
            else if (!string.IsNullOrEmpty(settings.ProxyPacUrl.StringValue))
            {
                ActivateProxyAutoConfigSettings(
                    new Uri(settings.ProxyPacUrl.StringValue),
                    GetProxyCredential());
            }
            else
            {
                // No proxy set -> use system settings.
                ActivateSystemProxySettings();
            }
        }
    }
}
