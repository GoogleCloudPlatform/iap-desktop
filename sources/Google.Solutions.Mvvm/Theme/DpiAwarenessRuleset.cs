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

        private void ScaleControl(Control c)
        {
            var location = c.Location;
            var size = c.Size;

            // TODO: Check if HighDPI needed.

            if (c.Dock.HasFlag(DockStyle.Fill))
            {
                return;
            }

            //
            // Resize horizontally.
            //
            if (c.Anchor.HasFlag(AnchorStyles.Right))
            {
                if (c.Anchor.HasFlag(AnchorStyles.Left))
                {
                    //
                    // Let auto-layout will take care of it.
                    //
                }
                else
                {
                    var newWidth = c.ScaleDpi(size.Width);

                    //
                    // Move left to maintain proportions.
                    //
                    var marginRight = c.Parent.Width - location.X - size.Width;
                    location.X = c.Parent.Width - newWidth - c.ScaleDpi(marginRight);

                    size.Width = newWidth;
                }
            }
            else if (c.Anchor.HasFlag(AnchorStyles.Left))
            {
                if (c.Anchor.HasFlag(AnchorStyles.Right))
                {
                    //
                    // Let auto-layout will take care of it.
                    //
                }
                else
                {
                    location.X = c.ScaleDpi(location.X);
                    size.Width = c.ScaleDpi(size.Width);
                }
            }

            //
            // Resize vertically.
            //
            if (c.Anchor.HasFlag(AnchorStyles.Top))
            {
                if (c.Anchor.HasFlag(AnchorStyles.Bottom))
                {
                    //
                    // Let auto-layout will take care of it.
                    //
                }
                else
                {
                    location.X = c.ScaleDpi(location.X);
                    size.Height = c.ScaleDpi(size.Height);
                }
            }
            else if (c.Anchor.HasFlag(AnchorStyles.Bottom))
            {
                if (c.Anchor.HasFlag(AnchorStyles.Top))
                {
                    //
                    // Let auto-layout will take care of it.
                    //
                }
                else
                {
                    var newHeight = c.ScaleDpi(size.Height);

                    //
                    // Move up to maintain proportions.
                    //
                    var marginBottom = c.Parent.Height - location.Y - size.Height;
                    location.Y = c.Parent.Height - newHeight - c.ScaleDpi(marginBottom);

                    size.Height = newHeight;
                }
            }

            c.Location = location;
            c.Size = size;
        }

        // TODO: adjust groupbox margin
        // TODO: adjust font size margin

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));

            controlTheme.AddRule<Control>(ScaleControl);
        }
    }

    //---------------------------------------------------------------------
    // Helper classes.
    //---------------------------------------------------------------------

    internal static class ControlExtensions
    {
        public static int ScaleDpi(this Control c, int size)
        {
            // TODO: use Control.DeviceDpi, MulDiv
            return (int)(1.5f * size);
        }
    }
}
