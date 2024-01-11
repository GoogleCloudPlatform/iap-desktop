using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
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

            this.rdpClient.StateChanged += (_, __) 
                => this.Text = this.rdpClient.State.ToString();
            this.rdpClient.ConnectionFailed += (_, args) 
                => MessageBox.Show(this, args.Exception.FullMessage());

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
    }
}
