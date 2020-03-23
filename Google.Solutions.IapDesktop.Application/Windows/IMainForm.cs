using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    public interface IMainForm
    {
        DockPanel MainPanel { get; }
        void Close();
    }

}
