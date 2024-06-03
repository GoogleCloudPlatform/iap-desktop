//
// Copyright 2024 Google LLC
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

using Google.Solutions.Mvvm.Theme;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    public static class ImageListExtensions
    {
        /// <summary>
        /// Scale images in an ImageList.
        /// </summary>
        public static void ScaleToDpi(this ImageList imageList)
        {
            if (imageList == null)
            {
                return;
            }

            var originalImages = imageList
                .Images
                .Cast<Image>();

            try
            {
                var images = originalImages
                    .Select(i => (Image)i.Clone())
                    .ToArray();

                //
                // Change the size.
                //
                // If the handle has been created already, this causes the
                // imagelist to reset the contained images. If this happens,
                // we need to re-add them.
                //
                imageList.ColorDepth = ColorDepth.Depth32Bit;
                imageList.ImageSize = DeviceCapabilities.Current.ScaleToDpi(imageList.ImageSize);

                if (imageList.Images.Count != images.Length)
                {
                    //
                    // Re-add images.
                    //
                    imageList.Images.AddRange(images);
                }
            }
            finally
            {
                foreach (var image in originalImages)
                {
                    image.Dispose();
                }
            }
        }
    }
}
