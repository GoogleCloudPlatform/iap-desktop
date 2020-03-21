using Google.Solutions.CloudIap.IapDesktop.Application.Settings;
using Google.Solutions.CloudIap.IapDesktop.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.CloudIap.IapDesktop.Windows
{
    public partial class SettingsWindow : ToolWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();

            this.TabText = this.Text;

            this.propertyGrid.SelectedObject = new VirtualMachineSettingsEditor(
                null,
                new VirtualMachineSettings());
        }
    }
}
