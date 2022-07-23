using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Services
{
    public interface IThemeService
    {
        DockPanelColorPalette ColorPalette { get; }
        void ApplyTheme(DockPanel dockPanel);
        void ApplyTheme(ToolStrip toolStrip);
    }

    public class ThemeService : IThemeService
    {
        private readonly ThemeBase theme = new VS2015DarkTheme();
        private readonly VisualStudioToolStripExtender toolStripExtender
            = new VisualStudioToolStripExtender();


        //---------------------------------------------------------------------
        // IThemeService.
        //---------------------------------------------------------------------

        public DockPanelColorPalette ColorPalette => this.theme.ColorPalette;

        public void ApplyTheme(DockPanel dockPanel)
        {
            dockPanel.Theme = theme;
        }

        public void ApplyTheme(ToolStrip toolStrip)
        {
            this.toolStripExtender.SetStyle(
                toolStrip,
                VisualStudioToolStripExtender.VsVersion.Vs2015,
                this.theme);
        }
    }
}
