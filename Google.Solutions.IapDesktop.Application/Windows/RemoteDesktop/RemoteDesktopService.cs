using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using System;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Windows.RemoteDesktop
{
    public class RemoteDesktopService
    {
        private readonly DockPanel dockPanel;

        public RemoteDesktopService(IServiceProvider serviceProvider)
        {
            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;
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
