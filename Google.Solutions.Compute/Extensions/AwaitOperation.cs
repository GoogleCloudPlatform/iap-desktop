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
using Google.Apis.Compute.v1.Data;
using Google.Apis.Requests;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Google.Solutions.Compute.Extensions
{
    public static class AwaitOperation
    {
        private static string ShortIdFromUrl(string url) => url.Substring(url.LastIndexOf("/") + 1);

        private const string DoneStatus = "DONE";
        private static readonly List<SingleError> NoErrors = new List<SingleError>();

        public static async Task ExecuteAndAwaitOperationAsync(
            this IClientServiceRequest<Operation> request,
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
                                    Message = e.Message,
                                    Location = e.Location
                                })
                                .ToList()
                            : NoErrors;

                        TraceSources.Compute.TraceWarning("Operation failed: {0}", operation.HttpErrorMessage);

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
                        TraceSources.Compute.TraceVerbose("Operation completed");
                        return;
                    }
                }

                await Task.Delay(200);

                var pollRequest = new ZoneOperationsResource(request.Service).Get(
                    projectId,
                    ShortIdFromUrl(operation.Zone),
                    operation.Name);

                operation = await pollRequest
                    .ExecuteAsync(token)
                    .ConfigureAwait(false);
            }
        }
    }
}
