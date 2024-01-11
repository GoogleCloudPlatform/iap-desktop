using Google.Solutions.Common.Util;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
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
            this.rdpClient.StateChanged += (_, __) => this.Text = this.rdpClient.State.ToString();
            this.connectButton.Click += (_, __) =>
            {
                try
                {
                    this.rdpClient.Connect();
                }
                catch (Exception e)
                {
                    MessageBox.Show(this, e.FullMessage());
                }
            };

            this.rdpClient.EnableNetworkLevelAuthentication = true;
            this.rdpClient.Username = ".\\admin";
            this.rdpClient.Password = "admin";
            this.rdpClient.Server = "rdptesthost";
        }

        [InteractiveTest]
        [Test]
        public void ShowTestUi()
        {
            ShowDialog();
        }
    }
}
