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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth
{
    /// <summary>
    /// Code receiver that runs a local server on a free port and
    /// waits for a call with the authorization verification code.
    /// 
    /// Unlike LocalServerCodeReceiver, this implementation supports
    /// custom redirect paths.
    /// </summary>
    public class LoopbackCodeReceiver : ICodeReceiver
    {
        private string? redirectUri;
        private readonly string path;
        private readonly string responseHtml;

        public LoopbackCodeReceiver(
            string path,
            string responseHtml)
        {
            this.path = path.ExpectNotEmpty(nameof(path));
            this.responseHtml = responseHtml.ExpectNotEmpty(nameof(responseHtml));

            if (!path.EndsWith("/"))
            {
                //
                // The path must end with a trailing slash for the
                // listener prefix to work correctly.
                //
                throw new ArgumentException("Path must end with a trailing slash");
            }
        }

        protected virtual void OpenBrowser(string url)
        {
            Process.Start(url);
        }

        /// <summary>
        /// Return a random, unused port.
        /// </summary>
        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        //---------------------------------------------------------------------
        // ICodeReceiver.
        //---------------------------------------------------------------------

        public string RedirectUri
        {
            get
            {
                if (this.redirectUri == null ||
                    string.IsNullOrEmpty(this.redirectUri))
                {
                    var port = GetRandomUnusedPort();
                    this.redirectUri = new UriBuilder()
                    {
                        Scheme = "http",
                        Port = port,
                        Path = this.path
                    }.Uri.ToString();
                }

                return this.redirectUri;
            }
        }

        public async Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(
            AuthorizationCodeRequestUrl url,
            CancellationToken cancellationToken)
        {
            var authorizationUrl = url.Build().AbsoluteUri;

            //
            // NB. HttpListener.GetContextAsync() doesn't accept a
            // cancellation token, so the HttpListener needs to be stopped
            // to abort the GetContextAsync() call.
            //
            using (var listener = new HttpListener())
            using (cancellationToken.Register(listener.Stop))
            {
                ApiTraceSource.Log.TraceVerbose(
                    "Start listener for {0}...", this.RedirectUri);

                listener.Prefixes.Add(this.RedirectUri);
                listener.Start();

                ApiTraceSource.Log.TraceVerbose(
                    "Open a browser for {0}...", authorizationUrl);

                OpenBrowser(authorizationUrl);

                try
                {
                    while (true)
                    {
                        var context = await listener
                            .GetContextAsync()
                            .ConfigureAwait(false);

                        if (!this.path.Equals(
                            context.Request.Url.AbsolutePath,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            //
                            // Different path (for ex, /favicon.ico). Ignore.
                            //

                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            context.Response.Close();
                        }
                        else if (context.Request.QueryString.Keys.Count == 0)
                        {
                            //
                            // Can't possibly be an OAuth response. Ignore.
                            //
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            context.Response.Close();
                        }
                        else
                        {
                            var queryParameters = context.Request.QueryString;

                            //
                            // This looks like an OAuth response. Generate a response
                            // to show in the user's browser.
                            //
                            var bytes = Encoding.UTF8.GetBytes(this.responseHtml);

                            context.Response.ContentLength64 = bytes.Length;
                            context.Response.SendChunked = false;
                            context.Response.KeepAlive = false;

                            var output = context.Response.OutputStream;
                            await output
                                .WriteAsync(bytes, 0, bytes.Length, CancellationToken.None)
                                .ConfigureAwait(false);
                            await output
                                .FlushAsync(CancellationToken.None)
                                .ConfigureAwait(false);
                            output.Close();
                            context.Response.Close();

                            //
                            // Create a new response URL with a dictionary that contains
                            // all the response query parameters.
                            //
                            return new AuthorizationCodeResponseUrl(
                                queryParameters
                                    .AllKeys
                                    .Where(k => k != null) // k is null if there's no equal sign.
                                    .ToDictionary(k => k, k => queryParameters[k]));
                        }
                    }
                }
                catch (Exception) when (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    //
                    // Next line will never be reached because cancellation will
                    // always have been requested in this catch block.
                    // But it's required to satisfy compiler.
                    //
                    throw new InvalidOperationException();
                }
                catch (Exception e)
                {
                    ApiTraceSource.Log.TraceError(e);
                    throw;
                }
            }
        }
    }
}
