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
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestAppProtocol : EquatableFixtureBase<AppProtocol, IProtocol>
    {
        protected override AppProtocol CreateInstance()
        {
            return CreateProtocol("app-1");
        }

        private static AppProtocol CreateProtocol(string name)
        {
            return new AppProtocol(
                name,
                Enumerable.Empty<ITrait>(),
                8080,
                null,
                null);
        }

        //---------------------------------------------------------------------
        // IsAvailable.
        //---------------------------------------------------------------------

        [Test]
        public void IsAvailable_WhenClientNotAvailable()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(false);

            var target = new Mock<IProtocolTarget>();
            var protocol = new AppProtocol(
                "test",
                Enumerable.Empty<ITrait>(),
                8080,
                null,
                client.Object);

            Assert.IsFalse(protocol.IsAvailable(target.Object));
        }

        [Test]
        public void IsAvailable_WhenRequiredTraitMissing()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);

            var target = new Mock<IProtocolTarget>();
            var protocol = new AppProtocol(
                "test",
                new[] { new Mock<ITrait>().Object },
                8080,
                null,
                client.Object);

            Assert.IsFalse(protocol.IsAvailable(target.Object));
        }

        [Test]
        public void IsAvailable_WhenPrerequisitesMet()
        {
            var client = new Mock<IAppProtocolClient>();
            client.SetupGet(c => c.IsAvailable).Returns(true);

            var trait = new Mock<ITrait>().Object;

            var target = new Mock<IProtocolTarget>();
            target.SetupGet(t => t.Traits).Returns(new[] { trait });

            var protocol = new AppProtocol(
                "test",
                new[] { trait },
                8080,
                null,
                client.Object);

            Assert.IsTrue(protocol.IsAvailable(target.Object));
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_ReturnsName()
        {
            var protocol = CreateProtocol("app-1");

            Assert.That(protocol.ToString(), Is.EqualTo("app-1"));
            Assert.That(protocol.Name, Is.EqualTo("app-1"));
        }

        //---------------------------------------------------------------------
        // Equals.
        //---------------------------------------------------------------------

        [Test]
        public void Equals_WhenOtherHasDifferentName()
        {
            var protocol1 = new AppProtocol(
                "app-1",
                Enumerable.Empty<ITrait>(),
                8080,
                null,
                null);
            var protocol2 = new AppProtocol(
                "app-2",
                Enumerable.Empty<ITrait>(),
                8080,
                null,
                null);

            Assert.IsFalse(protocol1.Equals(protocol2));
            Assert.IsTrue(protocol1 != protocol2);
            Assert.AreNotEqual(protocol1.GetHashCode(), protocol2.GetHashCode());
        }

        [Test]
        public void Equals_WhenOtherHasDifferentRequiredTraits()
        {
            var protocol1 = new AppProtocol(
                "app-1",
                Enumerable.Empty<ITrait>(),
                8080,
                null,
                null);
            var protocol2 = new AppProtocol(
                "app-1",
                new[] { InstanceTrait.Instance },
                8080,
                null,
                null);

            Assert.IsFalse(protocol1.Equals(protocol2));
            Assert.IsTrue(protocol1 != protocol2);
        }

        [Test]
        public void Equals_WhenOtherHasDifferentRemotePort()
        {
            var protocol1 = new AppProtocol(
                "app-1",
                Enumerable.Empty<ITrait>(),
                8080,
                null,
                null);
            var protocol2 = new AppProtocol(
                "app-1",
                Enumerable.Empty<ITrait>(),
                80,
                null,
                null);

            Assert.IsFalse(protocol1.Equals(protocol2));
            Assert.IsTrue(protocol1 != protocol2);
            Assert.AreNotEqual(protocol1.GetHashCode(), protocol2.GetHashCode());
        }

        [Test]
        public void Equals_WhenOtherHasDifferentClient()
        {
            var protocol1 = new AppProtocol(
                "app-1",
                Enumerable.Empty<ITrait>(),
                8080,
                null,
                null);
            var protocol2 = new AppProtocol(
                "app-1",
                Enumerable.Empty<ITrait>(),
                8080,
                null,
                new AppProtocolClient("cmd.exe", null));

            Assert.IsTrue(protocol1.Equals(protocol2));
            Assert.IsFalse(protocol1 != protocol2);
            Assert.That(protocol2.GetHashCode(), Is.EqualTo(protocol1.GetHashCode()));
        }
    }
}
