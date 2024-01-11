using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Controls
{
    public partial class RdpDiagnosticsWindow : Form
    {
        public RdpDiagnosticsWindow()
        {
            InitializeComponent();
            this.propertyGrid.SelectedObject = this.rdpClient;
            this.connectButton.Click += (_, __) => this.rdpClient.Connect();
            this.fullScreenButton.Click += (_, __) => this.rdpClient.TryEnterFullScreen(null);

            this.rdpClient.StateChanged += (_, __) 
                => this.Text = this.rdpClient.State.ToString();
            this.rdpClient.ConnectionFailed += (_, args) 
                => MessageBox.Show(this, args.Exception.FullMessage());
        }

        public RdpClient Client
        {
            get => this.rdpClient;
        }
    }
}
