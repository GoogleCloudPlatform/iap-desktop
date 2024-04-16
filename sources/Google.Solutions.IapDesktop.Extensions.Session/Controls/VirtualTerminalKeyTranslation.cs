//
// Copyright 2021 Google LLC
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

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using VtNetCore.VirtualTerminal;

#nullable disable

namespace Google.Solutions.IapDesktop.Extensions.Session.Controls
{
    internal static class VirtualTerminalKeyTranslation
    {
        /// <summary>
        /// Mapping that uses 0..1 modifiers.
        /// </summary>
        private class StandardMapping
        {
            public string Normal { get; set; }

            public string Shift { get; set; }

            public string Control { get; set; }

            public string Alt { get; set; }

            public virtual string Apply(bool alt, bool control, bool shift)
            {
                if (shift)
                {
                    return this.Shift;
                }
                else if (control)
                {
                    return this.Control;
                }
                else if (alt)
                {
                    return this.Alt;
                }
                else
                {
                    return this.Normal;
                }
            }
        }

        private const string Esc = "\u001b";
        private const string Ss3 = Esc + "O";
        private static readonly string DecimalSep = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;

        /// <summary>
        /// Standard Xterm key translations.
        /// </summary>
        private static readonly Dictionary<Keys, StandardMapping> StandardTranslations =
            new Dictionary<Keys, StandardMapping>
            {
                //
                // Function keys.
                //
                // Note that F1 through F4 are prefixed with SS3 , while the other keys are
                // prefixed with CSI. Older versions of xterm implement different escape
                // sequences for F1 through F4, with a CSI prefix.
                //
                { Keys.F1,       new StandardMapping { Normal = Ss3 + "P",    Shift = Esc + "[1;2P",  Control = Esc + "[1;5P",  Alt = Esc + "[1;3P" } },
                { Keys.F2,       new StandardMapping { Normal = Ss3 + "Q",    Shift = Esc + "[1;2Q",  Control = Esc + "[1;5Q",  Alt = Esc + "[1;3Q" } },
                { Keys.F3,       new StandardMapping { Normal = Ss3 + "R",    Shift = Esc + "[1;2R",  Control = Esc + "[1;5R",  Alt = Esc + "[1;3R" } },
                { Keys.F4,       new StandardMapping { Normal = Ss3 + "S",    Shift = Esc + "[1;2S",  Control = Esc + "[1;5S",  Alt = Esc + "[1;3S" } },
                { Keys.F5,       new StandardMapping { Normal = Esc + "[15~", Shift = Esc + "[15;2~", Control = Esc + "[15;5~", Alt = Esc + "[15;3~" } },
                { Keys.F6,       new StandardMapping { Normal = Esc + "[17~", Shift = Esc + "[17;2~", Control = Esc + "[17;5~", Alt = Esc + "[17;3~" } },
                { Keys.F7,       new StandardMapping { Normal = Esc + "[18~", Shift = Esc + "[18;2~", Control = Esc + "[18;5~", Alt = Esc + "[18;3~" } },
                { Keys.F8,       new StandardMapping { Normal = Esc + "[19~", Shift = Esc + "[19;2~", Control = Esc + "[19;5~", Alt = Esc + "[19;3~" } },
                { Keys.F9,       new StandardMapping { Normal = Esc + "[20~", Shift = Esc + "[20;2~", Control = Esc + "[20;5~", Alt = Esc + "[20;3~" } },
                { Keys.F10,      new StandardMapping { Normal = Esc + "[21~", Shift = Esc + "[21;2~", Control = Esc + "[21;5~", Alt = Esc + "[21;3~" } },
                { Keys.F11,      new StandardMapping { Normal = Esc + "[23~", Shift = Esc + "[23;2~", Control = Esc + "[23;5~", Alt = Esc + "[23;3~" } },
                { Keys.F12,      new StandardMapping { Normal = Esc + "[24~", Shift = Esc + "[24;2~", Control = Esc + "[24;5~", Alt = Esc + "[24;3~" } },

                //
                // Arrow keys.
                //
                { Keys.Up,       new StandardMapping { Normal = Esc + "[A",   Shift = Esc + "[1;2A",  Control = Esc + "[1;5A",  Alt = Esc + "[1;3A" } },
                { Keys.Down,     new StandardMapping { Normal = Esc + "[B",   Shift = Esc + "[1;2B",  Control = Esc + "[1;5B",  Alt = Esc + "[1;3B" } },
                { Keys.Right,    new StandardMapping { Normal = Esc + "[C",   Shift = Esc + "[1;2C",  Control = Esc + "[1;5C",  Alt = Esc + "[1;3C" } },
                { Keys.Left,     new StandardMapping { Normal = Esc + "[D",   Shift = Esc + "[1;2D",  Control = Esc + "[1;5D",  Alt = Esc + "[1;3D" } },
                { Keys.Home,     new StandardMapping { Normal = Esc + "[H",   Shift = Esc + "[1;2H",  Control = Esc + "[1;5H",  Alt = Esc + "[1;3H" } },
                { Keys.End,      new StandardMapping { Normal = Esc + "[F",   Shift = Esc + "[1;2F",  Control = Esc + "[1;5F",  Alt = Esc + "[1;3F" } },
                { Keys.Insert,   new StandardMapping { Normal = Esc + "[2~",  Shift = Esc + "[2;2~",  Control = Esc + "[2;5~",  Alt = Esc + "[2;3~" } },
                { Keys.Delete,   new StandardMapping { Normal = Esc + "[3~",  Shift = Esc + "[3;2~",  Control = Esc + "[3;5~",  Alt = Esc + "[3;3~" } },
                { Keys.PageUp,   new StandardMapping { Normal = Esc + "[5~",  Shift = Esc + "[5;2~",  Control = Esc + "[5;5~",  Alt = Esc + "[5;3~" } },
                { Keys.PageDown, new StandardMapping { Normal = Esc + "[6~",  Shift = Esc + "[6;2~",  Control = Esc + "[6;5~",  Alt = Esc + "[6;3~" } },

                //
                // Main keyboard.
                //
                { Keys.Back,    new StandardMapping { Normal = "\u007F",      Shift = "\u007F",       Control = "\b",           Alt = Esc + "\u007f" } },
                { Keys.Tab,     new StandardMapping { Normal = "\t",          Shift = Esc + "[Z",     Control = "\t"                                 } },
                { Keys.Return,  new StandardMapping { Normal = "\r",          Shift = "\r",           Control = "\r",           Alt = Esc + "\r" } },
                { Keys.Escape,  new StandardMapping { Normal = Esc,           Shift = Esc,            Control = Esc } },
                { Keys.Pause,   new StandardMapping { Normal = "\u001a",      Shift = "\u001a",                             Alt = Esc + "\u001a" } },
                { Keys.Space,   new StandardMapping {                                                 Control = "\u0000",   Alt = Esc + " " } },

                { Keys.A,       new StandardMapping { Control = "\u0001",     Alt = Esc + "a" } },
                { Keys.B,       new StandardMapping { Control = "\u0002",     Alt = Esc + "b" } },
                { Keys.C,       new StandardMapping { Control = "\u0003",     Alt = Esc + "c" } },
                { Keys.D,       new StandardMapping { Control = "\u0004",     Alt = Esc + "d" } },
                { Keys.E,       new StandardMapping { Control = "\u0005",     Alt = Esc + "e" } },
                { Keys.F,       new StandardMapping { Control = "\u0006",     Alt = Esc + "f" } },
                { Keys.G,       new StandardMapping { Control = "\u0007",     Alt = Esc + "g" } },
                { Keys.H,       new StandardMapping { Control = "\u0008",     Alt = Esc + "h" } },
                { Keys.I,       new StandardMapping { Control = "\u0009",     Alt = Esc + "i" } },
                { Keys.J,       new StandardMapping { Control = "\u000a",     Alt = Esc + "j" } },
                { Keys.K,       new StandardMapping { Control = "\u000b",     Alt = Esc + "k" } },
                { Keys.L,       new StandardMapping { Control = "\u000c",     Alt = Esc + "l" } },
                { Keys.M,       new StandardMapping { Control = "\u000d",     Alt = Esc + "m" } },
                { Keys.N,       new StandardMapping { Control = "\u000e",     Alt = Esc + "n" } },
                { Keys.O,       new StandardMapping { Control = "\u000f",     Alt = Esc + "o" } },
                { Keys.P,       new StandardMapping { Control = "\u0010",     Alt = Esc + "p" } },
                { Keys.Q,       new StandardMapping { Control = "\u0011",     Alt = Esc + "q" } },
                { Keys.R,       new StandardMapping { Control = "\u0012",     Alt = Esc + "r" } },
                { Keys.S,       new StandardMapping { Control = "\u0013",     Alt = Esc + "s" } },
                { Keys.T,       new StandardMapping { Control = "\u0014",     Alt = Esc + "t" } },
                { Keys.U,       new StandardMapping { Control = "\u0015",     Alt = Esc + "u" } },
                { Keys.V,       new StandardMapping { Control = "\u0016",     Alt = Esc + "v" } },
                { Keys.W,       new StandardMapping { Control = "\u0017",     Alt = Esc + "w" } },
                { Keys.X,       new StandardMapping { Control = "\u0018",     Alt = Esc + "x" } },
                { Keys.Y,       new StandardMapping { Control = "\u0019",     Alt = Esc + "y" } },
                { Keys.Z,       new StandardMapping { Control = "\u001a",     Alt = Esc + "z" } },

                { Keys.D0,      new StandardMapping {                         Alt = Esc + "0" } },
                { Keys.D1,      new StandardMapping {                         Alt = Esc + "1" } },
                { Keys.D2,      new StandardMapping { Control = "\u0000",     Alt = Esc + "2" } },
                { Keys.D3,      new StandardMapping { Control = Esc,          Alt = Esc + "3" } },
                { Keys.D4,      new StandardMapping { Control = "\u001c",     Alt = Esc + "4" } },
                { Keys.D5,      new StandardMapping { Control = "\u001d",     Alt = Esc + "5" } },
                { Keys.D6,      new StandardMapping { Control = "\u001e",     Alt = Esc + "6" } },
                { Keys.D7,      new StandardMapping { Control = "\u001f",     Alt = Esc + "7" } },
                { Keys.D8,      new StandardMapping { Control = "\u007f",     Alt = Esc + "8" } },
                { Keys.D9,      new StandardMapping {                         Alt = Esc + "9" } },

                //
                // Numpad keys.
                //
                // NB. We can't distinguish the numpad-return from the regular return key as
                // both map to the same key codes.
                //
                { Keys.NumPad0,  new StandardMapping { Control = "0",                             Alt = Esc + "0" } },
                { Keys.NumPad1,  new StandardMapping { Control = "1",                             Alt = Esc + "1" } },
                { Keys.NumPad2,  new StandardMapping { Control = "2",                             Alt = Esc + "2" } },
                { Keys.NumPad3,  new StandardMapping { Control = "3",                             Alt = Esc + "3" } },
                { Keys.NumPad4,  new StandardMapping { Control = "4",                             Alt = Esc + "4" } },
                { Keys.NumPad5,  new StandardMapping { Control = "5",                             Alt = Esc + "5" } },
                { Keys.NumPad6,  new StandardMapping { Control = "6",                             Alt = Esc + "6" } },
                { Keys.NumPad7,  new StandardMapping { Control = "7",                             Alt = Esc + "7" } },
                { Keys.NumPad8,  new StandardMapping { Control = "8",                             Alt = Esc + "8" } },
                { Keys.NumPad9,  new StandardMapping { Control = "9",                             Alt = Esc + "9" } },

                { Keys.Divide,   new StandardMapping { Control = "/",         Shift = "/",        Alt = "/" } },
                { Keys.Multiply, new StandardMapping { Control = "*",         Shift = "*",        Alt = "*" } },
                { Keys.Subtract, new StandardMapping { Control = "-",         Shift = "",         Alt = "-" } },
                { Keys.Add,      new StandardMapping { Control = "+",         Shift = "",         Alt = "+" } },
                { Keys.Decimal,  new StandardMapping { Control = DecimalSep,  Shift = DecimalSep, Alt = Esc + DecimalSep } },
            };

