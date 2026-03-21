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
using Google.Solutions.Common.IO;
using Google.Solutions.Common.Util;
using Google.Solutions.Platform.Net;
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
        private readonly IBrowser browser;
        private string? redirectUri;
        private readonly string path;
        private readonly string responseHtml;

        private static int portFindSeed = 1; // Any non-zero value is fine.
        private static readonly PortFinder portFinder = new PortFinder();

        public LoopbackCodeReceiver(
            IBrowser browser,
            string path,
            string responseHtml)
        {
            this.browser = browser.ExpectNotNull(nameof(browser));
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
            this.browser.Navigate(url);
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
                    //
                    // Amend seed so that we start the search with a different
                    // port each time we try to find a free port. This
                    // way, we ensure that:
                    //
                    // - The first time the receiver is used, we start with
                    //   a deterministic port.
                    // - The next time the receiver is used (which might be
                    //   after encountering a port conflict), we start with
                    //   a different port.
                    //
                    portFinder.AddSeed(BitConverter.GetBytes(portFindSeed++));

                    var port = portFinder.FindPort(out var _);
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
            const int ERROR_ACCESS_DENIED = 5;

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

                try
                {
                    listener.Start();
                }
                catch (HttpListenerException e)
                when (e.ErrorCode == ERROR_ACCESS_DENIED)
                {
                    ApiTraceSource.Log.TraceError(e);

                    //
                    // Invalidate the URI so that the next attempt uses a new port.
                    //
                    var deniedRedirectUri = this.RedirectUri;
                    this.redirectUri = null;

                    //
                    // This can happen if the endpoint overlaps with a persistent 
                    // port reservation.
                    //
                    throw new PortAccessDeniedException(deniedRedirectUri);
                }
                catch (Exception e)
                {
                    ApiTraceSource.Log.TraceError(e);
                    throw;
                }

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
