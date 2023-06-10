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
        public void WhenNoSeedProvided_ThenFindPortReturnsRandomPort()
        {
            var portFinder = new PortFinder();

            var port1 = portFinder.FindPort(out var preferred);
            Assert.IsFalse(preferred);

            var port2 = portFinder.FindPort(out preferred);
            Assert.IsFalse(preferred);

            Assert.AreNotEqual(port1, port2);
        }

        //---------------------------------------------------------------------
        // FindPort - with seed.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSeedProvided_ThenFindPortReturnsSamePort()
        {
            var portFinder = new PortFinder();
            portFinder.AddSeed(Encoding.ASCII.GetBytes("some seed"));

            var port1 = portFinder.FindPort(out var preferred);

            if (!preferred)
            {
                Assert.Inconclusive();
            }

            var port2 = portFinder.FindPort(out preferred);
            Assert.IsTrue(preferred);

            Assert.AreEqual(port1, port2);
        }
    }
}
