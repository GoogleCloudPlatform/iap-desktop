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
            Assert.That(ServiceRoute.Public.UsePrivateServiceConnect, Is.False);
            Assert.That(ServiceRoute.Public.Endpoint, Is.Null);
            Assert.That(ServiceRoute.Public.ToString(), Is.EqualTo("public"));
        }

        [Test]
        public void PscRoute()
        {
            var route = new ServiceRoute("www-endpoint.p.googleapis.com");
            Assert.That(route.UsePrivateServiceConnect, Is.True);
            Assert.That(route.Endpoint, Is.EqualTo("www-endpoint.p.googleapis.com"));
            Assert.That(route.ToString(), Is.EqualTo("www-endpoint.p.googleapis.com"));
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
        public async Task Probe_WhenPscEndpointInvalid()
        {
            //
            // Use IP address as pseudo-PSC endpoint.
            //
            var route = new ServiceRoute("127.0.0.254");

            await ExceptionAssert
                .ThrowsAsync<InvalidServiceRouteException>(() => route
                    .ProbeAsync(TimeSpan.FromSeconds(5)))
                .ConfigureAwait(false);
        }
    }
}
