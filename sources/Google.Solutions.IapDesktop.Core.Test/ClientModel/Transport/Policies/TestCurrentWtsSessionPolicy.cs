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

using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System.Diagnostics;
using System.Net;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Transport.Policies
{
    [TestFixture]
    public class TestCurrentWtsSessionPolicy
        : EquatableFixtureBase<ProcessPolicyBase, ITransportPolicy>
    {
        protected override ProcessPolicyBase CreateInstance()
        {
            return new CurrentWtsSessionPolicy();
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_ReturnsName()
        {
            Assert.That(new CurrentWtsSessionPolicy().ToString(), Is.EqualTo("Current WTS session"));
        }

        //---------------------------------------------------------------------
        // IsClientAllowed.
        //---------------------------------------------------------------------

        [Test]
        public void IsClientAllowed_WhenEndpointNotLoopback()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 1111);
            var policy = new CurrentWtsSessionPolicy();

            Assert.IsFalse(policy.IsClientAllowed(endpoint));
        }

        [Test]
        public void IsClientAllowed_WhenEndpointBelongsToDifferentProcess()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 445);
            var policy = new CurrentWtsSessionPolicy();

            Assert.IsFalse(policy.IsClientAllowed(endpoint));
        }

        //---------------------------------------------------------------------
        // IsClientProcessAllowed.
        //---------------------------------------------------------------------

        [Test]
        public void IsClientProcessAllowed_WhenProcessIdCurrent()
        {
            var pid = (uint)Process.GetCurrentProcess().Id;
            Assert.IsTrue(new CurrentWtsSessionPolicy().IsClientProcessAllowed(pid));
        }

        [Test]
        public void IsClientProcessAllowed_WhenProcessIdNotFound()
        {
            Assert.IsFalse(new CurrentWtsSessionPolicy().IsClientProcessAllowed(uint.MaxValue));
        }
    }
}
