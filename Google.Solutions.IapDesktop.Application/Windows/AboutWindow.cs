using System;
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
