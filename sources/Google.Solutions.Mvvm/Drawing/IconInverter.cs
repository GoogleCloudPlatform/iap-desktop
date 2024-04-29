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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Drawing
{
    /// <summary>
    /// Invert and adjusts icon colors to optimize them for different
    /// lightness modes (dark mode/light mode).
    /// </summary>
    public class IconInverter
    {
        private static readonly object invertedTag = new object();
        private float grayFactor = 1;
        private float colorFactor = 1;

        private static bool IsGrayish(byte red, byte green, byte blue)
        {
            var rgb = new[] { red, green, blue };
            return rgb.Max() - rgb.Min() < 10;
        }

        /// <summary>
        /// Luminosity factor to apply to grays.
        /// </summary>
        public float GrayFactor
        {
            get => this.grayFactor;
            set
            {
                this.grayFactor = value.ExpectInRange(0.0f, 1.0f, nameof(this.GrayFactor));
            }
        }

        /// <summary>
        /// Luminosity factor to apply to (non-gray) colors.
        /// </summary>
        public float ColorFactor
        {
            get => this.colorFactor;
            set
            {
                this.colorFactor = value.ExpectInRange(0.0f, 1.0f, nameof(this.ColorFactor));
            }
        }

        /// <summary>
        /// Add a marker pixel at (0, 0) to indicate that this icon was inverted.
        /// For testing only.
        /// </summary>
        public bool MarkerPixel { get; set; } = false;

        /// <summary>
        /// Invert gray-ish colors.
        /// </summary>
        public bool InvertGray { get; set; } = true;

        /// <summary>
        /// Invert and adjust colors:
        /// - Grays are inverted and adjusted using the GrayFactor.
        /// - Colors have their luminosity adjusted using the Color factor.
        /// </summary>
        /// <returns>true if inverted, false if it was inverted before.</returns>
        public bool Invert(Bitmap bitmapImage)
        {
            Debug.Assert(
                bitmapImage.Tag == null || bitmapImage.Tag == invertedTag,
                "Images has existing tag");

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

            if (bitmapImage.Tag == invertedTag)
            {
                //
                // Icon has been inverted already.
                //
                return false;
            }

            for (var i = 0; i < bitmapLength; i += 4)
            {
                var red = bitmapBGRA[i + 2];
                var green = bitmapBGRA[i + 1];
                var blue = bitmapBGRA[i];

                HslColor hsl;

                if (IsGrayish(red, green, blue))
                {
                    //
                    // This is gray.
                    //
                    var gray = bitmapBGRA[i];

                    //
                    // Invert.
                    //
                    var invertedGray = this.InvertGray
                        ? 255 - gray
                        : gray;


                    //
                    // Adjust luminosity.
                    //
                    hsl = HslColor.FromRgb(invertedGray, invertedGray, invertedGray);
                    hsl.L = ((1 - this.grayFactor) + (hsl.L * this.grayFactor));
                }
                else
                {
                    //
                    // Non-Gray color.
                    //
                    // Adjust luminosity.
                    //
                    hsl = HslColor.FromRgb(red, green, blue);
                    hsl.L = ((1 - this.colorFactor) + (hsl.L * this.colorFactor));
                }

                var rgb = hsl.ToColor();
                bitmapBGRA[i + 0] = rgb.B;
                bitmapBGRA[i + 1] = rgb.G;
                bitmapBGRA[i + 2] = rgb.R;
            }

            //
            // Add guard tag as indicator that this icon was inverted.
            //
            bitmapImage.Tag = invertedTag;

            if (this.MarkerPixel)
            {
                bitmapBGRA[0] = Color.Magenta.B;
                bitmapBGRA[1] = Color.Magenta.G;
                bitmapBGRA[2] = Color.Magenta.R;
                bitmapBGRA[3] = Color.Magenta.A;
            }

            var bitmapWrite = bitmapImage.LockBits(
                dimensions,
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppPArgb);

            Marshal.Copy(bitmapBGRA, 0, bitmapWrite.Scan0, bitmapLength);
            bitmapImage.UnlockBits(bitmapWrite);

            return true;
        }

        /// <summary>
        /// Invert and adjust colors of all images in an image list.
        /// </summary>
        /// <returns>true if inverted, false if it was inverted before.</returns>
        public bool Invert(ImageList imageList)
        {
            if (imageList == null)
            {
                return false;
            }

            Debug.Assert(
                imageList.Tag == null || imageList.Tag == invertedTag,
                "ImageList has existing tag");

            if (imageList.Tag == invertedTag)
            {
                //
                // Icon has been inverted already.
                // 
                // NB. This check seems redundant to the check we do 
                // for individual images. But when we invert an image
                // from an image list, the tag is lost. Therefore, we
                // must attach a tag to the image list as well.
                //
                return false;
            }

            //
            // Invert all icons.
            //
            var images = imageList.Images.Cast<Image>().ToList();
            imageList.Images.Clear();

            foreach (var image in images)
            {
                Invert((Bitmap)image);
                imageList.Images.Add(image);
            }

            //
            // Add guard tag as indicator that this icon was inverted.
            //
            imageList.Tag = invertedTag;

            return true;
        }
    }
}
