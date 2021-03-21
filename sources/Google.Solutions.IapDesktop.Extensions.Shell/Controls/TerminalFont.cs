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
    internal sealed class TerminalFont : IDisposable
    {
        public const string FontFamily = "Consolas";

        public const TextFormatFlags FormatFlags =
            TextFormatFlags.NoPadding |
            TextFormatFlags.NoPrefix |
            TextFormatFlags.PreserveGraphicsClipping;

        internal Font Font {  get; private set; }

        //---------------------------------------------------------------------
        // Statics.
        //---------------------------------------------------------------------

        public static bool IsValidFont(Font font)
        {
            return font.FontFamily.Name == FontFamily;
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public TerminalFont(float emSize)
        {
            this.Font = new Font(FontFamily, emSize);
        }

        public TerminalFont() : this(9.75f)
        { }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public SizeF Measure(Graphics graphics, string text)
        {
            return graphics.MeasureString(
                text,
                this.Font,
                new Size(short.MaxValue, short.MaxValue),
                StringFormat.GenericTypographic);
        }

        public SizeF Measure(Graphics graphics, int numberOfChars)
            => Measure(graphics, new string('m', numberOfChars));

        public TerminalFont NextSmallerFont()
        {
            return new TerminalFont(this.Font.Size - 1);
        }

        public TerminalFont NextLargerFont()
        {
            return new TerminalFont(this.Font.Size + 1);
        }

        public void DrawString(
            Graphics graphics,
            PointF point,
            string text,
            FontStyle fontStyle,
            Color foregroundColor)
        {
            using (var brush = new SolidBrush(foregroundColor))
            using (var font = new Font(this.Font, fontStyle))
            {
                graphics.DrawString(
                    text,
                    font,
                    brush,
                    point,
                    StringFormat.GenericTypographic);
            }
        }

        public int MeasureColumns(Graphics graphics, int width)
        {
            var sampleSize = Measure(graphics, 100);
            var widthOfChar = sampleSize.Width / 100;
            return (int)Math.Floor(width / widthOfChar);
        }

        public int MeasureRows(Graphics graphics, int height)
        {
            var sampleSize = Measure(graphics, 1);
            return (int)Math.Floor(height / sampleSize.Height);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.Font.Dispose();
        }
    }
}
