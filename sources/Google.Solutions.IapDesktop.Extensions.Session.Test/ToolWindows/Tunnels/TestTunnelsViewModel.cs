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
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Tunnels;
using Moq;
using NUnit.Framework;
using System.ComponentModel;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Tunnels
{
    [TestFixture]
    public class TestTunnelsViewModel
    {
        private static Mock<IIapTunnel> CreateTunnel(string instanceName)
        {
            var tunnel = new Mock<IIapTunnel>();
            tunnel
                .SetupGet(t => t.TargetInstance)
                .Returns(new InstanceLocator("project-1", "zone", instanceName));
            return tunnel;
        }

        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        [Test]
        public void List_WhenTunnelCreatedOrClosed()
        {
            var invoke = new Mock<ISynchronizeInvoke>();
            invoke.SetupGet(i => i.InvokeRequired).Returns(false);

            var queue = new EventQueue(invoke.Object);
            var factory = new Mock<IIapTransportFactory>();
            factory
                .SetupGet(f => f.Pool)
                .Returns(new[] { CreateTunnel("instance-1").Object });

            var viewModel = new TunnelsViewModel(
                factory.Object,
                queue);

            factory.VerifyGet(f => f.Pool, Times.Never);
            Assert.That(viewModel.Tunnels.Count, Is.EqualTo(0));

            queue.Publish(new TunnelEvents.TunnelCreated());
            factory.VerifyGet(f => f.Pool, Times.Exactly(1));

            queue.Publish(new TunnelEvents.TunnelClosed());
            factory.VerifyGet(f => f.Pool, Times.Exactly(2));
        }

        //---------------------------------------------------------------------
        // IsRefreshButtonEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void IsRefreshButtonEnabled_WhenTunnelsListEmpty()
        {
            var factory = new Mock<IIapTransportFactory>(); factory
                 .SetupGet(f => f.Pool)
                 .Returns(Enumerable.Empty<IIapTunnel>());

            var viewModel = new TunnelsViewModel(
                factory.Object,
                new Mock<IEventQueue>().Object);
            viewModel.RefreshTunnels();

            Assert.That(viewModel.IsRefreshButtonEnabled, Is.False);
        }

        [Test]
        public void IsRefreshButtonEnabled_WhenOneTunnelOpen()
        {
            var factory = new Mock<IIapTransportFactory>();
            factory
                .SetupGet(f => f.Pool)
                .Returns(new[] { CreateTunnel("instance-1").Object });

            var viewModel = new TunnelsViewModel(
                factory.Object,
                new Mock<IEventQueue>().Object);
            viewModel.RefreshTunnels();

            Assert.IsTrue(viewModel.IsRefreshButtonEnabled);
        }
    }
}
