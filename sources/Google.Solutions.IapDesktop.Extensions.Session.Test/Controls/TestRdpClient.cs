using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
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
    public partial class TestRdpClient : Form
    {
        public TestRdpClient()
        {
            InitializeComponent();
            this.propertyGrid.SelectedObject = this.rdpClient;
            this.connectButton.Click += (_, __) => this.rdpClient.Connect();
            this.fullScreenButton.Click += (_, __) => this.rdpClient.TryEnterFullScreen(null);

            this.rdpClient.StateChanged += (_, __) 
                => this.Text = this.rdpClient.State.ToString();
            this.rdpClient.ConnectionFailed += (_, args) 
                => MessageBox.Show(this, args.Exception.FullMessage());

            this.rdpClient.MainWindow = this;
            this.rdpClient.Username = ".\\admin";
            this.rdpClient.Password = "admin";
            this.rdpClient.Server = Dns.GetHostEntry("rdptesthost").AddressList.First().ToString();

            ApplicationTraceSource.Log.Listeners.Add(new DefaultTraceListener());
        }

        [InteractiveTest]
        [Test]
        public void ShowTestUi()
        {
            ShowDialog();
        }


        [InteractiveTest]
        [Test]
        public async Task EnterAndLeaveFullscreen()
        {
            Show();

            this.rdpClient.Connect();

            await this.rdpClient
                .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                .ConfigureAwait(true);

  //          await Task.Delay(200);
//            Assert.IsTrue(this.rdpClient.TryEnterFullScreen(null));


            await this.rdpClient
                .AwaitStateAsync(RdpClient.ConnectionState.NotConnected)
                .ConfigureAwait(true);
        }
    }
}
