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

using Google.Apis.Logging.v2;
using Google.Apis.Logging.v2.Data;
using Google.Apis.Util;
using Google.Solutions.Common.ApiExtensions.Request;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.Events;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Logs
{
    public static class ListLogEntriesExtensions
    {
        private const int MaxPageSize = 1000;
        private const int MaxRetries = 10;
        private static readonly TimeSpan initialBackOff = TimeSpan.FromMilliseconds(100);

        public static async Task ListEventsAsync(
            this EntriesResource entriesResource,
            ListLogEntriesRequest request,
            Action<EventBase> callback,
            ExponentialBackOff backOff,
            CancellationToken cancellationToken)
        {
            using (TraceSources.LogAnalysis.TraceMethod().WithParameters(request.Filter))
            {
                try
                {
                    string nextPageToken = null;
                    do
                    {
                        request.PageToken = nextPageToken;

                        using (var stream = await entriesResource
                            .List(request)
                            .ExecuteAsStreamWithRetryAsync(backOff, cancellationToken))
                        using (var reader = new JsonTextReader(new StreamReader(stream)))
                        {
                            nextPageToken = ListLogEntriesParser.Read(reader, callback);
                        }
                    }
                    while (nextPageToken != null);
                }
                catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 403)
                {
                    throw new AuditLogException(
                        $"Access to audit logs has been denied", e);
                }
            }
        }

        public static async Task ListInstanceEventsAsync(
            this EntriesResource entriesResource,
            IEnumerable<string> projectIds,
            DateTime startTime,
            IEventProcessor processor,
            CancellationToken cancellationToken)
        {
            using (TraceSources.LogAnalysis.TraceMethod().WithParameters(
                string.Join(", ", projectIds), 
                startTime))
            {
                var request = new ListLogEntriesRequest()
                {
                    ResourceNames = projectIds.Select(p => "projects/" + p).ToList(),
                    Filter = $"protoPayload.methodName=(\"{string.Join("\" OR \"", processor.SupportedMethods)}\") " +
                        $"AND severity=(\"{string.Join("\" OR \"", processor.SupportedSeverities)}\") " +
                        $"AND resource.type=\"gce_instance\" " +
                        $"AND timestamp > {startTime:yyyy-MM-dd} ",
                    PageSize = MaxPageSize,
                    OrderBy = "timestamp desc"
                };

                await ListEventsAsync(
                    entriesResource,
                    request,
                    processor.Process,
                    new ExponentialBackOff(initialBackOff, MaxRetries),
                    cancellationToken);
            }
        }
    }

    [Serializable]
    public class AuditLogException : Exception
    {
        protected AuditLogException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public AuditLogException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
