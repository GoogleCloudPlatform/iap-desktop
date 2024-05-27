using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    public class DpiAwareUserControl : UserControl
    {
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            if (this.Parent == null)
            {
                //
                // Not parented yet, bug doesn't apply.
                //
                base.ScaleControl(factor, specified);
            }
            else
            {
                //
                // Resize anchored controls.
                //

                // TODO: Consider Dock = Fill

                var horizontallyAnchoredControls = this.Controls
                    .OfType<Control>()
                    .Where(c => c.Anchor.HasFlag(AnchorStyles.Left | AnchorStyles.Right))
                    .Select(c => new {
                        Control = c,
                        RightMargin = this.Width - c.Location.X - c.Width
                    })
                    .ToList();
                var verticallyAnchoredControls = this.Controls
                    .OfType<Control>()
                    .Where(c => c.Anchor.HasFlag(AnchorStyles.Top | AnchorStyles.Bottom))
                    .Select(c => new {
                        Control = c,
                        BottomMargin = this.Width - c.Location.X - c.Width
                    })
                    .ToList();

                base.ScaleControl(factor, specified);

                foreach (var c in horizontallyAnchoredControls)
                {
                    c.Control.Width = this.Width - c.Control.Location.X - c.RightMargin;
                }

                foreach (var c in verticallyAnchoredControls)
                {
                    c.Control.Height = this.Height - c.Control.Location.Y - c.BottomMargin;
                }
            }
        }
    }
}
