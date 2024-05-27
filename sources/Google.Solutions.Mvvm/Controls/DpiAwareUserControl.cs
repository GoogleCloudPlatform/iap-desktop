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



        private AnchorStyles anchor;

        public override AnchorStyles Anchor 
        {
            get => AnchorStyles.None;
            set => this.anchor = value; 
        }


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
