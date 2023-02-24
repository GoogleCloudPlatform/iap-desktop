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


using System.Drawing;

namespace Google.Solutions.Mvvm.Drawing
{
    public static class ColorExtensions
    {
        public static uint ToCOLORREF(this Color color)
        {
            return (((uint)color.R) | (((uint)color.G) << 8) | (((uint)color.B) << 16));
        }

        public static Color FromCOLORREF(uint color)
        {
            //
            // COLORREF uses 0x00bbggrr encoding, which is
            // different from ARGB.
            //
            return Color.FromArgb(
                (int)(color & 0xFF),
                (int)((color & 0xFF00) >> 8),
                (int)((color & 0xFF0000) >> 16));
        }

        public static HslColor ToHsl(this Color color)
        {
            return HslColor.FromColor(color);
        }
    }
}
