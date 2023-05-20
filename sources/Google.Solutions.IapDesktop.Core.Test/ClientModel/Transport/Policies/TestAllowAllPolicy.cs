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


using Google.Solutions.Iap.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Transport.Policies
{
    [TestFixture]
    public class TestAllowAllPolicy
    {
        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToStringReturnsName()
        {
            Assert.AreEqual("Allow all", new AllowAllPolicy().ToString());
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOtherIsNull_ThenEqualsReturnsFalse()
        {
            var policy = new AllowAllPolicy();
            Assert.IsFalse(policy.Equals((object)null));
            Assert.IsFalse(((IEquatable<ITransportPolicy>)policy).Equals((ITransportPolicy)null));
            Assert.IsFalse(policy == (ISshRelayPolicy)null);
            Assert.IsTrue(policy != (ISshRelayPolicy)null);
        }

        [Test]
        public void WhenOtherIsOfDifferentType_ThenEqualsReturnsFalse()
        {
            var policy1 = new AllowAllPolicy();
            var policy2 = new Mock<ITransportPolicy>().Object;

            Assert.IsFalse(policy1.Equals(policy2));
            Assert.IsTrue((ITransportPolicy)policy1 != (ITransportPolicy)policy2);
        }

        [Test]
        public void WhenObjectsAreEquivalent_ThenEqualsReturnsTrue()
        {
            var policy1 = new AllowAllPolicy();
            var policy2 = new AllowAllPolicy();
            Assert.IsTrue(policy1.Equals(policy2));
            Assert.IsTrue(policy1 == policy2);
        }

        [Test]
        public void WhenObjectsAreSame_ThenEqualsReturnsTrue()
        {
            var policy1 = new AllowAllPolicy();
            var policy2 = policy1;
            Assert.IsTrue(policy1.Equals(policy2));
            Assert.IsTrue(policy1 == policy2);
        }
    }
}
