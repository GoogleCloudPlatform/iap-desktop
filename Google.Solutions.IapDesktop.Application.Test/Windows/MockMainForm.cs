using Google.Solutions.IapDesktop.Application.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    public partial class MockMainForm : Form, IMainForm
    {
        public MockMainForm()
        {
            InitializeComponent();
        }

        public DockPanel MainPanel => this.dockPanel;
    }
}
