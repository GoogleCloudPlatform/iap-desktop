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
        // FromString.
        //---------------------------------------------------------------------

        [Test]
        public void WhenStringIsNullOrEmpty_ThenFromStringThrowsException(
            [Values(null, "")] string id)
        {
            Assert.Throws<ArgumentNullException>(
                () => WorkforcePoolProviderLocator.FromString(id));
        }

        [Test]
        public void WhenStringIsMalformed_ThenFromStringThrowsException(
            [Values("x", "principal://", " ")] string id)
        {
            Assert.Throws<ArgumentException>(
                () => WorkforcePoolProviderLocator.FromString(id));
        }

        [Test]
        public void WhenStringComponentIsNullOrEmpty_ThenFromStringThrowsException(
            [Values(
                "locations//workforcePools/POOL/providers/PROVIDER",
                "locations/LOCATION/workforcePools//providers/PROVIDER",
                "locations/LOCATION/workforcePools/POOL/providers/")]
            string id)
        {
            Assert.Throws<ArgumentNullException>(
                () => WorkforcePoolProviderLocator.FromString(id));
        }

        [Test]
        public void WhenStringValid_ThenFromStringSucceeds()
        {
            var locator = WorkforcePoolProviderLocator.FromString(
                "locations/LOCATION/workforcePools/POOL/providers/PROVIDER");

            Assert.AreEqual("LOCATION", locator.Location);
            Assert.AreEqual("POOL", locator.Pool);
            Assert.AreEqual("PROVIDER", locator.Provider);
        }
    }
}
