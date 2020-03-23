using Google.Solutions.IapDesktop.Application.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Windows.RemoteDesktop
{
    public class RemoteDesktopService
    {
        private readonly DockPanel dockPanel;

        public RemoteDesktopService(DockPanel dockPanel)
        {
            this.dockPanel = dockPanel;
        }

        public void Connect(
            string server,
            ushort port,
            VmInstanceSettings settings)
        {
            var rdpPane = new RemoteDesktopPane();
            rdpPane.Show(this.dockPanel, DockState.Document);
            //rdpPane.Show();
            rdpPane.Connect(server, port, settings);
        }
    }
}
