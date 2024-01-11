using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestRdpClient
    {
        private RdpDiagnosticsWindow CreateWindow()
        {
            var window = new RdpDiagnosticsWindow();
            window.Client.MainWindow = window;
            window.Client.Username = ".\\admin";
            window.Client.Password = "admin";
            window.Client.Server = Dns.GetHostEntry("rdptesthost").AddressList.First().ToString();
            return window;
        }

        [InteractiveTest]
        [Test]
        public void ShowTestUi()
        {
            using (var window = CreateWindow())
            {
                window.ShowDialog();
            }
        }

        [WindowsFormsTest]
        public async Task ResizeWindow()
        {
            using (var window = CreateWindow())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Maximize.
                //
                window.WindowState = FormWindowState.Maximized;
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Restore.
                //
                window.WindowState = FormWindowState.Normal;
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Disonnect.
                //
                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task MinimizeAndRestoreFullScreenWindow()
        {
            using (var window = CreateWindow())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Enter full-screen.
                //
                Assert.IsFalse(window.Client.IsFullScreen);
                Assert.IsTrue(window.Client.CanEnterFullScreen);
                Assert.IsTrue(window.Client.TryEnterFullScreen(null));
                Assert.IsTrue(window.Client.IsFullScreen);

                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);
                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Minimize.
                //
                window.WindowState = FormWindowState.Minimized;
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Restore.
                //
                window.WindowState = FormWindowState.Normal;
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Disonnect.
                //
                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task EnterAndLeaveFullscreen()
        {
            using (var window = CreateWindow())
            {
                window.Show();

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                //
                // Enter full-screen.
                //
                Assert.IsFalse(window.Client.IsFullScreen);
                Assert.IsTrue(window.Client.CanEnterFullScreen);
                Assert.IsTrue(window.Client.TryEnterFullScreen(null));
                Assert.IsTrue(window.Client.IsFullScreen);

                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);
                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Leave full-screen.
                //
                Assert.IsTrue(window.Client.TryLeaveFullScreen());
                Assert.IsFalse(window.Client.IsFullScreen);

                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);
                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Enter full-screen again.
                //
                Assert.IsFalse(window.Client.IsFullScreen);
                Assert.IsTrue(window.Client.CanEnterFullScreen);
                Assert.IsTrue(window.Client.TryEnterFullScreen(null));
                Assert.IsTrue(window.Client.IsFullScreen);

                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);
                await Task.Delay(TimeSpan.FromSeconds(1));

                //
                // Leave full-screen again.
                //
                Assert.IsTrue(window.Client.TryLeaveFullScreen());
                Assert.IsFalse(window.Client.IsFullScreen);

                //
                // Disonnect.
                //
                window.Close();
            }
        }
    }
}
