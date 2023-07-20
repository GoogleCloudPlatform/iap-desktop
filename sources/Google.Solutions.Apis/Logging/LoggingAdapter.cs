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

using Google.Apis.Logging.v2;
using Google.Apis.Logging.v2.Data;
using Google.Apis.Util;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using static Google.Solutions.Apis.Logging.LoggingAdapter;

namespace Google.Solutions.Apis.Logging
{
    /// <summary>
    /// Adapter for Cloud Logging API.
    /// </summary>
    public interface ILoggingAdapter
    {
        /// <summary>
        /// Read logs in reverse chronological order, page-by-page.
        /// </summary>
        Task ReadLogsAsync(
            IList<string> resourceNames,
            string filter,
            ReadPageCallback readPageCallback,
            CancellationToken cancellationToken);
    }

    public class LoggingAdapter : ILoggingAdapter
    {
        private const string MtlsBaseUri = "https://logging.mtls.googleapis.com/";

        private const int MaxPageSize = 1000;
        private const int MaxRetries = 10;
        private static readonly TimeSpan initialBackOff = TimeSpan.FromMilliseconds(100);

        private readonly LoggingService service;

        /// <summary>
        /// Read a page of results.
        /// </summary>
        /// <returns>next page token</returns>
        public delegate string ReadPageCallback(Stream stream);

        public LoggingAdapter(
            IAuthorization authorization,
            UserAgent userAgent)
        {
            authorization.ExpectNotNull(nameof(authorization));
            userAgent.ExpectNotNull(nameof(userAgent));

            this.service = new LoggingService(
                new AuthorizedClientInitializer(
                    authorization,
                    userAgent,
                    MtlsBaseUri));
        }

        public async Task ReadLogsAsync(
            IList<string> resourceNames,
            string filter,
            ReadPageCallback readPageCallback,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSources.Default.TraceMethod().WithParameters(filter))
            {
                var backoff = new ExponentialBackOff(initialBackOff, MaxRetries);
                var request = new ListLogEntriesRequest()
                {
                    ResourceNames = resourceNames,
                    Filter = filter,
                    PageSize = MaxPageSize,
                    OrderBy = "timestamp desc"
                };

                try
                {
                    string nextPageToken = null;
                    do
                    {
                        request.PageToken = nextPageToken;

                        using (var stream = await this.service.Entries
                            .List(request)
                            .ExecuteAsStreamWithRetryAsync(backoff, cancellationToken)
                            .ConfigureAwait(false))
                        {
                            nextPageToken = readPageCallback(stream);
                        }
                    }
                    while (nextPageToken != null);
                }
                catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 403)
                {
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to view logs. " +
                        "You need the 'Logs Viewer' role (or an equivalent custom role) " +
                        "to perform this action.",
                        e);
                }
            }
        }
    }
}
