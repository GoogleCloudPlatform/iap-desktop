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

using Google.Solutions.Apis.Locator;
using NUnit.Framework;

namespace Google.Solutions.Apis.Test.Locator
{
    [TestFixture]
    public class TestOrganizationLocator
        : TestLocatorFixtureBase<OrganizationLocator>
    {
        protected override OrganizationLocator CreateInstance()
        {
            return new OrganizationLocator(12345678900001);
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse_WhenPathIsValid_TryParseReturnsObject()
        {
            Assert.That(OrganizationLocator.TryParse(
                "organizations/12345678900001",
                out var ref1), Is.True);

            Assert.That(ref1, Is.Not.Null);
            Assert.That(ref1!.ResourceType, Is.EqualTo("organizations"));
            Assert.That(ref1.Id, Is.EqualTo(12345678900001));
        }

        [Test]
        public void TryParse_WhenPathLacksOrganization_TryParseThrowsArgumentException()
        {
            Assert.That(OrganizationLocator.TryParse("/1", out var _), Is.False);
        }

        [Test]
        public void TryParse_WhenPathInvalid_TryParseThrowsArgumentException(
            [Values("x/1", "organizations/", "organizations/0xxx")] string path)
        {
            Assert.That(OrganizationLocator.TryParse(path, out var _), Is.False);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "organizations/12345678900001";
            Assert.That(OrganizationLocator.TryParse(path, out var locator), Is.True);

            Assert.That(locator!.ToString(), Is.EqualTo(path));
        }
    }
}
