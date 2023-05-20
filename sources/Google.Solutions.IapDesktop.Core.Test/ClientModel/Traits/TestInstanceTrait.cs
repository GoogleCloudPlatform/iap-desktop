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


using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Traits
{
    [TestFixture]
    public class TestInstanceTrait
    {
        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToStringReturnsExpression()
        {
            Assert.AreEqual("isInstance()", new InstanceTrait().ToString());
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOtherIsNull_ThenEqualsReturnsFalse()
        {
            var trait = new InstanceTrait();
            Assert.IsFalse(trait.Equals((object)null));
            Assert.IsFalse(((IEquatable<IProtocolTargetTrait>)trait).Equals((IProtocolTargetTrait)null));
            Assert.IsFalse(trait == (IProtocolTargetTrait)null);
            Assert.IsTrue(trait != (IProtocolTargetTrait)null);
        }

        [Test]
        public void WhenOtherIsOfDifferentType_ThenEqualsReturnsFalse()
        {
            var trait1 = new InstanceTrait();
            var trait2 = new Mock<IProtocolTargetTrait>().Object;

            Assert.IsFalse(trait1.Equals(trait2));
        }

        [Test]
        public void WhenObjectsAreSame_ThenEqualsReturnsTrue()
        {
            var policy1 = new InstanceTrait();
            var policy2 = policy1;
            Assert.IsTrue(policy1.Equals(policy2));
            Assert.IsTrue(policy1 == policy2);
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void WhenExpressionIsNullOrEmpty_ThenTryParseReturnsFalse(
            [Values(" \t", "", null)] string expression)
        {
            Assert.IsFalse(InstanceTrait.TryParse(expression, out var _));
        }

        [Test]
        public void WhenExpressionIsValid_ThenTryParseReturnsTrue(
            [Values("isInstance()", " isInstance(  \n) \n\r\t ")] string expression)
        {
            Assert.IsTrue(InstanceTrait.TryParse(expression, out var trait));
            Assert.IsNotNull(trait);
        }
    }
}
