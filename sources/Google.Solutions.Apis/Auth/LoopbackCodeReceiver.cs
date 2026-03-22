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
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

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

        /// <summary>
        /// Create a AuthorizationCodeResponseUrl from a collection of query 
        /// parameters.
        /// </summary>
        private static AuthorizationCodeResponseUrl CreateResponseUrl(
            NameValueCollection queryParameters)
        {
            return new AuthorizationCodeResponseUrl(
                queryParameters
                    .AllKeys
                    .Where(k => k != null) // k is null if there's no equal sign.
                    .ToDictionary(k => k, k => queryParameters[k]));
        }

        protected virtual void OpenBrowser(string url)
        {
            this.browser.Navigate(url);
        }

        private async Task<AuthorizationCodeResponseUrl> ReceiveCodeUsingHttpSysAsync(
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
                    "Start HTTP listener for {0}...", this.RedirectUri);

                listener.Prefixes.Add(this.RedirectUri);

                //
                // Attempt to listen on the port we previously chose. This may
                // fail, for example because of a persistent port reservation.
                //
                try
                {
                    listener.Start();
                }
                catch (HttpListenerException e)
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

                //
                // Open browser to kick off the authorization flow.
                //
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

                            return CreateResponseUrl(queryParameters);
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

        private async Task<AuthorizationCodeResponseUrl> ReceiveCodeUsingSimpleServerAsync(
            AuthorizationCodeRequestUrl url,
            CancellationToken cancellationToken)
        {
            var authorizationUrl = url.Build().AbsoluteUri;

            //
            // Start a simple HTTP Server and attempt to listen on the
            // port we previously chose. This may fail, for example
            // because of a persistent port reservation.
            //
            ApiTraceSource.Log.TraceVerbose(
                "Start TCP listener for {0}...", this.RedirectUri);

            var redirectUri = new Uri(this.RedirectUri);
            var listener = new TcpListener(IPAddress.Loopback, redirectUri.Port);

            using (cancellationToken.Register(listener.Stop))
            {
                try
                {
                    listener.Start();
                }
                catch (SocketException e)
                {
                    ApiTraceSource.Log.TraceError(e);
                    throw new PortAccessDeniedException(listener.LocalEndpoint);
                }
                catch (Exception e)
                {
                    ApiTraceSource.Log.TraceError(e);
                    throw;
                }

                try
                {
                    //
                    // Open browser to kick off the authorization flow.
                    //
                    ApiTraceSource.Log.TraceVerbose(
                        "Open a browser for {0}...", authorizationUrl);

                    OpenBrowser(authorizationUrl);

                    //
                    // Listen for responses.
                    //
                    while (true)
                    {
                        using (var client = await listener
                            .AcceptTcpClientAsync()
                            .ConfigureAwait(false))
                        using (var stream = client.GetStream())
                        using (var writer = new StreamWriter(stream, new UTF8Encoding(false))
                        {
                            AutoFlush = true
                        })
                        {
                            var buffer = new byte[4096];
                            int bytesRead = await stream
                                .ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                                .ConfigureAwait(false);

                            if (bytesRead == 0)
                            {
                                //
                                // No data received, ignore.
                                //
                                continue;
                            }

                            //
                            // Parse the request line.
                            //
                            var parts = Encoding.UTF8
                                .GetString(buffer, 0, bytesRead)
                                .Split(new[] { "\r\n" }, StringSplitOptions.None)[0]
                                .Split(' ');
                            if (parts.Length != 3 ||
                                !string.Equals(
                                    parts[0], 
                                    "GET", 
                                    StringComparison.OrdinalIgnoreCase) ||
                                !parts[1].StartsWith("/") ||
                                !string.Equals(
                                    parts[2], 
                                    "HTTP/1.1", 
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                //
                                // Malformed request, ignore.
                                //
                                ApiTraceSource.Log.TraceWarning(
                                    "Received malformed HTTP request");
                                continue;
                            }

                            var requestUrl = new Uri(redirectUri, parts[1]);
                            var requestParameters = HttpUtility.ParseQueryString(requestUrl.Query);

                            if (string.Equals(
                                this.path,
                                requestUrl.AbsolutePath, 
                                StringComparison.Ordinal) &&
                                requestParameters.Count > 0)
                            {
                                //
                                // Respond with the provided page.
                                //

                                var responseBytes = Encoding.UTF8.GetBytes(
                                    this.responseHtml);
                                await writer
                                    .WriteAsync(
                                        $"HTTP/1.1 200 OK\r\n" +
                                        $"Content-Type: text/html; charset=UTF-8\r\n" +
                                        $"Content-Length: {responseBytes.Length}\r\n" +
                                        $"Connection: close\r\n\r\n" +
                                        $"{this.responseHtml}")
                                    .ConfigureAwait(false);

                                return CreateResponseUrl(requestParameters);
                            }
                            else
                            {
                                //
                                // Not the expected path.
                                //
                                await writer
                                    .WriteAsync(
                                        "HTTP/1.1 404 Not Found\r\n" +
                                        "Content-Length: 0\r\n" +
                                        "Connection: close\r\n\r\n")
                                    .ConfigureAwait(false); 
                            }
                        }
                    }
                }
                catch (InvalidOperationException) 
                when (cancellationToken.IsCancellationRequested)
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
                finally
                {
                    try
                    {
                        listener.Stop();
                    }
                    catch (Exception e)
                    {
                        ApiTraceSource.Log.TraceError(e);
                    }
                }
            }
        }

        /// <summary>
        /// Use platform-provided HTTP server (http.sys) to receive the 
        /// authorization code. If
        /// </summary>
        public bool UseHttpSys { get; set; } = true;

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

        public Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(
            AuthorizationCodeRequestUrl url,
            CancellationToken cancellationToken)
        {
            if (this.UseHttpSys)
            {
                return ReceiveCodeUsingHttpSysAsync(url, cancellationToken);
            }
            else
            {
                return ReceiveCodeUsingSimpleServerAsync(url, cancellationToken);
            }
        }
    }
}
