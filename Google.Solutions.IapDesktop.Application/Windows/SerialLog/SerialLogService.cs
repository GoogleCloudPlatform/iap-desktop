using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Windows;
using System;
using System.Linq;
using WeifenLuo.WinFormsUI.Docking;

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
