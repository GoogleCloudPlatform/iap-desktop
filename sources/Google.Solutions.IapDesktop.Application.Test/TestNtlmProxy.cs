//
// Copyright 2024 Google LLC
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
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test
{
    [TestFixture]
    public class TestNtlmProxy
    {
        private static readonly Uri proxyAddress = null;
        private static readonly NetworkCredential proxyCredential =  null;

        [SetUp]
        public void Setup()
        {
            if (proxyAddress == null || proxyCredential == null)
            {
                Assert.Inconclusive("Proxy credentials not set");
            }

            var proxy = new HttpProxyAdapter();
            proxy.ActivateCustomProxySettings(
                proxyAddress,
                Enumerable.Empty<string>(),
                proxyCredential);
        }

        [Test]
        public async Task SequentialRequests()
        {
            var compute = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(ServiceRoute.Public),
                TestProject.AdminAuthorization,
                TestProject.UserAgent);

            for (int i = 0; i < 10; i++)
            {
                await compute
                    .ListInstancesAsync(
                        new ZoneLocator(TestProject.ProjectId, "us-central1-a"),
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        public static ComputeEngineClient _client;

        [Test]
        public async Task ParallelRequests()
        {
            var compute = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(ServiceRoute.Public),
                TestProject.AdminAuthorization,
                TestProject.UserAgent);
            _client = compute;
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(compute
                    .ListInstancesAsync(
                        new ZoneLocator(TestProject.ProjectId, "us-central1-a"),
                        CancellationToken.None));
            }

            await Task.WhenAll(tasks);
        }
    }
}
