//
// Copyright 2022 Google LLC
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
using System.Runtime.InteropServices;

namespace Google.Solutions.Mvvm.Drawing
{
    /// <summary>
    /// 16x16 icon that can be used as overlay.
    /// </summary>
    public sealed class BadgeIcon : IDisposable
    {
        public Color BackColor { get; }
        public IntPtr Handle { get; }

        private BadgeIcon(IntPtr handle, Color backColor)
        {
            this.Handle = handle;
            this.BackColor = backColor;
        }

        private static Color ColorFromText(string text)
        {
            var hue = (ushort)(text.GetHashCode() % 255);
            ushort lightness = 128;
            ushort saturation = 255;

            return ColorTranslator.FromWin32(
                NativeMethods.ColorHLSToRGB(hue, lightness, saturation));
        }

        public static BadgeIcon ForTextInitial(string text)
        {
            Precondition.ExpectNotEmpty(text, nameof(text));

            //
            // Generate a color based on the input text.
            //
            var backColor = ColorFromText(text);

            using (var bitmap = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var format = StringFormat.GenericTypographic)
            using (var font = new Font(FontFamily.GenericSansSerif, 6, FontStyle.Bold, GraphicsUnit.Point))
            using (var brush = new SolidBrush(backColor))
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;

                //
                // Draw text box at lower right corner.
                //
                var initial = text.Substring(0, 1).ToUpper();
                var textSize = graphics.MeasureString(initial, font, new PointF(0, 0), format);
                var textSizeWithMargin = textSize + new SizeF(5.0f, 2.0f);
                var textBox = new RectangleF(
                    new PointF(16, 16) - textSizeWithMargin,
                    textSizeWithMargin);

                graphics.FillRectangle(brush, textBox);
                graphics.DrawString(
                    initial,
                    font,
                    Brushes.White,
                    textBox,
                    format);

                return new BadgeIcon(bitmap.GetHicon(), backColor);
            }
        }

        public void Dispose()
        {
            NativeMethods.DestroyIcon(this.Handle);
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern bool DestroyIcon(IntPtr handle);

            [DllImport("shlwapi.dll")]
            public static extern int ColorHLSToRGB(ushort h, ushort l, ushort s);
        }

    }
}
