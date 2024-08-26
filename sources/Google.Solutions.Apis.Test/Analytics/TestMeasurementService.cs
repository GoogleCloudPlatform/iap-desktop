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

using Google.Solutions.Apis.Analytics;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Analytics
{
    [TestFixture]
    public class TestMeasurementService
    {
        //---------------------------------------------------------------------
        // Collect.
        //---------------------------------------------------------------------

        [Test]
        public async Task Collect_WhenClientIdMissingAndDebugModeIsOff()
        {
            var service = new MeasurementService(new MeasurementService.Initializer()
            {
                ApiKey = "invalid-key",
                MeasurementId = "invalid"
            });

            await service
                .CollectAsync(
                    new MeasurementService.MeasurementRequest()
                    {
                        DebugMode = false,
                        ClientId = string.Empty,
                        Events = new[]
                        {
                            new MeasurementService.EventSection()
                            {
                                Name = "click",
                            }
                        }
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        [Test]
        public void Collect_WhenClientIdMissingAndDebugModeIsOn()
        {
            var service = new MeasurementService(new MeasurementService.Initializer()
            {
                ApiKey = "invalid-key",
                MeasurementId = "invalid"
            });

            ExceptionAssert.ThrowsAggregateException<GoogleApiException>(
                "client_id",
                () => service
                    .CollectAsync(
                        new MeasurementService.MeasurementRequest()
                        {
                            DebugMode = true,
                            ClientId = string.Empty,
                            Events = new[]
                            {
                                new MeasurementService.EventSection()
                                {
                                    Name = "click",
                                }
                            }
                        },
                        CancellationToken.None)
                    .Wait());
        }
    }
}
