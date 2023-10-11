using Google.Solutions.Common.Util;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Theming rules for DPI-awareness.
    /// </summary>
    public class DpiAwarenessRuleset : ControlTheme.IRuleSet
    {
        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        private void ResizeControl(Control c)
        { }

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));

            controlTheme.AddRule<Control>(ResizeControl);
        }
    }
}
