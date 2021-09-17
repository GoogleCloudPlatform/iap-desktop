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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapTunneling.Net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Net
{
    [TestFixture]
    public class TestInternalDns : IapFixtureBase
    {
        [Test]
        public void WhenNameIsZonalDnsName_ThenParseZonalDnsReturnsLocator()
        {
            InstanceLocator locator;
            Assert.IsTrue(InternalDns.TryParseZonalDns(
                "my-instance-1.us-central1-a.c.my-project.internal",
                out locator));

            Assert.AreEqual("my-instance-1", locator.Name);
            Assert.AreEqual("us-central1-a", locator.Zone);
            Assert.AreEqual("my-project", locator.ProjectId);
        }

        [Test]
        public void WhenNameIsUpperCaseZonalDnsName_ThenParseZonalDnsReturnsLocator()
        {
            InstanceLocator locator;
            Assert.IsTrue(InternalDns.TryParseZonalDns(
                "MY-INSTANCE-1.US-CENTRAL1-A.C.MY-PROJECT.INTERNAL",
                out locator));

            Assert.AreEqual("my-instance-1", locator.Name);
            Assert.AreEqual("us-central1-a", locator.Zone);
            Assert.AreEqual("my-project", locator.ProjectId);
        }

        [Test]
        public void WhenNameIsRegionalDnsName_ThenParseZonalDnsReturnsFalse()
        {
            InstanceLocator locator;
            Assert.IsFalse(InternalDns.TryParseZonalDns(
                "my-instance-1.c.my-project.internal",
                out locator));
        }

        [Test]
        public void WhenNameIsUnqualified_ThenParseZonalDnsReturnsFalse()
        {
            InstanceLocator locator;
            Assert.IsFalse(InternalDns.TryParseZonalDns(
                "my-instance-1",
                out locator));
        }
    }
}
