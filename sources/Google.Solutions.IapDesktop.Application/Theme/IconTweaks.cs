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

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    internal class IconTweaks
    {
        /// <summary>
        /// Invert and adjust all grays in an image. For images that are mostly gray,
        /// combined with an accent color, this results in an image that works well
        /// on dark backgrounds.
        /// 
        /// Adapted and extended from:
        /// https://stackoverflow.com/questions/36778989/vs2015-icon-guide-color-inversion
        /// </summary>
        public static void InvertAndScaleGrays(Bitmap bitmapImage, float scaleFactor)
        {
            Debug.Assert(scaleFactor >= 0);
            Debug.Assert(scaleFactor <= 1);

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
                if (bitmapBGRA[i] == bitmapBGRA[i + 1] &&
                    bitmapBGRA[i] == bitmapBGRA[i + 2])
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
                    var scaledGray = ((1 - scaleFactor) * 255 + (invertedGray * scaleFactor));

                    bitmapBGRA[i] =     (byte)scaledGray;
                    bitmapBGRA[i + 1] = (byte)scaledGray;
                    bitmapBGRA[i + 2] = (byte)scaledGray;
                }
                else
                {
                    //
                    // Non-gray. Assume this is an accent color, and keep
                    // it unchanged.
                    //
                }
            }

            var bitmapWrite = bitmapImage.LockBits(
                dimensions, 
                ImageLockMode.WriteOnly, 
                PixelFormat.Format32bppPArgb);
            
            Marshal.Copy(bitmapBGRA, 0, bitmapWrite.Scan0, bitmapLength);
            bitmapImage.UnlockBits(bitmapWrite);
        }

        public static void InvertAndScaleGrays(ImageList imageList, float scaleFactor)
        {
            if (imageList == null)
            {
                return;
            }

            var images = imageList.Images.Cast<Image>().ToList();
            imageList.Images.Clear();
            
            foreach (var image in images)
            {
                InvertAndScaleGrays((Bitmap)image, scaleFactor);
                imageList.Images.Add(image);
            }
        }
    }
}
