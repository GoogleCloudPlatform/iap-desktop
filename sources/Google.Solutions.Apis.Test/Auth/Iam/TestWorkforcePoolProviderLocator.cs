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

using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System;

namespace Google.Solutions.Apis.Test.Auth.Iam
{
    [TestFixture]
    public class TestWorkforcePoolProviderLocator
        : EquatableFixtureBase<WorkforcePoolProviderLocator, WorkforcePoolProviderLocator>
    {
        protected override WorkforcePoolProviderLocator CreateInstance()
        {
            return new WorkforcePoolProviderLocator("global", "pool", "provider");
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void WhenStringIsNullOrEmpty_ThenTryParseReturnsFalse(
            [Values(null, "")] string id)
        {
            Assert.IsFalse(WorkforcePoolProviderLocator.TryParse(id, out var _));
        }

        [Test]
        public void WhenStringIsMalformed_ThenTryParseReturnsFalse(
            [Values("x", "principal://", " ")] string id)
        {
            Assert.IsFalse(WorkforcePoolProviderLocator.TryParse(id, out var _));
        }

        [Test]
        public void WhenStringComponentIsNullOrEmpty_ThenTryParseReturnsFalse(
            [Values(
                "locations//workforcePools/POOL/providers/PROVIDER",
                "locations/LOCATION/workforcePools//providers/PROVIDER",
                "locations/LOCATION/workforcePools/POOL/providers/")]
            string id)
        {
            Assert.IsFalse(WorkforcePoolProviderLocator.TryParse(id, out var _));
        }

        [Test]
        public void WhenStringValid_ThenTryParseReturnsTrue()
        {
            Assert.IsTrue(WorkforcePoolProviderLocator.TryParse(
                "locations/LOCATION/workforcePools/POOL/providers/PROVIDER",
                out var locator));

            Assert.IsNotNull(locator);
            Assert.AreEqual("LOCATION", locator!.Location);
            Assert.AreEqual("POOL", locator.Pool);
            Assert.AreEqual("PROVIDER", locator.Provider);
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void WhenStringIsNullOrEmpty_ThenParseThrowsException(
            [Values(null, "")] string id)
        {
            Assert.Throws<ArgumentException>(
                () => WorkforcePoolProviderLocator.Parse(id));
        }

        [Test]
        public void WhenStringIsMalformed_ThenParseThrowsException(
            [Values("x", "principal://", " ")] string id)
        {
            Assert.Throws<ArgumentException>(
                () => WorkforcePoolProviderLocator.Parse(id));
        }

        [Test]
        public void WhenStringComponentIsNullOrEmpty_ThenParseThrowsException(
            [Values(
                "locations//workforcePools/POOL/providers/PROVIDER",
                "locations/LOCATION/workforcePools//providers/PROVIDER",
                "locations/LOCATION/workforcePools/POOL/providers/")]
            string id)
        {
            Assert.Throws<ArgumentException>(
                () => WorkforcePoolProviderLocator.Parse(id));
        }

        [Test]
        public void WhenStringValid_ThenParseSucceeds()
        {
            var locator = WorkforcePoolProviderLocator.Parse(
                "locations/LOCATION/workforcePools/POOL/providers/PROVIDER");

            Assert.IsNotNull(locator);
            Assert.AreEqual("LOCATION", locator!.Location);
            Assert.AreEqual("POOL", locator.Pool);
            Assert.AreEqual("PROVIDER", locator.Provider);
        }
    }
}
