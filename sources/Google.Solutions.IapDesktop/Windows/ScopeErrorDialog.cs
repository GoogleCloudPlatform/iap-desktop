using Google.Solutions.IapDesktop.Application.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Windows
{
    public partial class ScopeErrorDialog : Form
    {
        public ScopeErrorDialog()
        {
            InitializeComponent();
            this.headlineLabel.ForeColor = ThemeColors.HighlightBlue;
        }
    }
}
