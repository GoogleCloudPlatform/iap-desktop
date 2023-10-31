using Google.Solutions.Common.Util;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Theming rules for DPI-awareness.
    /// </summary>
    public class DpiAwarenessRuleset : ControlTheme.IRuleSet
    {
        private readonly DeviceCapabilities deviceCaps;

        private readonly Font UiFont;
        private readonly Font UiFontUnscaled;
        private readonly SizeF UiFontDimensions;

        public DpiAwarenessRuleset()
        {
            //
            // Get system DPI and use this for scaling operations.
            //
            this.deviceCaps = DeviceCapabilities.GetScreenCapabilities();

            //
            // Use Segoe instead of the legacy Microsoft Sans Serif.
            //
            // NB. We must set the initial size based on the current DPI settings.
            // 

            this.UiFont = new Font(
                new FontFamily("Segoe UI"),
               (9f * this.deviceCaps.SystemDpi) / DeviceCapabilities.DefaultDpi);
            this.UiFontUnscaled = new Font(
                new FontFamily("Segoe UI"),
               9f);
            this.UiFontDimensions = new SizeF(7f, 15f);

            //this.UiFont = SystemFonts.DefaultFont;
            //this.UiFontDimensions = new SizeF(6f, 13f);
        }

        //---------------------------------------------------------------------
        // Helper methods for DPI calculation.
        //---------------------------------------------------------------------

        private static int MulDiv(int number, int numerator, int denominator)
        {
            return (int)(((long)number * numerator) / denominator);
        }

        private Size ScaleToSystemDpi(Size size)
        {
            return new Size(
                MulDiv(size.Width, this.deviceCaps.SystemDpi, DeviceCapabilities.DefaultDpi),
                MulDiv(size.Height, this.deviceCaps.SystemDpi, DeviceCapabilities.DefaultDpi));
        }

        private Padding ScaleToSystemDpi(Padding padding)
        {
            return new Padding(
                MulDiv(padding.Left, this.deviceCaps.SystemDpi, DeviceCapabilities.DefaultDpi),
                MulDiv(padding.Top, this.deviceCaps.SystemDpi, DeviceCapabilities.DefaultDpi),
                MulDiv(padding.Right, this.deviceCaps.SystemDpi, DeviceCapabilities.DefaultDpi),
                MulDiv(padding.Bottom, this.deviceCaps.SystemDpi, DeviceCapabilities.DefaultDpi));
        }

        private int ScaleToSystemDpi(int size)
        {
            return MulDiv(size, this.deviceCaps.SystemDpi, DeviceCapabilities.DefaultDpi);
        }

        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        private void PrepareControlForFontSizing(Control c)
        {

            // Cf https://stackoverflow.com/questions/22735174/how-to-write-winforms-code-that-auto-scales-to-system-font-and-dpi-settings

            if (c is ContainerControl container)
            {
                //
                // All ContainerControls must be set to the same AutoScaleMode = Font.
                //
                // (Font will handle both DPI changes and changes to the system font size
                // setting; DPI will only handle DPI changes, not changes to the system
                // font size setting.)
                //
                container.AutoScaleMode = AutoScaleMode.Font;

                //
                // All ContainerControls must be set with the same AutoScaleDimensions.
                //
                // NB. Dimension must match the font.
                //
                container.AutoScaleDimensions = this.UiFontDimensions;
            }
            else  
            //else if (
            //    c is Label ||
            //    c is CheckBox ||
            //    c is RadioButton ||
            //    c is Button ||
            //    c is TextBoxBase ||
            //    c is ListBox ||
            //    c is ListView ||
            //    c is ComboBox ||
            //    c is TabControl ||
            //    c is TreeView)
            { 
                //
                // These controls have their font size scaled by the system.
                //
                c.Font = this.UiFontUnscaled;// Control.DefaultFont;
            }
            //else if (c.Font == Control.DefaultFont)
            //{
            //    // TODO: cache font.
            //    var oldFont = c.Font;
            //    c.Font = new Font(
            //        oldFont.FontFamily,
            //        (oldFont.Size * this.deviceCaps.SystemDpi) / DeviceCapabilities.DefaultDpi,
            //        oldFont.Style);
            //}
        }

        private void StylePictureBox(PictureBox pictureBox)
        {
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void StyleToolStrip(ToolStrip toolStrip)
        {
            toolStrip.ImageScalingSize = ScaleToSystemDpi(toolStrip.ImageScalingSize);
            // TODO: margin is too small
            toolStrip.Font = this.UiFontUnscaled;
        }
        private void StyleTextBox(TextBoxBase textBox)
        {
            if (textBox.Multiline)
            {
                // Quirk: adjust height
                textBox.Height /= 4; //TODO: What's the right factor here?
            }
        }

        private void ForceRescaleForm(Form c)
        {
            if (c.Parent != null)
            {
                //
                // Top-level window. Force scaling and relayout
                // (after the form's handle has been created).
                //
                c.Font = this.UiFont;
            }
        }

        //private void ScaleControl(Control c)
        //{
        //    var location = c.Location;
        //    var size = c.Size;

        //    if (c.Dock.HasFlag(DockStyle.Fill))
        //    {
        //        return;
        //    }

        //    //
        //    // Resize horizontally.
        //    //
        //    if (c.Anchor.HasFlag(AnchorStyles.Right))
        //    {
        //        if (c.Anchor.HasFlag(AnchorStyles.Left))
        //        {
        //            //
        //            // Let auto-layout will take care of it.
        //            //
        //        }
        //        else
        //        {
        //            var newWidth = ScaleToSystemDpi(size.Width);

        //            //
        //            // Move left to maintain proportions.
        //            //
        //            var marginRight = c.Parent.ClientRectangle.Width - location.X - size.Width;
        //            location.X = c.Parent.ClientRectangle.Width - newWidth - ScaleToSystemDpi(marginRight);

        //            size.Width = newWidth;
        //        }
        //    }
        //    else if (c.Anchor.HasFlag(AnchorStyles.Left))
        //    {
        //        if (c.Anchor.HasFlag(AnchorStyles.Right))
        //        {
        //            //
        //            // Let auto-layout will take care of it.
        //            //
        //        }
        //        else
        //        {
        //            location.X = ScaleToSystemDpi(location.X);
        //            size.Width = ScaleToSystemDpi(size.Width);
        //        }
        //    }

        //    //
        //    // Resize vertically.
        //    //
        //    if (c.Anchor.HasFlag(AnchorStyles.Top))
        //    {
        //        if (c.Anchor.HasFlag(AnchorStyles.Bottom))
        //        {
        //            //
        //            // Shrink to maintain bottom margin.
        //            //
        //            var marginBottom = c.Parent.ClientRectangle.Height - location.Y - size.Height;
        //            size.Height -= (ScaleToSystemDpi(marginBottom) - marginBottom);
        //        }
        //        else
        //        {
        //            location.Y = ScaleToSystemDpi(location.Y);
        //            size.Height = ScaleToSystemDpi(size.Height);
        //        }
        //    }
        //    else if (c.Anchor.HasFlag(AnchorStyles.Bottom))
        //    {
        //        if (c.Anchor.HasFlag(AnchorStyles.Top))
        //        {
        //            //
        //            // Let auto-layout will take care of it.
        //            //
        //        }
        //        else
        //        {
        //            var newHeight = ScaleToSystemDpi(size.Height);

        //            //
        //            // Move up to maintain proportions.
        //            //
        //            var marginBottom = c.Parent.ClientRectangle.Height - location.Y - size.Height;
        //            location.Y -= (ScaleToSystemDpi(marginBottom) - marginBottom);

        //            size.Height = newHeight;
        //        }
        //    }

        //    c.Location = location;
        //    c.Size = size;
        //}

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));

            if (this.deviceCaps.IsHighDpiEnabled)
            {
                //
                // Ensure that controls are properly configured
                // before their handle is created.
                //
                controlTheme.AddRule<Control>(PrepareControlForFontSizing);
                controlTheme.AddRule<PictureBox>(StylePictureBox);
                controlTheme.AddRule<ToolStrip>(StyleToolStrip);
                controlTheme.AddRule<TextBoxBase>(StyleTextBox);

                //
                // Force scaling once the handle has been created.
                //
                controlTheme.AddRule<Form>(ForceRescaleForm, ControlTheme.Options.ApplyWhenHandleCreated);
            }
        }
    }

    //---------------------------------------------------------------------
    // Helper classes.
    //---------------------------------------------------------------------

    internal class DeviceCapabilities
    {
        public const ushort DefaultDpi = 96;

        public ushort SystemDpi { get; }

        public bool IsHighDpiEnabled
        {
            get => this.SystemDpi != DefaultDpi;
        }

        private DeviceCapabilities(ushort systemDpi)
        {
            this.SystemDpi = systemDpi;
        }

        public static DeviceCapabilities GetScreenCapabilities()
        {
            var hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                return new DeviceCapabilities(
                    (ushort)NativeMethods.GetDeviceCaps(
                        hdc,
                        NativeMethods.DeviceCap.LOGPIXELSX));
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }
    }

    internal class NativeMethods
    {
        internal enum DeviceCap : int
        {
            LOGPIXELSX = 88,
            LOGPIXELSY = 90
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(
            IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(
            IntPtr hwnd,
            IntPtr hdc);

        [DllImport("gdi32.dll")] 
        public static extern int GetDeviceCaps(
            IntPtr hdc,
            DeviceCap nIndex);
    }
}
