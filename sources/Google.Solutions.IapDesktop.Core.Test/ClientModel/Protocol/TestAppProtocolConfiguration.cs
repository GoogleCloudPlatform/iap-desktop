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
using NUnit.Framework;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestAppProtocolConfiguration
    {

        //---------------------------------------------------------------------
        // ParseCondition.
        //---------------------------------------------------------------------

        [Test]
        public void WhenConditionIsNullOrEmpty_ThenParseConditionReturnsEmpty(
            [Values(" ", "", null)] string condition)
        {
            CollectionAssert.IsEmpty(
                AppProtocol.Configuration.ParseCondition(condition));
        }

        [Test]
        public void WhenConditionContainsSingleClause_ThenParseConditionReturnsTraits(
            [Values("isInstance()", " \nisInstance( )\r\n")] string condition)
        {
            var traits = AppProtocol.Configuration.ParseCondition(condition);
            CollectionAssert.IsNotEmpty(traits);

            Assert.IsTrue(traits.All(t => t is InstanceTrait));
        }

        [Test]
        public void WhenConditionContainsTwoClauses_ThenParseConditionReturnsTraits()
        {
            var traits = AppProtocol.Configuration.ParseCondition(
                "isInstance() && isInstance() ");
            CollectionAssert.IsNotEmpty(traits);

            Assert.AreEqual(2, traits.Count());
            Assert.IsTrue(traits.All(t => t is InstanceTrait));
        }

        [Test]
        public void WhenConditionContainsUnknownClause_ThenParseConditionThrowsException(
            [Values("isFoo()", " \nisInstance( ) && isBar\r\n")] string condition)
        {
            Assert.Throws<InvalidAppProtocolException>(
                () => AppProtocol.Configuration.ParseCondition(condition));
        }
    }
}
