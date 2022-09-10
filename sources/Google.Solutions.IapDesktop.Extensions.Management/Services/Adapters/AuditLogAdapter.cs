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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Logging.v2;
using Google.Apis.Logging.v2.Data;
using Google.Apis.Util;
using Google.Solutions.Common.ApiExtensions.Request;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Net;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Extensions.Management.Data.Events;
using Google.Solutions.IapDesktop.Extensions.Management.Data.Logs;
using Google.Solutions.IapDesktop.Extensions.Management.History;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Services.Adapters
{
    public interface IAuditLogAdapter
    {
        Task ProcessInstanceEventsAsync(
            IEnumerable<string> projectIds,
            IEnumerable<string> zones,
            IEnumerable<ulong> instanceIds,
            DateTime startTime,
            IEventProcessor processor,
            CancellationToken cancellationToken);
    }

    [Service(typeof(IAuditLogAdapter), ServiceLifetime.Transient)]
    public class AuditLogAdapter : IAuditLogAdapter
    {
        private const string MtlsBaseUri = "https://logging.mtls.googleapis.com/";

        private const int MaxPageSize = 1000;
        private const int MaxRetries = 10;
        private static readonly TimeSpan initialBackOff = TimeSpan.FromMilliseconds(100);

        private readonly LoggingService service;

        public bool IsDeviceCertiticateAuthenticationEnabled
            => this.service.IsMtlsEnabled() && this.service.IsClientCertificateProvided();

        public AuditLogAdapter(
            ICredential credential,
            IDeviceEnrollment deviceEnrollment)
        {
            this.service = new LoggingService(
                ClientServiceFactory.ForMtlsEndpoint(
                    credential,
                    deviceEnrollment,
                    MtlsBaseUri));

            Debug.Assert(
                (deviceEnrollment?.Certificate != null &&
                    HttpClientHandlerExtensions.IsClientCertificateSupported)
                    == IsDeviceCertiticateAuthenticationEnabled);
        }

        public AuditLogAdapter(ICredential credential)
            : this(credential, null)
        {
            // This constructor should only be used for test cases
            Debug.Assert(Globals.IsTestCase);
        }

        public AuditLogAdapter(IAuthorizationSource authService)
            : this(
                  authService.Authorization.Credential,
                  authService.Authorization.DeviceEnrollment)
        {
        }

        public AuditLogAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IAuthorizationSource>())
        {
        }

        internal async Task ListEventsAsync(
            ListLogEntriesRequest request,
            Action<EventBase> callback,
            ExponentialBackOff backOff,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(request.Filter))
            {
                try
                {
                    string nextPageToken = null;
                    do
                    {
                        request.PageToken = nextPageToken;

                        using (var stream = await this.service.Entries
                            .List(request)
                            .ExecuteAsStreamWithRetryAsync(backOff, cancellationToken)
                            .ConfigureAwait(false))
                        using (var reader = new JsonTextReader(new StreamReader(stream)))
                        {
                            nextPageToken = ListLogEntriesParser.Read(reader, callback);
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

        internal static string CreateFilterString(
            IEnumerable<string> zones,
            IEnumerable<ulong> instanceIds,
            IEnumerable<string> methods,
            IEnumerable<string> severities,
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
            Utilities.ThrowIfNull(projectIds, nameof(projectIds));

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(
                string.Join(", ", projectIds),
                startTime))
            {
                var request = new ListLogEntriesRequest()
                {
                    ResourceNames = projectIds.Select(p => "projects/" + p).ToList(),
                    Filter = CreateFilterString(
                        zones,
                        instanceIds,
                        processor.SupportedMethods,
                        processor.SupportedSeverities,
                        startTime),
                    PageSize = MaxPageSize,
                    OrderBy = "timestamp desc"
                };

                await ListEventsAsync(
                    request,
                    processor.Process,
                    new ExponentialBackOff(initialBackOff, MaxRetries),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
