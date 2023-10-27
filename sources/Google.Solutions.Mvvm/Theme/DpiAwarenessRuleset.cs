using Google.Solutions.Common.Util;
using Microsoft.Win32.SafeHandles;
using System;
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

        public DpiAwarenessRuleset()
        {
            //
            // Get system DPI and use this for scaling operations.
            //
            this.deviceCaps = DeviceCapabilities.GetScreenCapabilities();
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

        private void ScaleControlFont(Control c)
        {
            if (c is Form)
            {
                //
                // Changing the font size of the form changes the layout.
                //
            }
            else if (c is Form ||
                c is Label ||
                c is CheckBox ||
                c is RadioButton ||
                c is Button)
            {
                //
                // These controls have their font size scaled by the system.
                //
                c.Font = Control.DefaultFont;
            }
            else if (c.Font == Control.DefaultFont)
            {
                // TODO: cache font.
                var oldFont = c.Font;
                c.Font = new Font(
                    oldFont.FontFamily,
                    (oldFont.Size * this.deviceCaps.SystemDpi) / DeviceCapabilities.DefaultDpi,
                    oldFont.Style);
            }
        }

        private void ScaleControl(Control c)
        {
            var location = c.Location;
            var size = c.Size;

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
                    var newWidth = ScaleToSystemDpi(size.Width);

                    //
                    // Move left to maintain proportions.
                    //
                    var marginRight = c.Parent.ClientRectangle.Width - location.X - size.Width;
                    location.X = c.Parent.ClientRectangle.Width - newWidth - ScaleToSystemDpi(marginRight);

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
                    location.X = ScaleToSystemDpi(location.X);
                    size.Width = ScaleToSystemDpi(size.Width);
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
                    // Shrink to maintain bottom margin.
                    //
                    var marginBottom = c.Parent.ClientRectangle.Height - location.Y - size.Height;
                    size.Height -= (ScaleToSystemDpi(marginBottom) - marginBottom);
                }
                else
                {
                    location.Y = ScaleToSystemDpi(location.Y);
                    size.Height = ScaleToSystemDpi(size.Height);
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
                    var newHeight = ScaleToSystemDpi(size.Height);

                    //
                    // Move up to maintain proportions.
                    //
                    var marginBottom = c.Parent.ClientRectangle.Height - location.Y - size.Height;
                    location.Y -= (ScaleToSystemDpi(marginBottom) - marginBottom);

                    size.Height = newHeight;
                }
            }

            c.Location = location;
            c.Size = size;
        }

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));

            if (this.deviceCaps.IsHighDpiEnabled)
            {
                controlTheme.AddRule<Control>(ScaleControl);
                controlTheme.AddRule<Control>(ScaleControlFont);
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
            get => SystemDpi != DefaultDpi;
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
