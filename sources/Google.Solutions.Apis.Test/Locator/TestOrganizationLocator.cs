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
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System;

namespace Google.Solutions.Apis.Test.Locator
{
    [TestFixture]
    public class TestOrganizationLocator
        : EquatableFixtureBase<OrganizationLocator, OrganizationLocator>
    {
        protected override OrganizationLocator CreateInstance()
        {
            return new OrganizationLocator(12345678900001);
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenPathIsValid_ParseReturnsObject()
        {
            var ref1 = OrganizationLocator.Parse(
                "organizations/12345678900001");

            Assert.AreEqual("organizations", ref1.ResourceType);
            Assert.AreEqual(12345678900001, ref1.Id);
        }

        [Test]
        public void Parse_WhenPathLacksOrganization_ParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => OrganizationLocator.Parse("/1"));
        }

        [Test]
        public void Parse_WhenPathInvalid_ParseThrowsArgumentException(
            [Values("x/1", "organizations/", "organizations/0xxx")] string path)
        {
            Assert.Throws<ArgumentException>(() => OrganizationLocator.Parse(path));
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "organizations/12345678900001";

            Assert.AreEqual(
                path,
                OrganizationLocator.Parse(path).ToString());
        }
    }
}
