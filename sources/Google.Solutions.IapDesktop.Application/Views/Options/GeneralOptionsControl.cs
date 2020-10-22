using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Solutions.IapDesktop.Application.ObjectModel;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    internal partial class GeneralOptionsControl : UserControl
    {
        public GeneralOptionsControl(GeneralOptionsViewModel viewModel)
        {
            InitializeComponent();

            this.enableUpdateCheckBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsUpdateCheckEnabled,
                this.Container);
            this.lastCheckLabel.BindProperty(
                c => c.Text,
                viewModel,
                m => m.LastUpdateCheck,
                this.Container);
            this.enableBrowserIntegrationCheclBox.BindProperty(
                c => c.Checked,
                viewModel,
                m => m.IsBrowserIntegrationEnabled,
                this.Container);
        }
    }
}
