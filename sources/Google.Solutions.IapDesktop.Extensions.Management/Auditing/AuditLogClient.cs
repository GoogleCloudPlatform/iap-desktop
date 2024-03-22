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

using Google.Solutions.Apis.Logging;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using Google.Solutions.IapDesktop.Extensions.Management.History;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management
{
    public interface IAuditLogClient
    {
        Task ProcessInstanceEventsAsync(
            IEnumerable<string> projectIds,
            IEnumerable<string> zones,
            IEnumerable<ulong> instanceIds,
            DateTime startTime,
            IEventProcessor processor,
            CancellationToken cancellationToken);
    }

    [Service(typeof(IAuditLogClient), ServiceLifetime.Singleton)]
    public class AuditLogClient : IAuditLogClient
    {
        private readonly ILoggingClient client;

        public AuditLogClient(ILoggingClient client)
        {
            this.client = client.ExpectNotNull(nameof(client));
        }

        internal static string CreateFilterString(
            IEnumerable<string>? zones,
            IEnumerable<ulong>? instanceIds,
            IEnumerable<string>? methods,
            IEnumerable<string>? severities,
            DateTime startTime)
        {
            Debug.Assert(startTime.Kind == DateTimeKind.Utc);

            var criteria = new LinkedList<string>();

            //
            // NB. OSLogin logs have the zone and instance_id at the top level:
            //
            // "labels": {
            //   "zone": "europe-west4-a",
            //   "instance_id": "1234567890"
            // }
            //
            // All other logs have these fields under resource.labels.
            //

            if (zones != null && zones.Any())
            {
                var zonesClause = string.Join("\" OR \"", zones);
                criteria.AddLast($"(resource.labels.zone=(\"{zonesClause}\") OR labels.zone=(\"{zonesClause}\"))");
            }

            if (instanceIds != null && instanceIds.Any())
            {
                var instanceIdsClause = string.Join("\" OR \"", instanceIds);
                criteria.AddLast($"(resource.labels.instance_id=(\"{instanceIdsClause}\") OR labels.instance_id=(\"{instanceIdsClause}\"))");
            }

            if (methods != null && methods.Any())
            {
                criteria.AddLast($"protoPayload.methodName=(\"{string.Join("\" OR \"", methods)}\")");
            }

            if (severities != null && severities.Any())
            {
                criteria.AddLast($"severity=(\"{string.Join("\" OR \"", severities)}\")");
            }

            // NB. Some instance-related events use project scope, for example
            // setCommonInstanceMetadata events.
            criteria.AddLast($"resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\")");
            criteria.AddLast($"timestamp > \"{startTime:o}\"");

            return string.Join(" AND ", criteria);
        }

        //---------------------------------------------------------------------
        // IAuditLogAdapter
        //---------------------------------------------------------------------

        public async Task ProcessInstanceEventsAsync(
            IEnumerable<string> projectIds,
            IEnumerable<string> zones,
            IEnumerable<ulong> instanceIds,
            DateTime startTime,
            IEventProcessor processor,
            CancellationToken cancellationToken)
        {
            projectIds.ExpectNotNull(nameof(projectIds));

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(
                string.Join(", ", projectIds),
                startTime))
            {
                await this.client
                    .ReadLogsAsync(
                        projectIds.Select(p => "projects/" + p).ToList(),
                        CreateFilterString(
                            zones,
                            instanceIds,
                            processor.SupportedMethods,
                            processor.SupportedSeverities,
                            startTime),
                        stream =>
                        {
                            using (var reader = new JsonTextReader(new StreamReader(stream)))
                            {
                                return ListLogEntriesParser.Read(reader, processor.Process);
                            }
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
