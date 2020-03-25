using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services
{
    [TestFixture]
    public class TestTunnelBrokerService
    {
        [Test]
        public async Task WhenConnectSuccessful_ThenOpenTunnelsIncludesTunnel()
        {
            var mockTunnelService = new Mock<ITunnelService>();
            var mockTunnel = new Mock<Tunnel>(null, null, null);
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(true));

            var broker = new TunnelBrokerService(mockTunnelService.Object);
            var vmInstanceRef = new VmInstanceReference("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(destination))
                .Returns(Task.FromResult(mockTunnel.Object));

            var tunnel = await broker.ConnectAsync(destination, TimeSpan.FromMinutes(1));

            Assert.IsNotNull(tunnel);
            Assert.AreEqual(1, broker.OpenTunnels.Count());

            Assert.AreSame(tunnel, broker.OpenTunnels.First());
        }

        [Test]
        public async Task WhenConnectingTwice_ExistingTunnelIsReturned()
        {
            var mockTunnelService = new Mock<ITunnelService>();
            var mockTunnel = new Mock<Tunnel>(null, null, null);
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(true));
            var broker = new TunnelBrokerService(mockTunnelService.Object);

            var vmInstanceRef = new VmInstanceReference("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(destination))
                .Returns(Task.FromResult(mockTunnel.Object));

            var tunnel1 = await broker.ConnectAsync(destination, TimeSpan.FromMinutes(1));
            var tunnel2 = await broker.ConnectAsync(destination, TimeSpan.FromMinutes(1));

            Assert.IsNotNull(tunnel1);
            Assert.IsNotNull(tunnel2);
            Assert.AreSame(tunnel1, tunnel2);
            Assert.AreEqual(1, broker.OpenTunnels.Count());
        }

        [Test]
        public void WhenConnectFails_ThenOpenTunnelsDoesNotIncludeTunnel()
        {
            var mockTunnelService = new Mock<ITunnelService>();

            var broker = new TunnelBrokerService(mockTunnelService.Object);
            var vmInstanceRef = new VmInstanceReference("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(destination))
                .Returns(Task.FromException<Tunnel>(new ApplicationException()));

            AssertEx.ThrowsAggregateException<ApplicationException>(() =>
            {
                broker.ConnectAsync(destination, TimeSpan.FromMinutes(1)).Wait();
            });

            Assert.AreEqual(0, broker.OpenTunnels.Count());
        }

        [Test]
        public void WhenProbeFails_ThenOpenTunnelsDoesNotIncludeTunnel()
        {
            var mockTunnelService = new Mock<ITunnelService>();
            var mockTunnel = new Mock<Tunnel>(null, null, null);
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromException(new ApplicationException()));

            var broker = new TunnelBrokerService(mockTunnelService.Object);
            var vmInstanceRef = new VmInstanceReference("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(destination))
                .Returns(Task.FromResult(mockTunnel.Object));

            AssertEx.ThrowsAggregateException<ApplicationException>(() =>
            {
                broker.ConnectAsync(destination, TimeSpan.FromMinutes(1)).Wait();
            });

            Assert.AreEqual(0, broker.OpenTunnels.Count());
        }

        [Test]
        public async Task WhenClosingTunnel_ThenTunnelIsRemovedFromOpenTunnels()
        {
            var mockTunnelService = new Mock<ITunnelService>();
            var mockTunnel = new Mock<Tunnel>(null, null, null);
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(true));
            mockTunnel.Setup(t => t.Close());

            var broker = new TunnelBrokerService(mockTunnelService.Object);
            var vmInstanceRef = new VmInstanceReference("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(destination))
                .Returns(Task.FromResult(mockTunnel.Object));

            var tunnel = await broker.ConnectAsync(destination, TimeSpan.FromMinutes(1));

            Assert.AreEqual(1, broker.OpenTunnels.Count());

            broker.CloseTunnel(destination);

            Assert.AreEqual(0, broker.OpenTunnels.Count());
        }

        [Test]
        public async Task WhenClosingAllTunnels_AllTunnelsAreClosed()
        {
            var mockTunnelService = new Mock<ITunnelService>();
            var mockTunnel = new Mock<Tunnel>(null, null, null);
            mockTunnel.Setup(t => t.Probe(It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(true));
            mockTunnel.Setup(t => t.Close());

            var broker = new TunnelBrokerService(mockTunnelService.Object);
            var vmInstanceRef = new VmInstanceReference("project", "zone", "instance");
            var destination = new TunnelDestination(vmInstanceRef, 3389);
            mockTunnelService.Setup(s => s.CreateTunnelAsync(destination))
                .Returns(Task.FromResult(mockTunnel.Object));

            var tunnel = await broker.ConnectAsync(destination, TimeSpan.FromMinutes(1));

            Assert.AreEqual(1, broker.OpenTunnels.Count());

            broker.CloseTunnels();

            Assert.AreEqual(0, broker.OpenTunnels.Count());
        }

    }
}
