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

using System.Drawing;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Color table for a virtual terminal.
    /// </summary>
    public struct VirtualTerminalColors
    {
        public Color Black { get; set; }
        public Color Red { get; set; }
        public Color Green { get; set; }
        public Color Yellow { get; set; }
        public Color Blue { get; set; }
        public Color Purple { get; set; }
        public Color Cyan { get; set; }
        public Color White { get; set; }
        public Color BrightBlack { get; set; }
        public Color BrightRed { get; set; }
        public Color BrightGreen { get; set; }
        public Color BrightYellow { get; set; }
        public Color BrightBlue { get; set; }
        public Color BrightPurple { get; set; }
        public Color BrightCyan { get; set; }
        public Color BrightWhite { get; set; }

        /// <summary>
        /// Convert to representation as used by the Windows Terminal.
        /// </summary>
        internal uint[] ToNative()
        {
            return new uint[]
            {
                (uint)ColorTranslator.ToWin32(this.Black),
                (uint)ColorTranslator.ToWin32(this.Red),
                (uint)ColorTranslator.ToWin32(this.Green),
                (uint)ColorTranslator.ToWin32(this.Yellow),
                (uint)ColorTranslator.ToWin32(this.Blue),
                (uint)ColorTranslator.ToWin32(this.Purple),
                (uint)ColorTranslator.ToWin32(this.Cyan),
                (uint)ColorTranslator.ToWin32(this.White),
                (uint)ColorTranslator.ToWin32(this.BrightBlack),
                (uint)ColorTranslator.ToWin32(this.BrightRed),
                (uint)ColorTranslator.ToWin32(this.BrightGreen),
                (uint)ColorTranslator.ToWin32(this.BrightYellow),
                (uint)ColorTranslator.ToWin32(this.BrightBlue),
                (uint)ColorTranslator.ToWin32(this.BrightPurple),
                (uint)ColorTranslator.ToWin32(this.BrightCyan),
                (uint)ColorTranslator.ToWin32(this.BrightWhite),
            };
        }

        /// <summary>
        /// Default color scheme, equivalent to the "Campbell" theme
        /// used by the Windows Terminal.
        /// <see href="https://learn.microsoft.com/en-us/windows/terminal/customize-settings/color-schemes"/>
        /// </summary>
        public static VirtualTerminalColors Default
        {
            get => new VirtualTerminalColors()
            {
                Black = Color.FromArgb(0x0C, 0x0C, 0x0C),
                Red = Color.FromArgb(0xC5, 0x0F, 0x1F),
                Green = Color.FromArgb(0x13, 0xA1, 0x0E),
                Yellow = Color.FromArgb(0xC1, 0x9C, 0x00),
                Blue = Color.FromArgb(0x00, 0x37, 0xDA),
                Purple = Color.FromArgb(0x88, 0x17, 0x98),
                Cyan = Color.FromArgb(0x3A, 0x96, 0xDD),
                White = Color.FromArgb(0xCC, 0xCC, 0xCC),
                BrightBlack = Color.FromArgb(0x76, 0x76, 0x76),
                BrightRed = Color.FromArgb(0xE7, 0x48, 0x56),
                BrightGreen = Color.FromArgb(0x16, 0xC6, 0x0C),
                BrightYellow = Color.FromArgb(0xF9, 0xF1, 0xA5),
                BrightBlue = Color.FromArgb(0x3B, 0x78, 0xFF),
                BrightPurple = Color.FromArgb(0xB4, 0x00, 0x9E),
                BrightCyan = Color.FromArgb(0x61, 0xD6, 0xD6),
                BrightWhite = Color.FromArgb(0xF2, 0xF2, 0xF2)
            };
        }
    }
}
