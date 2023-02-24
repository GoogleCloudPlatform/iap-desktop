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
using Google.Solutions.Mvvm.Drawing;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    /// <summary>
    /// Invert and adjusts icon colors to optimize them for different
    /// lightness modes (dark mode/light mode).
    /// </summary>
    public class IconInverter
    {
        private float grayFactor = 1;
        private float colorFactor = 1;

        /// <summary>
        /// Factor to apply to grays.
        /// </summary>
        public float GrayFactor
        {
            get => this.grayFactor;
            set
            {
                this.grayFactor = value.ThrowIfOutOfRange(0.0f, 1.0f, nameof(GrayFactor));
            }
        }

        /// <summary>
        /// Factor to apply to the luminosity of (non-gray) colors.
        /// </summary>
        public float ColorFactor
        {
            get => this.colorFactor;
            set
            {
                this.colorFactor = value.ThrowIfOutOfRange(0.0f, 1.0f, nameof(ColorFactor));
            }
        }

        /// <summary>
        /// Invert and adjust colors:
        /// - Grays are inverted and adjusted using the GrayFactor.
        /// - Colors have their luminosity adjusted using the Color factor.
        /// </summary>
        public void Invert(Bitmap bitmapImage)
        {
            var dimensions = new Rectangle(
                0,
                0,
                bitmapImage.Width,
                bitmapImage.Height);

            var bitmapRead = bitmapImage.LockBits(
                dimensions, 
                ImageLockMode.ReadOnly, 
                PixelFormat.Format32bppPArgb);
            var bitmapLength = bitmapRead.Stride * bitmapRead.Height;

            var bitmapBGRA = new byte[bitmapLength];
            Marshal.Copy(bitmapRead.Scan0, bitmapBGRA, 0, bitmapLength);
            bitmapImage.UnlockBits(bitmapRead);

            for (int i = 0; i < bitmapLength; i += 4)
            {
                var red = bitmapBGRA[i + 2];
                var green = bitmapBGRA[i + 1];
                var blue = bitmapBGRA[i];

                if (red == green && green == blue)
                {
                    //
                    // This is gray.
                    //
                    var gray = bitmapBGRA[i];

                    //
                    // Invert.
                    //
                    var invertedGray = 255 - gray;

                    //
                    // Adjust the intensity based on a linear function to make
                    // the grays brighter:
                    //
                    // |            --- 
                    // |        ----    
                    // |    ----
                    // |----
                    // |
                    // |
                    // +---------------
                    //
                    var scaledGray = ((1 - this.grayFactor) * 255 + (invertedGray * this.grayFactor));

                    bitmapBGRA[i] = (byte)scaledGray;
                    bitmapBGRA[i + 1] = (byte)scaledGray;
                    bitmapBGRA[i + 2] = (byte)scaledGray;
                }
                else
                {
                    //
                    // This is some non-gray color. Adjust luminosity.
                    //
                    var hsl = HslColor.FromRgb(red, green, blue);

                    hsl.L = ((1 - this.colorFactor) + (hsl.L * this.colorFactor));

                    var rgb = hsl.ToColor();
                    bitmapBGRA[i + 0] = rgb.B;
                    bitmapBGRA[i + 1] = rgb.G;
                    bitmapBGRA[i + 2] = rgb.R;
                }
            }

            var bitmapWrite = bitmapImage.LockBits(
                dimensions, 
                ImageLockMode.WriteOnly, 
                PixelFormat.Format32bppPArgb);
            
            Marshal.Copy(bitmapBGRA, 0, bitmapWrite.Scan0, bitmapLength);
            bitmapImage.UnlockBits(bitmapWrite);
        }

        /// <summary>
        /// Invert and adjust colors of all images in an image list.
        /// </summary>
        public void Invert(ImageList imageList)
        {
            if (imageList == null)
            {
                return;
            }

            var images = imageList.Images.Cast<Image>().ToList();
            imageList.Images.Clear();
            
            foreach (var image in images)
            {
                Invert((Bitmap)image);
                imageList.Images.Add(image);
            }
        }
    }
}
