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
using Google.Solutions.Common.Test;
using NUnit.Framework;
using System;

namespace Google.Solutions.Apis.Test.Locator
{
    [TestFixture]
    public class TestOrganizationLocator : CommonFixtureBase
    {
        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPathIsValid_ParseReturnsObject()
        {
            var ref1 = OrganizationLocator.Parse(
                "organizations/12345678900001");

            Assert.AreEqual("organizations", ref1.ResourceType);
            Assert.AreEqual(12345678900001, ref1.Id);
        }

        [Test]
        public void WhenPathLacksOrganization_ParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => OrganizationLocator.Parse("/1"));
        }

        [Test]
        public void WhenPathInvalid_ParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => OrganizationLocator.Parse("x/1"));
            Assert.Throws<ArgumentException>(() => OrganizationLocator.Parse("1"));
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------


        [Test]
        public void WhenReferencesAreEquivalent_ThenEqualsReturnsTrue()
        {
            var ref1 = new OrganizationLocator(12345678900001);
            var ref2 = new OrganizationLocator(12345678900001);

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreEquivalent_ThenGetHashCodeIsSame()
        {
            var ref1 = new OrganizationLocator(12345678900001);
            var ref2 = new OrganizationLocator(12345678900001);

            Assert.AreEqual(ref1.GetHashCode(), ref2.GetHashCode());
        }

        [Test]
        public void WhenReferencesAreSame_ThenEqualsReturnsTrue()
        {
            var ref1 = new OrganizationLocator(12345678900001);
            var ref2 = ref1;

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new OrganizationLocator(12345678900001);
            var ref2 = new OrganizationLocator(12345678900002);

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        [Test]
        public void TestEqualsNull()
        {
            var ref1 = new OrganizationLocator(12345678900001);

            Assert.IsFalse(ref1.Equals(null));
            Assert.IsFalse(ref1!.Equals((object?)null));
            Assert.IsFalse(ref1 == null);
            Assert.IsFalse(null == ref1);
            Assert.IsTrue(ref1 != null);
            Assert.IsTrue(null != ref1);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "organizations/12345678900001";

            Assert.AreEqual(
                path,
                OrganizationLocator.Parse(path).ToString());
        }
    }
}
