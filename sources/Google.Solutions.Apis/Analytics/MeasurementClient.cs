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

using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Analytics
{
    /// <summary>
    /// Client for the Google Analytics Measurement API.
    /// 
    /// For details, see
    /// https://developers.google.com/analytics/devguides/collection/protocol/ga4/reference
    /// https://developer.chrome.com/docs/extensions/mv3/tut_analytics/
    /// </summary>
    public interface IMeasurementClient : IClient
    {
        /// <summary>
        /// Collect an event.
        /// </summary>
        /// <param name="eventName">
        ///   Event name. This must not be one of the reserved names defined in
        ///   https://developers.google.com/analytics/devguides/collection/protocol/ga4/reference
        /// </param>
        /// <param name="parameters">Event-specific parameters</param>
        Task CollectEventAsync(
            MeasurementSession session,
            string eventName,
            IDictionary<string, string> parameters,
            CancellationToken cancellationToken);
    }

    public class MeasurementClient : IMeasurementClient
    {
        private readonly MeasurementService service;

        public MeasurementClient(
            ServiceEndpoint<MeasurementClient> endpoint,
            UserAgent userAgent,
            string apiSecret,
            string measurementId)
        {
            this.Endpoint = endpoint;
            this.service = new MeasurementService(new MeasurementService.Initializer()
            {
                ApiKey = apiSecret.ExpectNotEmpty(nameof(apiSecret)),
                MeasurementId = measurementId.ExpectNotEmpty(nameof(measurementId)),
                ApplicationName = userAgent.ToApplicationName()
            });
        }

        public static ServiceEndpoint<MeasurementClient> CreateEndpoint()
        {
            return new ServiceEndpoint<MeasurementClient>(
                ServiceRoute.Public,
                MeasurementService.PublicEndpoint,
                null);
        }

        //---------------------------------------------------------------------
        // IClient.
        //---------------------------------------------------------------------

        public IServiceEndpoint Endpoint { get; }

        //---------------------------------------------------------------------
        // IAnalyticsMeasurementClient.
        //---------------------------------------------------------------------

        public async Task CollectEventAsync(
            MeasurementSession session,
            string eventName,
            IDictionary<string, string>? parameters,
            CancellationToken cancellationToken)
        {
            session.ExpectNotNull(nameof(session));
            eventName.ExpectNotEmpty(nameof(eventName));

            try
            {
                await this.service
                    .CollectAsync(
                    new MeasurementService.MeasurementRequest()
                    {
                        DebugMode = session.DebugMode,
                        ClientId = session.ClientId,
                        UserId = session.UserId,
                        UserProperties = session
                                .UserProperties
                                .EnsureNotNull()
                                .ToDictionary(
                                    kvp => kvp.Key,
                                    kvp => new MeasurementService.PropertySection(kvp.Value)),
                        Events = new[]
                            {
                                new MeasurementService.EventSection()
                                {
                                    Name = eventName,
                                    Parameters = parameters
                                        .EnsureNotNull()
                                        .Concat(session.GenerateParameters())
                                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                                }
                            }
                    },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new MeasurementFailedException(
                    "The event collection failed", e);
            }
        }
    }

    public class MeasurementFailedException : GoogleApiException
    {
        public MeasurementFailedException(string message)
            : base(typeof(MeasurementService).Name, message)
        {
        }

        public MeasurementFailedException(string message, Exception exception)
            : base(typeof(MeasurementService).Name, message, exception)
        {
        }
    }
}
