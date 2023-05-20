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
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Session
{
    [TestFixture]
    public class TestRdpProtocol
    {
        //---------------------------------------------------------------------
        // IsAvailable.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTargetHasNoTraits_ThenIsAvailableReturnsFalse()
        {
            var target = new Mock<IProtocolTarget>();
            target
                .Setup(t => t.Traits)
                .Returns((IEnumerable<IProtocolTargetTrait>)null);

            Assert.IsFalse(RdpProtocol.Protocol.IsAvailable(target.Object));
        }

        [Test]
        public void WhenTargetHasTrait_ThenIsAvailableReturnsTrue()
        {
            var target = new Mock<IProtocolTarget>();
            target
                .Setup(t => t.Traits)
                .Returns(new[] { new WindowsTrait() });

            Assert.IsTrue(RdpProtocol.Protocol.IsAvailable(target.Object));
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToStringReturnsName()
        {
            Assert.AreEqual("RDP", RdpProtocol.Protocol.ToString());
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOtherIsNull_ThenEqualsReturnsFalse()
        {
            var protocol = RdpProtocol.Protocol;
            Assert.IsFalse(protocol.Equals((object)null));
            Assert.IsFalse(((IEquatable<IProtocol>)protocol).Equals((IProtocol)null));
            Assert.IsFalse(protocol == (IProtocol)null);
            Assert.IsTrue(protocol != (IProtocol)null);
        }

        [Test]
        public void WhenOtherIsOfDifferentType_ThenEqualsReturnsFalse()
        {
            var protocol1 = RdpProtocol.Protocol;
            var protocol2 = new Mock<IProtocol>().Object;

            Assert.IsFalse(protocol1.Equals(protocol2));
            Assert.IsTrue((IProtocol)protocol1 != (IProtocol)protocol2);
        }

        [Test]
        public void WhenObjectsAreSame_ThenEqualsReturnsTrue()
        {
            var protocol1 = RdpProtocol.Protocol;
            var protocol2 = protocol1;
            Assert.IsTrue(protocol1.Equals(protocol2));
            Assert.IsTrue(protocol1 == protocol2);
        }
    }
}
