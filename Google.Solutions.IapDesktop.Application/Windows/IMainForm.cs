using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    public interface IMainForm
    {
        DockPanel MainPanel { get; }
        void Close();
    }

}
