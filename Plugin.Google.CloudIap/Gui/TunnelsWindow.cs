using Plugin.Google.CloudIap.Integration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Plugin.Google.CloudIap.Gui
{
    public partial class TunnelsWindow : Form
    {
        private IapTunnelManager TunnelManager { get; set; }

        public TunnelsWindow()
        {
            InitializeComponent();
        }

        private void RefreshTunnels()
        {
            this.tunnelsList.Items.Clear();

            foreach (var tunnel in this.TunnelManager.OpenTunnels)
            {
                ListViewItem item = new ListViewItem(new string[] {
                    tunnel.Endpoint.Instance.InstanceName,
                    tunnel.Endpoint.Instance.ProjectId,
                    tunnel.Endpoint.Instance.Zone,
                    tunnel.LocalPort.ToString(),
                    tunnel.ProcessId.ToString()
                });
                item.Tag = tunnel;
                this.tunnelsList.Items.Add(item);
            }
        }

        private void tunnelsList_SelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            this.terminateTunnelButton.Enabled = this.tunnelsList.SelectedIndices.Count > 0;
        }

        private void terminateTunnelButton_Click(object sender, EventArgs eventArgse)
        {
            var selectedItem = this.tunnelsList.SelectedItems
                .Cast<ListViewItem>()
                .FirstOrDefault();
            if (selectedItem == null)
            {
                return;
            }

            var selectedTunnel = (IapTunnel)selectedItem.Tag;

            if (MessageBox.Show(
                this,
                $"Are you sure you wish to terminate the tunnel to {selectedTunnel.Endpoint}",
                "Terminate tunnel",
                MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
            {
                try
                {
                    this.TunnelManager.CloseTunnel(selectedTunnel.Endpoint);
                    RefreshTunnels();
                }
                catch (Exception e)
                {
                    ExceptionUtil.HandleException(this, "Terminating tunnel failed", e);
                }
            }
        }

        internal static void ShowDialog(IWin32Window owner, IapTunnelManager tunnelManager)
        {
            var window = new TunnelsWindow()
            {
                TunnelManager = tunnelManager
            };
            window.RefreshTunnels();
            window.ShowDialog(owner);
        }
    }
}
