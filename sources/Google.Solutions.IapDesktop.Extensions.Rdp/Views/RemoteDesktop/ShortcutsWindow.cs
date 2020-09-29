using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Views.RemoteDesktop
{
    public partial class ShortcutsWindow : Form
    {
        public ShortcutsWindow()
        {
            InitializeComponent();
        }

        private void ShortcutsWindow_Deactivate(object sender, EventArgs e)
        {
            Close();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ShortcutsWindow_Load(object sender, EventArgs e)
        {
            CenterToParent();

            var assembly = GetType().Assembly;
            var resourceName = assembly.GetManifestResourceNames().First(s => s.EndsWith("Shortcuts.rtf"));
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                this.richTextBox.Rtf = result;
            }
        }
    }
}
