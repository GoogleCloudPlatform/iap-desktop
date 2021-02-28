//
// Copyright 2020 Google LLC
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

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Controls
{
    internal static class TerminalFont
    {
        public const string FontFamily = "Consolas";

        public static bool IsValidFont(Font font)
        {
            return font.FontFamily.Name == FontFamily;
        }

        public static SizeF GetCharacterSize(Font font)
        {
            if (!IsValidFont(font))
            {
                throw new ArgumentException(nameof(font));
            }

            //
            // NB. MeasureText gives us a precise measure of a character's 
            // hight, but not of its width. There are mutliple facors that
            // seem to be playing into how wide a (monospace) character is,
            // and MeasureText, for some reason, does not account for these.
            // Therefore, use a "magic" factor to derive the width from
            // a character's height.
            //
            // While only valid for Consolas, this factor yields sufficiently
            // precise results that allow the width to be used as a basis for
            // calculating screen corrdinates.
            //

            var sizeOfChar = TextRenderer.MeasureText(
                "X",
                font,
                new Size(short.MaxValue, short.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping);

            return new SizeF(
                font.Size * 0.75603f,   // Empirically determined ratio.
                sizeOfChar.Height);
        }

        public static Font NextSmallerFont(Font font)
        {
            if (!IsValidFont(font))
            {
                throw new ArgumentException(nameof(font));
            }

            return new Font(
                font.FontFamily,
                font.Size - 1);
        }

        public static Font NextLargerFont(Font font)
        {
            if (!IsValidFont(font))
            {
                throw new ArgumentException(nameof(font));
            }

            return new Font(
                font.FontFamily,
                font.Size + 1);
        }
    }
}
