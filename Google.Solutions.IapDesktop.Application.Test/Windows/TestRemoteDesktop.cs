using Google.Solutions.Compute;
using Google.Solutions.Compute.Iap;
using Google.Solutions.Compute.Test.Env;
using Google.Solutions.IapDesktop.Application.Registry;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Windows.RemoteDesktop;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    public class TestRemoteDesktop : WindowTestFixtureBase
    {
        private readonly VmInstanceReference instanceReference = 
            new VmInstanceReference("project", "zone", "instance");

        [Test]
        public void WhenServerInvalid_ThenErrorIsShownAndWindowIsClosed()
        {
            var rdpService = new RemoteDesktopService(this.serviceProvider);
            rdpService.Connect(
                this.instanceReference,
                "invalid.corp", 
                3389, 
                new VmInstanceSettings());

            AwaitEvent<RemoteDesktopConnectionFailedEvent>();
            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(260, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }

        [Test]
        public void WhenPortNotListening_ThenErrorIsShownAndWindowIsClosed()
        {
            var rdpService = new RemoteDesktopService(this.serviceProvider);
            rdpService.Connect(
                this.instanceReference,
                "localhost", 
                1, 
                new VmInstanceSettings());

            AwaitEvent<RemoteDesktopConnectionFailedEvent>();
            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(516, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }

        [Test]
        public void WhenWrongPort_ThenErrorIsShownAndWindowIsClosed()
        {
            var rdpService = new RemoteDesktopService(this.serviceProvider);
            rdpService.Connect(
                this.instanceReference,
                "localhost",
                135,    // That one will be listening, but it is RPC, not RDP.
                new VmInstanceSettings());

            AwaitEvent<RemoteDesktopConnectionFailedEvent>();
            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(2308, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }
    }
}
