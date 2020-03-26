using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Windows;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;
using Google.Solutions.IapDesktop.Application.Adapters;

namespace Google.Solutions.IapDesktop.Application.Windows.SerialLog
{
    public class SerialLogService
    {
        private readonly DockPanel dockPanel;
        private readonly IExceptionDialog exceptionDialog;
        private readonly IServiceProvider serviceProvider;

        public SerialLogService(IServiceProvider serviceProvider)
        {
            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.serviceProvider = serviceProvider;
        }

        private SerialLogWindow TryGetExistingWindow(VmInstanceReference vmInstance)
            => this.dockPanel.Contents
                .EnsureNotNull()
                .OfType<SerialLogWindow>()
                .Where(w => w.Instance == vmInstance)
                .FirstOrDefault();

        public void ShowSerialLog(VmInstanceReference vmInstance)
        {
            var window = TryGetExistingWindow(vmInstance);
            if (window == null)
            {
                var gceAdapter = this.serviceProvider.GetService<IComputeEngineAdapter>();
                
                window = new SerialLogWindow(vmInstance);
                window.TailSerialPortStream(gceAdapter.GetSerialPortOutput(vmInstance));
            }

            window.Show(this.dockPanel, DockState.DockBottomAutoHide);
            this.dockPanel.ActiveAutoHideContent = window;
            window.Activate();
        }
    }
}
