//
// Copyright 2023 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Common.Util;
using System;
using System.Drawing;
using System.Linq;
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
            // Use Segoe UI instead of the legacy Microsoft Sans Serif.
            //
            // NB. We must set the initial size based on the current DPI settings.
            // 
            var fontFamily = new FontFamily("Segoe UI");
            this.UiFont = new Font(
                fontFamily,
                (9f * this.deviceCaps.SystemDpi) / DeviceCapabilities.DefaultDpi);
            this.UiFontUnscaled = new Font(
                fontFamily,
                9f);

            //
            // NB. Dimension must match the font family.
            //
            this.UiFontDimensions = new SizeF(7f, 15f);
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
                // Let the system use font-based autoscaling.
                //
                // (This will handle both DPI changes and changes to the system font size
                // setting; DPI will only handle DPI changes, not changes to the system
                // font size setting.)
                //
                container.AutoScaleMode = AutoScaleMode.Font;
                container.AutoScaleDimensions = this.UiFontDimensions;
            }
            else  
            { 
                //
                // These controls have their font size scaled by the system.
                // But for that to work, we habe to reassign the unscaled
                // font.
                //
                c.Font = this.UiFontUnscaled;
            }
        }

        private void StylePictureBox(PictureBox pictureBox)
        {
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void StyleToolStrip(ToolStrip toolStrip)
        {
            toolStrip.ImageScalingSize = ScaleToSystemDpi(toolStrip.ImageScalingSize);
            // TODO: margin is too small
            //toolStrip.Font = this.UiFontUnscaled;
        }

        private void StyleTextBox(TextBoxBase textBox)
        {
            if (textBox.Multiline)
            {
                // Quirk: adjust height
                textBox.Height /= 4; //TODO: What's the right factor here?
            }
        }

        private void StyleTreeView(TreeView treeView)
        {
            void ScaleImageList(ImageList imageList)
            {
                var images = imageList
                    .Images
                    .Cast<Image>()
                    .Select(i => (Image)i.Clone())
                    .ToArray();

                //
                // Change the size. If the handle has been created already,
                // this causes the imagelist to reset the contained images.
                //
                treeView.ImageList.ImageSize = ScaleToSystemDpi(treeView.ImageList.ImageSize);

                if (treeView.ImageList.Images.Count != images.Length)
                {
                    //
                    // Re-add images.
                    //
                    treeView.ImageList.Images.AddRange(images);
                }
            }

            if (treeView.ImageList != null)
            {
                ScaleImageList(treeView.ImageList);
            }

            if (treeView.StateImageList != null)
            {
                ScaleImageList(treeView.StateImageList);
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
                // Avoid doing the same for child window as that
                // would cause duplicate scaling.
                //
                c.Font = this.UiFont;
            }
        }

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
                controlTheme.AddRule<TreeView>(StyleTreeView);

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
