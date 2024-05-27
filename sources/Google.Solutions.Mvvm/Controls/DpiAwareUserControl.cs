using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    public class DpiAwareUserControl : UserControl
    {
        //private Size? unscaledSize;

        //internal bool Rescaled { get; private set; } = false;

        //public new Size Size
        //{
        //    get => base.Size;
        //    set
        //    {
        //        this.unscaledSize = value;

        //        base.Size = value;
        //    }
        //}

        //protected override void OnHandleCreated(EventArgs e)
        //{
        //    base.OnHandleCreated(e);
        //}

        //protected override void OnLayout(LayoutEventArgs e)
        //{
        //    base.OnLayout(e);
        //}

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
        }

        //protected override void OnResize(EventArgs e)
        //{
        //    base.OnResize(e);
        //}

        private bool rescalingDone = false;

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

                this.rescalingDone = true;
            }
        }

        //private AnchorStyles anchor;

        //public override AnchorStyles Anchor 
        //{
        //    get => AnchorStyles.None;
        //    set => this.anchor = value; 
        //}


        public void MitigateWinformsBug()
        {


            //if (!this.Rescaled && this.unscaledSize != null && this.Size != this.unscaledSize)
            //{
            //    //
            //    // Disable anchors and force the control back to its original size.
            //    //
            //    var originalAnchor = this.Anchor;
            //    this.Anchor = AnchorStyles.None;
            //    this.AutoScaleMode = AutoScaleMode.None;

            //    var scaledSize = this.Size;
            //    this.Size = this.unscaledSize.Value;

            //    //
            //    // Turn on anchor again and force rescale.
            //    //
            //    this.Anchor = originalAnchor;
            //    this.Size = scaledSize;
                
            //    // TODO: only when anchored?


            //    this.Rescaled = true;
            //}
        }
    }
}
