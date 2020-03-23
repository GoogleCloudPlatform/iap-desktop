using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Windows

{
    public partial class ToolWindow : DockContent
    {
        public ContextMenuStrip TabContextStrip => this.contextMenuStrip;

        public ToolWindow()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        private void closeMenuItem_Click(object sender, System.EventArgs e)
        {
            this.CloseSafely();
        }


        private void ToolWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Shift && e.KeyCode == Keys.Escape)
            {
                CloseSafely();
            }
        }

        protected void CloseSafely()
        {
            if (this.HideOnClose)
            {
                Hide();
            }
            else
            {
                Close();
            }
        }
    }    
}