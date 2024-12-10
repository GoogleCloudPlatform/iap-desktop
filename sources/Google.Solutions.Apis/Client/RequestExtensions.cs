//
// Copyright 2019 Google LLC
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

using Google.Apis.Compute.v1;
using Google.Apis.Requests;
using Google.Apis.Util;
using Google.Solutions.Common;
using Google.Solutions.Common.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Client
{
    public static class RequestExtensions
    {
        private static string ShortIdFromUrl(string url) => url.Substring(url.LastIndexOf("/") + 1);

        private const string DoneStatus = "DONE";
        private static readonly List<SingleError> NoErrors = new List<SingleError>();

        //---------------------------------------------------------------------
        // Extension methods for executing requests and reading the response
        // as stream.
        //---------------------------------------------------------------------

        /// <summary>
        /// Like ExecuteAsStream, but catch non-success HTTP codes and convert them
        /// into an exception.
        /// </summary>
        public static async Task<Stream> ExecuteAsStreamOrThrowAsync<TResponse>(
            this IClientServiceRequest<TResponse> request,
            CancellationToken cancellationToken)
        {
            using (var httpRequest = request.CreateRequest())
            {
                var httpResponse = await request.Service.HttpClient
                    .SendAsync(httpRequest, cancellationToken)
                    .ConfigureAwait(false);

                // NB. ExecuteAsStream does not do this check.
                if (!httpResponse.IsSuccessStatusCode)
                {
                    var error = await request.Service
                        .DeserializeError(httpResponse)
                        .ConfigureAwait(false);

                    throw new GoogleApiException(request.Service.Name, error.ToString())
                    {
                        Error = error,
                        HttpStatusCode = httpResponse.StatusCode
                    };
                }

                cancellationToken.ThrowIfCancellationRequested();
                return await httpResponse.Content
                    .ReadAsStreamAsync()
                    .ConfigureAwait(false);
            }
        }

        public static async Task<Stream> ExecuteAsStreamWithRetryAsync<TResponse>(
            this IClientServiceRequest<TResponse> request,
            ExponentialBackOff backOff,
            CancellationToken cancellationToken)
        {
            var retries = 0;
            while (true)
            {
                try
                {
                    return await request
                        .ExecuteAsStreamOrThrowAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e)
                    when (e.Error != null && (e.Error.Code == 429 || e.Error.Code == 500))
                {
                    //
                    // Too many requests.
                    //
                    if (retries < backOff.MaxNumOfRetries)
                    {
                        CommonTraceSource.Log.TraceWarning(
                            "Too many requests - backing of and retrying...", retries);

                        retries++;
                        await Task
                            .Delay(backOff.GetNextBackOff(retries))
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        //
                        // Retried too often already.
                        //
                        CommonTraceSource.Log.TraceWarning("Giving up after {0} retries", retries);
                        throw;
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Extension methods for requests that return an operation.
        //---------------------------------------------------------------------

        public static async Task ExecuteAndAwaitOperationAsync(
            this IClientServiceRequest<Google.Apis.Compute.v1.Data.Operation> request,
            string projectId,
            CancellationToken token)
        {
            var operation = await request.ExecuteAsync(token).ConfigureAwait(false);

            while (true)
            {
                token.ThrowIfCancellationRequested();

                if (operation.Status == DoneStatus)
                {
                    if (operation.HttpErrorStatusCode >= 400)
                    {
                        // Operation failed. Translate error details into an 
                        // exception.

                        var errors = operation.Error != null && operation.Error.Errors != null
                            ? operation.Error.Errors
                                .Select(e => new SingleError()
                                {
                                    //
                                    // e.Message typically contains a readable error message
                                    // that should be displayed to the user.
                                    //
                                    // This is different from non-Operation API calls where
                                    // Message typically contains a concatenated mess.
                                    //
                                    // To avoid losing the message here, map it to Reason.
                                    //
                                    Message = e.Code,
                                    Reason = e.Message,
                                    Location = e.Location
                                })
                                .ToList()
                            : NoErrors;

                        CommonTraceSource.Log.TraceWarning("Operation failed: {0}", operation.HttpErrorMessage);

                        throw new GoogleApiException(
                            "ComputeEngine",
                            operation.HttpErrorMessage)
                        {
                            Error = new RequestError()
                            {
                                Code = operation.HttpErrorStatusCode ?? 0,
                                Message = operation.HttpErrorMessage,
                                Errors = errors
                            }
                        };
                    }
                    else
                    {
                        CommonTraceSource.Log.TraceVerbose("Operation completed");
                        return;
                    }
                }

                await Task.Delay(200).ConfigureAwait(false);

                if (operation.Zone != null)
                {
                    // Zonal operation.
                    operation = await new ZoneOperationsResource(request.Service)
                        .Get(
                            projectId,
                            ShortIdFromUrl(operation.Zone),
                            operation.Name)
                        .ExecuteAsync(token)
                        .ConfigureAwait(false);
                }
                else
                {
                    // Global operation.
                    operation = await new GlobalOperationsResource(request.Service)
                        .Get(projectId, operation.Name)
                        .ExecuteAsync(token)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
