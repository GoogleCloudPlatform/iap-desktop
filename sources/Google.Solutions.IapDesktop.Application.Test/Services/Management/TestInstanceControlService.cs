﻿//
// Copyright 2022 Google LLC
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
using Google.Apis.Compute.v1;
using Google.Apis.Services;
using Google.Solutions.Common.Locator;
using Google.Solutions.Testing.Common.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Solutions.Testing.Common;
using Google.Solutions.Testing.Application.Test;
using Google.Solutions.IapDesktop.Application.Services.Management;
using Moq;
using Google.Solutions.IapDesktop.Application.Services.Integration;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Management
{
    [TestFixture]
    public class TestInstanceControlService : ApplicationFixtureBase
    {
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // ControlInstance.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenCausingVmStart_ThenControlInstanceFiresEvent()
        {
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            var eventService = new Mock<IEventService>();

            var service = new InstanceControlService(
                computeEngineAdapter.Object,
                eventService.Object);

            await service.ControlInstanceAsync(
                    SampleLocator,
                    InstanceControlCommand.Start,
                    CancellationToken.None)
                .ConfigureAwait(false);

            eventService.Verify(s => s.FireAsync<InstanceStateChangedEvent>(
                It.Is<InstanceStateChangedEvent>(e => e.Instance == SampleLocator && e.IsRunning)),
                Times.Once);
        }

        [Test]
        public async Task WhenCausingVmStop_ThenControlInstanceFiresEvent()
        {
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            var eventService = new Mock<IEventService>();

            var service = new InstanceControlService(
                computeEngineAdapter.Object,
                eventService.Object);

            await service.ControlInstanceAsync(
                    SampleLocator,
                    InstanceControlCommand.Suspend,
                    CancellationToken.None)
                .ConfigureAwait(false);

            eventService.Verify(s => s.FireAsync<InstanceStateChangedEvent>(
                It.Is<InstanceStateChangedEvent>(e => e.Instance == SampleLocator && !e.IsRunning)),
                Times.Once);
        }
    }
}