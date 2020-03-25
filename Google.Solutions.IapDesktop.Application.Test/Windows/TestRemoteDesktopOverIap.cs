using Google.Solutions.Compute;
using Google.Solutions.Compute.Iap;
using Google.Solutions.Compute.Test.Env;
using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Registry;
using Google.Solutions.IapDesktop.Application.Services;
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
    [Category("IntegrationTest")]
    [Category("IAP")]
    public class TestRemoteDesktopOverIap : WindowTestFixtureBase
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


        [Test]
        public async Task WhenCredentialsInvalid_ThenErrorIsShownAndWindowIsClosed(
            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            using (var tunnel = RdpTunnel.Create(testInstance.InstanceReference))
            {
                var rdpService = new RemoteDesktopService(this.serviceProvider);
                var session = rdpService.Connect(
                    testInstance.InstanceReference,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    new VmInstanceSettings()
                    {
                        Username = "wrong",
                        Password = SecureStringExtensions.FromClearText("wrong"),
                        AuthenticationLevel = RdpAuthenticationLevel.NoServerAuthentication,
                        UserAuthenticationBehavior = RdpUserAuthenticationBehavior.AbortOnFailure
                    });

                AwaitEvent<RemoteDesktopConnectionFailedEvent>();
                Assert.IsNotNull(this.ExceptionShown);
                Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
                Assert.AreEqual(2055, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
            }
        }

        //
        // There's no reliable way to dismiss the warning/error, so these tests seem 
        // challenging to implement.
        //
        //[Test]
        //public void WhenAttemptServerAuthentication_ThenWarningIsShown()
        //{
        //}

        //[Test]
        //public void WhenRequireServerAuthentication_ThenConnectionFails(
        //    [WindowsInstance] InstanceRequest testInstance)
        //{
        //}

        [Test]
        public async Task WhenCredentialsValid_ThenConnectingSucceeds(
            [Values(RdpConnectionBarState.AutoHide, RdpConnectionBarState.Off, RdpConnectionBarState.Pinned)]
            RdpConnectionBarState connectionBarState,

            [Values(RdpDesktopSize.ClientSize, RdpDesktopSize.ScreenSize)]
            RdpDesktopSize desktopSize,

            [Values(RdpAudioMode.DoNotPlay, RdpAudioMode.PlayLocally, RdpAudioMode.PlayOnServer)]
            RdpAudioMode audioMode,

            [Values(RdpRedirectClipboard.Disabled, RdpRedirectClipboard.Enabled)]
            RdpRedirectClipboard redirectClipboard,

            [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            using (var tunnel = RdpTunnel.Create(testInstance.InstanceReference))
            using (var gceAdapter = new ComputeEngineAdapter(this.serviceProvider.GetService<IAuthorizationService>()))
            {
                var credentials = await gceAdapter.ResetWindowsUserAsync(
                    testInstance.InstanceReference, 
                    "test", 
                    CancellationToken.None);

                var rdpService = new RemoteDesktopService(this.serviceProvider);
                var session = rdpService.Connect(
                    testInstance.InstanceReference,
                    "localhost",
                    (ushort)tunnel.LocalPort,
                    new VmInstanceSettings()
                    {
                        Username = credentials.UserName,
                        Password = credentials.SecurePassword,
                        ConnectionBar = connectionBarState,
                        DesktopSize = desktopSize,
                        AudioMode = audioMode,
                        RedirectClipboard = redirectClipboard,
                        AuthenticationLevel = RdpAuthenticationLevel.NoServerAuthentication
                    });

                AwaitEvent<RemoteDesktopConnectionSuceededEvent>();
                Assert.IsNull(this.ExceptionShown);


                RemoteDesktopWindowClosedEvent expectedEvent = null;

                this.serviceProvider.GetService<IEventService>()
                    .BindHandler<RemoteDesktopWindowClosedEvent>(e =>
                    {
                        expectedEvent = e;
                    });
                session.Close();

                Assert.IsNotNull(expectedEvent);
            }
        }
    }
}
