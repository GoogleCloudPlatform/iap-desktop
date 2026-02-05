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

using Google.Solutions.Iap.Net;
using NUnit.Framework;
using System.Text;

namespace Google.Solutions.Iap.Test.Net
{
    [TestFixture]
    public class TestPortFinder
    {
        //---------------------------------------------------------------------
        // FindPort - no seed.
        //---------------------------------------------------------------------

        [Test]
        public void FindPort_WhenNoSeedProvided()
        {
            var port1 = new PortFinder().FindPort(out var preferred);
            Assert.That(preferred, Is.False);

            var port2 = new PortFinder().FindPort(out preferred);
            Assert.That(preferred, Is.False);

            Assert.That(port2, Is.Not.EqualTo(port1));
        }

        //---------------------------------------------------------------------
        // FindPort - with seed.
        //---------------------------------------------------------------------

        [Test]
        public void FindPort_WhenSeedProvided()
        {
            var seed = Encoding.ASCII.GetBytes("some seed");

            var portFinder1 = new PortFinder();
            portFinder1.AddSeed(seed);
            var port1 = portFinder1.FindPort(out var preferred);

            if (!preferred)
            {
                Assert.Inconclusive();
            }

            var portFinder2 = new PortFinder();
            portFinder2.AddSeed(seed);
            var port2 = portFinder2.FindPort(out preferred);

            Assert.That(preferred, Is.True);
            Assert.That(port2, Is.EqualTo(port1));
        }
    }
}
