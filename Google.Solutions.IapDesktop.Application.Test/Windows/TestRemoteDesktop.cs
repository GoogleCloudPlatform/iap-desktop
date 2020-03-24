using Google.Solutions.Compute;
using Google.Solutions.Compute.Iap;
using Google.Solutions.Compute.Test.Env;
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
        private class RdpTunnel : IDisposable
        {
            private readonly SshRelayListener listener;
            private readonly CancellationTokenSource tokenSource;

            public int LocalPort => listener.LocalPort;

            public void Dispose()
            {
                this.tokenSource.Cancel();
            }

            private RdpTunnel(SshRelayListener listener, CancellationTokenSource tokenSource)
            {
                this.listener = listener;
                this.tokenSource = tokenSource;
            }

            public static RdpTunnel Create(VmInstanceReference vmRef)
            {
                var listener = SshRelayListener.CreateLocalListener(
                    new IapTunnelingEndpoint(
                        Defaults.GetCredential(),
                        vmRef,
                        3389,
                        IapTunnelingEndpoint.DefaultNetworkInterface));

                var tokenSource = new CancellationTokenSource();
                listener.ListenAsync(tokenSource.Token);

                return new RdpTunnel(listener, tokenSource);
            }
        }

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


            // check that window has closed

            Assert.IsNull(this.exceptionDialog.ExceptionShown);
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
            Assert.Inconclusive();
        }

        [Test]
        public void WhenWrongPort_ThenErrorIsShownAndWindowIsClosed()
        {
            // connect to port 7
            Assert.Inconclusive();
        }

        [Test]
        public void WhenCredentialsInvalid_ThenErrorIsShownAndWindowIsClosed()
        {
            // connect to port 7
            Assert.Inconclusive();
        }

        [Test]
        public void WhenAttemptServerAuthentication_ThenWarningIsShown()
        {
            // connect to port 7
            Assert.Inconclusive();
        }

        [Test]
        public void WhenNoServerAuthentication_ThenConnectionSucceeds()
        {
            // connect to port 7
            Assert.Inconclusive();
        }

        [Test]
        public void WhenRequireServerAuthentication_ThenConnectionFails()
        {
            // connect to port 7
            Assert.Inconclusive();
        }

        [Test]
        public void WhenCredentialsValid_ThenConnectingSucceeds(
            [Values(RdpConnectionBarState.AutoHide, RdpConnectionBarState.Off, RdpConnectionBarState.Pinned)]
            RdpConnectionBarState connectionBarState,

            [Values(RdpDesktopSize.ClientSize, RdpDesktopSize.ScreenSize)]
            RdpDesktopSize desktopSize,

            [Values(RdpAudioMode.DoNotPlay, RdpAudioMode.PlayLocally, RdpAudioMode.PlayOnServer)]
            RdpAudioMode audioMode,

            [Values(RdpRedirectClipboard.Disabled, RdpRedirectClipboard.Enabled)]
            RdpRedirectClipboard redirectClipboard
            )
        {
            Assert.Inconclusive();
        }
    }
}
