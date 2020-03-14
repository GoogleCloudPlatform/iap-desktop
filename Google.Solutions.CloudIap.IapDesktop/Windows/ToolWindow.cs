using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.CloudIap.IapDesktop.Windows

{
    public partial class ToolWindow : DockContent
    {
        public ToolWindow()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        private void closeMenuItem_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        public ContextMenuStrip TabContextStrip => this.contextMenuStrip;

    }    
}