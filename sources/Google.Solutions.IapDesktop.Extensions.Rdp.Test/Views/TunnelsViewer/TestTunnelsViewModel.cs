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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Tunnel;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.TunnelsViewer;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Views.TunnelsViewer
{
    [TestFixture]
    public class TestTunnelsViewModel : FixtureBase
    {
        private static Mock<IConfirmationDialog> CreateConfirmationDialog(DialogResult result)
        {
            var confirmationDialog = new Mock<IConfirmationDialog>();
            confirmationDialog.Setup(d => d.Confirm(
                It.IsAny<IWin32Window>(),
                It.IsAny<string>(),
                It.IsAny<string>())).Returns(result);
            return confirmationDialog;
        }

        private static Mock<ITunnel> CreateTunnel(string instanceName)
        {
            var tunnel = new Mock<ITunnel>();
            tunnel.SetupGet(t => t.Destination)
                .Returns(new TunnelDestination(
                    new InstanceLocator("project-1", "zone", instanceName),
                    123));
            return tunnel;
        }

        private static Mock<ITunnelBrokerService> CreateTunnelBroker(int tunnelCount)
        {
            var tunnels = new List<ITunnel>();

            for (int i = 0; i < tunnelCount; i++)
            {
                tunnels.Add(CreateTunnel("instance-" + i).Object);
            }

            var broker = new Mock<ITunnelBrokerService>();
            broker.SetupGet(b => b.OpenTunnels).Returns(tunnels);
            return broker;
        }

        class MockEventService : IEventService
        {
            public virtual void BindHandler<TEvent>(Action<TEvent> handler)
            {
            }

            public virtual void BindAsyncHandler<TEvent>(Func<TEvent, Task> handler)
            {
            }

            public Task FireAsync<TEvent>(TEvent eventObject)
            {
                return Task.FromResult(0);
            }
        }


        [Test]
        public void WhenTunnelsListEmpty_ThenDisconnectButtonIsDisabled()
        {
            var viewModel = new TunnelsViewModel(
                CreateTunnelBroker(0).Object,
                CreateConfirmationDialog(DialogResult.Cancel).Object,
                new MockEventService());
            viewModel.RefreshTunnels();

            Assert.IsFalse(viewModel.IsDisconnectButtonEnabled);
        }

        [Test]
        public void WhenTunnelSelected_ThenDisconnectButtonIsEnabled()
        {
            var viewModel = new TunnelsViewModel(
                CreateTunnelBroker(1).Object,
                CreateConfirmationDialog(DialogResult.Cancel).Object,
                new MockEventService());
            viewModel.RefreshTunnels();

            Assert.IsFalse(viewModel.IsDisconnectButtonEnabled);

            viewModel.SelectedTunnel = viewModel.Tunnels[0];
            Assert.IsTrue(viewModel.IsDisconnectButtonEnabled);
        }

        [Test]
        public async Task WhenNoTunnelSelected_ThenDisconnectSelectedTunnelDoesNothing()
        {
            var viewModel = new TunnelsViewModel(
                CreateTunnelBroker(1).Object,
                CreateConfirmationDialog(DialogResult.Cancel).Object,
                new MockEventService());
            viewModel.RefreshTunnels();

            Assert.AreEqual(1, viewModel.Tunnels.Count);

            await viewModel.DisconnectSelectedTunnelAsync();

            Assert.AreEqual(1, viewModel.Tunnels.Count);
        }

        [Test]
        public async Task WhenTunnelSelectedAndDisconnectConfirmed_ThenTunnelIsClosed()
        {
            var broker = CreateTunnelBroker(1);
            var viewModel = new TunnelsViewModel(
                broker.Object,
                CreateConfirmationDialog(DialogResult.Yes).Object,
                new MockEventService());
            viewModel.RefreshTunnels();
            viewModel.SelectedTunnel = viewModel.Tunnels[0];

            Assert.AreEqual(1, viewModel.Tunnels.Count);

            await viewModel.DisconnectSelectedTunnelAsync();

            broker.Verify(b => b.DisconnectAsync(It.IsAny<TunnelDestination>()), Times.Once);
            Assert.IsNull(viewModel.SelectedTunnel);
        }

        [Test]
        public async Task WhenTunnelSelectedAndDisconnectNotConfirmed_ThenTunnelIsLeftOpen()
        {
            var broker = CreateTunnelBroker(1);
            var viewModel = new TunnelsViewModel(
                broker.Object,
                CreateConfirmationDialog(DialogResult.Cancel).Object,
                new MockEventService());
            viewModel.RefreshTunnels();
            viewModel.SelectedTunnel = viewModel.Tunnels[0];

            Assert.AreEqual(1, viewModel.Tunnels.Count);

            await viewModel.DisconnectSelectedTunnelAsync();

            broker.Verify(b => b.DisconnectAsync(It.IsAny<TunnelDestination>()), Times.Never);
            Assert.IsNotNull(viewModel.SelectedTunnel);
        }
    }
}
