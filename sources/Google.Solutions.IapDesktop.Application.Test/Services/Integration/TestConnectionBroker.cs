﻿//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Integration
{
    [TestFixture]
    public class TestConnectionBroker : FixtureBase
    {
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        [Test]
        public void WhenNoServiceRegistered_ThenIsConnectedReturnsFalse()
        {
            var registry = new ServiceRegistry();
            var broker = new GlobalConnectionBroker(registry);

            Assert.IsFalse(broker.IsConnected(SampleLocator));
        }

        [Test]
        public void WhenNoServiceRegistered_ThenTryActivateReturnsFalse()
        {
            var registry = new ServiceRegistry();
            var broker = new GlobalConnectionBroker(registry);

            Assert.IsFalse(broker.IsConnected(SampleLocator));
        }


        [Test]
        public void WhenServicesRegistered_ThenIsConnectedReturnsResult()
        {
            var service = new Mock<IConnectionBroker>();
            service.Setup(s => s.IsConnected(
                    It.Is<InstanceLocator>(l => l.Equals(SampleLocator))))
                .Returns(true);

            var registry = new ServiceRegistry();
            registry.AddSingleton<IConnectionBroker>(service.Object);
            registry.AddServiceToCategory(typeof(IConnectionBroker), typeof(IConnectionBroker));

            var broker = new GlobalConnectionBroker(registry);

            Assert.IsTrue(broker.IsConnected(SampleLocator));

            service.Verify(s => s.IsConnected(
                    It.Is<InstanceLocator>(l => l.Equals(SampleLocator))), Times.Once);
        }

        [Test]
        public void WhenServicesRegistered_ThenTryActivateReturnsResult()
        {
            var service = new Mock<IConnectionBroker>();
            service.Setup(s => s.TryActivate(
                    It.Is<InstanceLocator>(l => l.Equals(SampleLocator))))
                .Returns(true);

            var registry = new ServiceRegistry();
            registry.AddSingleton<IConnectionBroker>(service.Object);
            registry.AddServiceToCategory(typeof(IConnectionBroker), typeof(IConnectionBroker));

            var broker = new GlobalConnectionBroker(registry);

            Assert.IsTrue(broker.TryActivate(SampleLocator));

            service.Verify(s => s.TryActivate(
                    It.Is<InstanceLocator>(l => l.Equals(SampleLocator))), Times.Once);
        }
    }
}
