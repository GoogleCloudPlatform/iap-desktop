﻿//
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
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Test.Client
{
    [TestFixture]
    public class TestServiceRoute
    {
        //---------------------------------------------------------------------
        // Basic properties.
        //---------------------------------------------------------------------

        [Test]
        public void PublicRoute()
        {
            Assert.IsFalse(ServiceRoute.Public.UsePrivateServiceConnect);
            Assert.IsNull(ServiceRoute.Public.Endpoint);
            Assert.AreEqual("public", ServiceRoute.Public.ToString());
        }

        [Test]
        public void PscRoute()
        {
            var route = new ServiceRoute("www-endpoint.p.googleapis.com");
            Assert.IsTrue(route.UsePrivateServiceConnect);
            Assert.AreEqual("www-endpoint.p.googleapis.com", route.Endpoint);
            Assert.AreEqual("www-endpoint.p.googleapis.com", route.ToString());
        }

        //---------------------------------------------------------------------
        // Probe.
        //---------------------------------------------------------------------

        [Test]
        public async Task Probe_Public_RouteSucceeds()
        {
            await ServiceRoute.Public
                .ProbeAsync(TimeSpan.FromSeconds(5))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task Probe_WhenPscEndpointValid()
        {
            //
            // Use IP address as pseudo-PSC endpoint.
            //
            var address = await Dns
                .GetHostAddressesAsync("www.googleapis.com")
                .ConfigureAwait(false);

            var route = new ServiceRoute(address.First().ToString());

            await route
                .ProbeAsync(TimeSpan.FromSeconds(5))
                .ConfigureAwait(false);
        }

        [Test]
        public void Probe_WhenPscEndpointInvalid()
        {
            //
            // Use IP address as pseudo-PSC endpoint.
            //
            var route = new ServiceRoute("127.0.0.254");

            ExceptionAssert.ThrowsAggregateException<InvalidServiceRouteException>(
                () => route
                    .ProbeAsync(TimeSpan.FromSeconds(5))
                    .Wait());
        }
    }
}
