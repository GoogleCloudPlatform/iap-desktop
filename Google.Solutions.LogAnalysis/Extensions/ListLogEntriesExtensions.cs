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
using Google.Solutions.LogAnalysis.Events;
using Google.Solutions.LogAnalysis.History;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.LogAnalysis.Extensions
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
            ExponentialBackOff backOff)
        {
            // TODO: Trace

            string nextPageToken = null;
            do
            {
                request.PageToken = nextPageToken;

                using (var stream = await entriesResource
                    .List(request)
                    .ExecuteAsStreamWithRetryAsync(backOff))
                using (var reader = new JsonTextReader(new StreamReader(stream)))
                {
                    nextPageToken = ListLogEntriesParser.Read(reader, callback);
                }
            }
            while (nextPageToken != null);
        }

        public static async Task ListInstanceEventsAsync(
            this EntriesResource entriesResource,
            IEnumerable<string> projectIds,
            DateTime startTime,
            IEventProcessor processor)
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
                new ExponentialBackOff(initialBackOff, MaxRetries));
        }
    }
}
