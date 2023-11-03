using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Theming rules for ensuring compatibility with
    /// GDI scaling.
    /// </summary>
    public class GdiScalingRuleset : ControlTheme.IRuleSet
    {
        private readonly PropertyInfo doubleBufferedProperty;

        public GdiScalingRuleset()
        {
            this.doubleBufferedProperty = typeof(Control).GetProperty(
                "DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        private void DisableDoubleBuffering<TControl>(TControl control)
            where TControl : Control
        {
            //
            // GDI scaling doesn't work if a control uses double-buffering.
            // For simple controls, it's a reasonable trade-off to sacrifice
            // double-buffering in exchange for crisp text.
            //
            if (DpiVirtualization.IsActive && this.doubleBufferedProperty != null)
            {
                this.doubleBufferedProperty.SetValue(control, false);
            }
        }

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        /// <summary>
        /// Register rules.
        /// </summary>
        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));

            controlTheme.AddRule<Label>(DisableDoubleBuffering);
            controlTheme.AddRule<CheckBox>(DisableDoubleBuffering);
            controlTheme.AddRule<Button>(DisableDoubleBuffering);
        }
    }
}
