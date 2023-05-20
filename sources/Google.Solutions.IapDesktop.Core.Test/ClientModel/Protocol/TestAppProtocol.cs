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
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestAppProtocol
    {
        private static AppProtocol CreateProtocol(string name)
        {
            return new AppProtocol(
                name,
                Enumerable.Empty<IProtocolTargetTrait>(),
                new AllowAllPolicy(),
                8080,
                null,
                null);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToStringReturnsName()
        {
            var protocol = CreateProtocol("app-1");

            Assert.AreEqual("app-1", protocol.ToString());
            Assert.AreEqual("app-1", protocol.Name);
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOtherIsNull_ThenEqualsReturnsFalse()
        {
            var protocol = CreateProtocol("app-1");

            Assert.IsFalse(protocol.Equals((object)null));
            Assert.IsFalse(((IEquatable<IProtocol>)protocol).Equals((IProtocol)null));
            Assert.IsFalse(protocol == (IProtocol)null);
            Assert.IsTrue(protocol != (IProtocol)null);
        }

        [Test]
        public void WhenOtherIsEquivalent_ThenEqualsReturnsTrue()
        {
            var protocol1 = new AppProtocol(
                "app-1",
                Enumerable.Empty<IProtocolTargetTrait>(),
                new AllowAllPolicy(),
                8080,
                null,
                null);
            var protocol2 = new AppProtocol(
                "app-1",
                Enumerable.Empty<IProtocolTargetTrait>(),
                new AllowAllPolicy(),
                8080,
                null,
                null);

            Assert.IsTrue(protocol1.Equals(protocol2));
            Assert.IsTrue(protocol1 == protocol2);
            Assert.IsFalse(protocol1 != protocol2);
            Assert.AreEqual(protocol1.GetHashCode(), protocol2.GetHashCode());
        }

        [Test]
        public void WhenOtherIsOfDifferentType_ThenEqualsReturnsFalse()
        {
            var protocol1 = CreateProtocol("app-1");
            var protocol2 = new Mock<IProtocol>().Object;

            Assert.IsFalse(protocol1.Equals(protocol2));
        }

        [Test]
        public void WhenOtherHasDifferentName_ThenEqualsReturnsFalse()
        {
            var protocol1 = new AppProtocol(
                "app-1",
                Enumerable.Empty<IProtocolTargetTrait>(),
                new AllowAllPolicy(),
                8080,
                null,
                null);
            var protocol2 = new AppProtocol(
                "app-2",
                Enumerable.Empty<IProtocolTargetTrait>(),
                new AllowAllPolicy(),
                8080,
                null,
                null);

            Assert.IsFalse(protocol1.Equals(protocol2));
            Assert.IsTrue(protocol1 != protocol2);
            Assert.AreNotEqual(protocol1.GetHashCode(), protocol2.GetHashCode());
        }

        [Test]
        public void WhenOtherHasDifferentRequiredTraits_ThenEqualsReturnsFalse()
        {
            var protocol1 = new AppProtocol(
                "app-1",
                Enumerable.Empty<IProtocolTargetTrait>(),
                new AllowAllPolicy(),
                8080,
                null,
                null);
            var protocol2 = new AppProtocol(
                "app-1",
                new[] { new InstanceTrait() },
                new AllowAllPolicy(),
                8080,
                null,
                null);

            Assert.IsFalse(protocol1.Equals(protocol2));
            Assert.IsTrue(protocol1 != protocol2);
        }

        [Test]
        public void WhenOtherHasDifferentPolicy_ThenEqualsReturnsFalse()
        {
            var protocol1 = new AppProtocol(
                "app-1",
                Enumerable.Empty<IProtocolTargetTrait>(),
                new AllowAllPolicy(),
                8080,
                null,
                null);
            var protocol2 = new AppProtocol(
                "app-1",
                Enumerable.Empty<IProtocolTargetTrait>(),
                new Mock<ITransportPolicy>().Object,
                8080,
                null,
                null);

            Assert.IsFalse(protocol1.Equals(protocol2));
            Assert.IsTrue(protocol1 != protocol2);
            Assert.AreNotEqual(protocol1.GetHashCode(), protocol2.GetHashCode());
        }

        [Test]
        public void WhenOtherHasDifferentRemotePort_ThenEqualsReturnsFalse()
        {
            var protocol1 = new AppProtocol(
                "app-1",
                Enumerable.Empty<IProtocolTargetTrait>(),
                new AllowAllPolicy(),
                8080,
                null,
                null);
            var protocol2 = new AppProtocol(
                "app-1",
                Enumerable.Empty<IProtocolTargetTrait>(),
                new AllowAllPolicy(),
                80,
                null,
                null);

            Assert.IsFalse(protocol1.Equals(protocol2));
            Assert.IsTrue(protocol1 != protocol2);
            Assert.AreNotEqual(protocol1.GetHashCode(), protocol2.GetHashCode());
        }

        [Test]
        public void WhenOtherHasDifferentLaunchCommand_ThenEqualsReturnsFalse()
        {
            var protocol1 = new AppProtocol(
                "app-1",
                Enumerable.Empty<IProtocolTargetTrait>(),
                new AllowAllPolicy(),
                8080,
                null,
                null);
            var protocol2 = new AppProtocol(
                "app-1",
                Enumerable.Empty<IProtocolTargetTrait>(),
                new AllowAllPolicy(),
                8080,
                null,
                "cmd.exe");

            Assert.IsFalse(protocol1.Equals(protocol2));
            Assert.IsTrue(protocol1 != protocol2);
            Assert.AreNotEqual(protocol1.GetHashCode(), protocol2.GetHashCode());
        }

        [Test]
        public void WhenObjectsAreSame_ThenEqualsReturnsTrue()
        {
            var protocol1 = CreateProtocol("app-1");
            var protocol2 = protocol1;
            Assert.IsTrue(protocol1.Equals(protocol2));
            Assert.IsTrue(protocol1 == protocol2);
        }
    }
}
