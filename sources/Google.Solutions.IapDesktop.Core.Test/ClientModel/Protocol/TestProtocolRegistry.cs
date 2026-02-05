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
using Moq;
using NUnit.Framework;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestProtocolRegistry
    {
        //---------------------------------------------------------------------
        // Protocols.
        //---------------------------------------------------------------------

        [Test]
        public void Protocols_WhenNoProtocolsRegistered()
        {
            var registry = new ProtocolRegistry();
            Assert.IsNotNull(registry.Protocols);
            Assert.That(registry.Protocols, Is.Empty);
        }

        [Test]
        public void Protocols_WhenProtocolsRegistered()
        {
            var registry = new ProtocolRegistry();
            registry.RegisterProtocol(new Mock<IProtocol>().Object);
            registry.RegisterProtocol(new Mock<IProtocol>().Object);
            Assert.That(registry.Protocols.Count(), Is.EqualTo(2));
        }

        //---------------------------------------------------------------------
        // GetAvailableProtocols.
        //---------------------------------------------------------------------

        [Test]
        public void GetAvailableProtocols_WhenNoProtocolsRegistered()
        {
            var registry = new ProtocolRegistry();
            var protocols = registry.GetAvailableProtocols(new Mock<IProtocolTarget>().Object);

            Assert.IsNotNull(protocols);
            Assert.That(protocols, Is.Empty);
        }

        [Test]
        public void GetAvailableProtocols_WhenNoProtocolsAvailable()
        {
            var target = new Mock<IProtocolTarget>().Object;
            var unavailableProtocol = new Mock<IProtocol>();
            unavailableProtocol.Setup(p => p.IsAvailable(target)).Returns(false);

            var registry = new ProtocolRegistry();
            registry.RegisterProtocol(unavailableProtocol.Object);

            var protocols = registry.GetAvailableProtocols(target);

            Assert.IsNotNull(protocols);
            Assert.That(protocols, Is.Empty);
        }
    }
}
