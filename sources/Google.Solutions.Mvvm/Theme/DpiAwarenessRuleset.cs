using Google.Solutions.Common.Util;
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
        private static int MulDiv(int number, int numerator, int denominator)
        {
            return (int)(((long)number * numerator) / denominator);
        }

        private static Size ScaleToSystemDpi(Size size)
        {
            return new Size(
                MulDiv(size.Width, DeviceCaps.SystemDpi, DeviceCaps.DefaultDpi),
                MulDiv(size.Height, DeviceCaps.SystemDpi, DeviceCaps.DefaultDpi));
        }

        private static int ScaleToSystemDpi(int size)
        {
            return MulDiv(size, DeviceCaps.SystemDpi, DeviceCaps.DefaultDpi);
        }

        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        private void ScaleForm(Form form)
        {
            //
            // NB. Form.DeviceDpi might not be set correctly, so
            // use the system DPI.
            //
            //form.Size = ScaleToSystemDpi(form.Size);
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
                    // Let auto-layout will take care of it.
                    //
                }
                else
                {
                    location.X = ScaleToSystemDpi(location.X);
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
                    location.Y = c.Parent.ClientRectangle.Height - newHeight - ScaleToSystemDpi(marginBottom);

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

            if (DeviceCaps.IsHighDpiEnabled)
            {
                controlTheme.AddRule<Form>(ScaleForm);
                controlTheme.AddRule<Control>(ScaleControl);
            }
        }
    }

    //---------------------------------------------------------------------
    // Helper classes.
    //---------------------------------------------------------------------

    //internal static class HighDpiExtensions
    //{


    //    //internal static Size ScaleToDeviceDpi(this Control c, Size size)
    //    //{
    //    //    return ScaleToDpi(c, size, (ushort)c.DeviceDpi);
    //    //}

    //    //internal static int ScaleToDeviceDpi(this Control c, int size)
    //    //{
    //    //    return MulDiv(size, c.DeviceDpi, DeviceCaps.DefaultDpi);
    //    //}
    //}

    internal class DeviceCaps
    {
        public const ushort DefaultDpi = 96;

        public static ushort SystemDpi { get; }

        public static bool IsHighDpiEnabled
        {
            get => SystemDpi != DefaultDpi;
        }

        static DeviceCaps() 
        {
            var hdc = NativeMethods.GetDC(IntPtr.Zero);
            SystemDpi = (ushort)NativeMethods.GetDeviceCaps(
                hdc, 
                NativeMethods.DeviceCap.LOGPIXELSX);

            NativeMethods.ReleaseDC(IntPtr.Zero, hdc); //TODO: use dispose 
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