        /// <summary>
        /// Extra translations to apply in application cursor keys mode.
        /// </summary>
        private static readonly Dictionary<Keys, StandardMapping> ApplicationCursorKeysModeTranslations
            = new Dictionary<Keys, StandardMapping>
            {
                //
                // NB. To manually enter this mode, run
                //     echo -e "\e[?1h"
                // To disable:
                //     echo -e "\e[?1l"
                //
                { Keys.Up,       new StandardMapping { Normal = Ss3 + "A" } },
                { Keys.Down,     new StandardMapping { Normal = Ss3 + "B" } },
                { Keys.Right,    new StandardMapping { Normal = Ss3 + "C" } },
                { Keys.Left,     new StandardMapping { Normal = Ss3 + "D" } },
                { Keys.Home,     new StandardMapping { Normal = Ss3 + "H" } },
                { Keys.End,      new StandardMapping { Normal = Ss3 + "F" } },
            };

        /// <summary>
        /// Extra translations to apply when modifyOtherKeys = 1.
        /// 
        /// See https://invisible-island.net/xterm/manpage/xterm.html#VT100-Widget-Resources:modifyOtherKeys
        /// </summary>
        private static readonly Dictionary<Keys, StandardMapping> ModifyOtherKeysModeWithExceptionTranslations
            = new Dictionary<Keys, StandardMapping>
            {
                //
                // NB. To manually enter this mode, run
                //     printf '\033[>4;1m'
                // To disable:
                //     printf '\033[>4;2m'
                //
                { Keys.Tab,      new StandardMapping {                           Control = Esc + "[27;5;9"  } },
                { Keys.Return,   new StandardMapping { Shift = Esc + "[27;2;13", Control = Esc + "[27;5;13" ,    Alt = Esc + "[27;3;13" } },

                { Keys.D0,       new StandardMapping {                           Control = Esc + "[27;5;48"  } },
                { Keys.D1,       new StandardMapping {                           Control = Esc + "[27;5;49"  } },
                { Keys.D9,       new StandardMapping {                           Control = Esc + "[27;5;57"  } },
            };

        public static string ForKey(
            Keys keyCode,
            bool alt,
            bool control,
            bool shift,
            bool applicationCursorKeysMode,
            ModifyOtherKeysMode modifyOtherKeysMode)
        {
            Debug.Assert((keyCode & Keys.KeyCode) == keyCode, "No modifiers");

            if (alt && control)
            {
                //
                // AltGr - that's probably a composition.
                //
                return null;
            }

            if (modifyOtherKeysMode == ModifyOtherKeysMode.EnabledWithExceptions &&
                ModifyOtherKeysModeWithExceptionTranslations.TryGetValue(keyCode, out var modMapping) &&
                modMapping.Apply(alt, control, shift) is string modSequence)
            {
                return modSequence;
            }

            if (applicationCursorKeysMode &&
                ApplicationCursorKeysModeTranslations.TryGetValue(keyCode, out var appMapping) &&
                appMapping.Apply(alt, control, shift) is string appSequence)
            {
                return appSequence;
            }

            if (StandardTranslations.TryGetValue(keyCode, out var stdMapping) &&
                stdMapping.Apply(alt, control, shift) is string stdSequence)
            {
                return stdSequence;
            }

            return null;
        }
    }
}
