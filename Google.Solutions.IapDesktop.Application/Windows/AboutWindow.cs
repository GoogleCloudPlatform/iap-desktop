using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    public partial class AboutWindow : Form
    {
        public static Version ProgramVersion => typeof(AboutWindow).Assembly.GetName().Version;

        public AboutWindow()
        {
            InitializeComponent();

            this.infoLabel.Text = $"IAP Desktop\nVersion {ProgramVersion}";
        }
    }
}
