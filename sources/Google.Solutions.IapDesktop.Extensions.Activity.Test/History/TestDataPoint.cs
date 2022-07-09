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

using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.History
{
    [TestFixture]
    public class TestDataPoint : ApplicationFixtureBase
    {
        private static readonly DateTime SampleDate = new DateTime(2020, 1, 2, 3, 4, 5, 6);

        [Test]
        public void WhenReferencesAreEquivalent_ThenEqualsReturnsTrue()
        {
            var ref1 = new DataPoint(SampleDate, 42);
            var ref2 = new DataPoint(SampleDate, 42);

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreEquivalent_ThenHashIsSame()
        {
            var ref1 = new DataPoint(SampleDate, 42);
            var ref2 = new DataPoint(SampleDate, 42);

            Assert.AreEqual(ref1.GetHashCode(), ref2.GetHashCode());
        }

        [Test]
        public void WhenReferencesAreSame_ThenEqualsReturnsTrue()
        {
            var ref1 = new DataPoint(SampleDate, 42);
            var ref2 = ref1;

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new DataPoint(SampleDate, 42);
            var ref2 = new DataPoint(SampleDate, 0);

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        [Test]
        public void TestEqualsNull()
        {
            var ref1 = new DataPoint(SampleDate, 42);

            Assert.IsFalse(ref1.Equals(null));
            Assert.IsFalse(ref1.Equals((object)null));
            Assert.IsFalse(ref1 == null);
            Assert.IsFalse(null == ref1);
            Assert.IsTrue(ref1 != null);
            Assert.IsTrue(null != ref1);
        }

        [Test]
        public void ToStringContainsValue()
        {
            var ref1 = new DataPoint(SampleDate, 42);

            StringAssert.Contains("42", ref1.ToString());
        }
    }
}
