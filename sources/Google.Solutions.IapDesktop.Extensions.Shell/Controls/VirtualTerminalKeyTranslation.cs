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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VtNetCore.VirtualTerminal;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Controls
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

        private static string Esc = "\u001b";
        private static string Ss3 = Esc + "[O";

        /// <summary>
        /// Standard Xterm key translations.
        /// </summary>
        private static readonly Dictionary<Keys, StandardMapping> StandardTranslations =
            new Dictionary<Keys, StandardMapping>
            {
                //
                // Function keys.
                //
                { Keys.F1,       new StandardMapping { Normal = Esc + "[11~", Shift = Esc + "[23~", Control = Esc + "[11~", Alt = Esc + Esc + "[11~" } },
                { Keys.F2,       new StandardMapping { Normal = Esc + "[12~", Shift = Esc + "[24~", Control = Esc + "[12~", Alt = Esc + Esc + "[12~" } },
                { Keys.F3,       new StandardMapping { Normal = Esc + "[13~", Shift = Esc + "[25~", Control = Esc + "[13~", Alt = Esc + Esc + "[13~" } },
                { Keys.F4,       new StandardMapping { Normal = Esc + "[14~", Shift = Esc + "[26~", Control = Esc + "[14~", Alt = Esc + Esc + "[14~" } },
                { Keys.F5,       new StandardMapping { Normal = Esc + "[15~", Shift = Esc + "[28~", Control = Esc + "[15~", Alt = Esc + Esc + "[15~" } },
                { Keys.F6,       new StandardMapping { Normal = Esc + "[17~", Shift = Esc + "[29~", Control = Esc + "[17~", Alt = Esc + Esc + "[17~" } },
                { Keys.F7,       new StandardMapping { Normal = Esc + "[18~", Shift = Esc + "[31~", Control = Esc + "[18~", Alt = Esc + Esc + "[18~" } },
                { Keys.F8,       new StandardMapping { Normal = Esc + "[19~", Shift = Esc + "[32~", Control = Esc + "[19~", Alt = Esc + Esc + "[19~" } },
                { Keys.F9,       new StandardMapping { Normal = Esc + "[20~", Shift = Esc + "[33~", Control = Esc + "[20~", Alt = Esc + Esc + "[20~" } },
                { Keys.F10,      new StandardMapping { Normal = Esc + "[21~", Shift = Esc + "[24~", Control = Esc + "[21~", Alt = Esc + Esc + "[21~" } },
                { Keys.F11,      new StandardMapping { Normal = Esc + "[23~", Shift = Esc + "[23~", Control = Esc + "[23~", Alt = Esc + Esc + "[23~" } },
                { Keys.F12,      new StandardMapping { Normal = Esc + "[24~", Shift = Esc + "[24~", Control = Esc + "[24~", Alt = Esc + Esc + "[24~" } },

                //
                // Arrow keys.
                //
                { Keys.Up,       new StandardMapping { Normal = Esc + "[A",   Shift = Esc + "OA", Control = Esc + "OA",     Alt = Esc + Esc + "[A" } },
                { Keys.Down,     new StandardMapping { Normal = Esc + "[B",   Shift = Esc + "OB", Control = Esc + "OB",     Alt = Esc + Esc + "[B" } },
                { Keys.Right,    new StandardMapping { Normal = Esc + "[C",   Shift = Esc + "OC", Control = Esc + "OC",     Alt = Esc + Esc + "[C" } },
                { Keys.Left,     new StandardMapping { Normal = Esc + "[D",   Shift = Esc + "OD", Control = Esc + "OD",     Alt = Esc + Esc + "[D" } },
                { Keys.Home,     new StandardMapping { Normal = Esc + "[1~",  Shift = Esc + "[1~",                          Alt = Esc + Esc + "[1~" } },
                { Keys.Insert,   new StandardMapping { Normal = Esc + "[2~",                                                Alt = Esc + Esc + "[2~" } },
                { Keys.Delete,   new StandardMapping { Normal = Esc + "[3~",  Shift = Esc + "[3~",                          Alt = Esc + Esc + "[3~" } },
                { Keys.End,      new StandardMapping { Normal = Esc + "[4~",  Shift = Esc + "[4~",                          Alt = Esc + Esc + "[4~" } },
                { Keys.PageUp,   new StandardMapping { Normal = Esc + "[5~",  Shift = Esc + "[5~",                          Alt = Esc + Esc + "[5~" } },
                { Keys.PageDown, new StandardMapping { Normal = Esc + "[6~",  Shift = Esc + "[6~",                          Alt = Esc + Esc + "[6~" } },

                //
                // Main keyboard.
                //
                { Keys.Back,    new StandardMapping { Normal = "\u007F",      Shift = "\b",           Control = "\u007F" } },
                { Keys.Tab,     new StandardMapping { Normal = "\t",          Shift = Esc + "[Z",     } },
                { Keys.Return,  new StandardMapping { Normal = "\r",          Shift = "\r",           Control = "\r",       Alt = Esc + "\r" } },
                { Keys.Escape,  new StandardMapping { Normal = Esc + Esc,     Shift = Esc + Esc,      Control = Esc + Esc } },
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

                { Keys.NumPad0,  new StandardMapping { Normal = Ss3 + "p",  Shift = Ss3 + "p",    Control = Ss3 + "p",    Alt = Ss3 + "p" } },
                { Keys.NumPad1,  new StandardMapping { Normal = Ss3 + "q",  Shift = Ss3 + "q",    Control = Ss3 + "q",    Alt = Ss3 + "q" } },
                { Keys.NumPad2,  new StandardMapping { Normal = Ss3 + "r",  Shift = Ss3 + "r",    Control = Ss3 + "r",    Alt = Ss3 + "r" } },
                { Keys.NumPad3,  new StandardMapping { Normal = Ss3 + "s",  Shift = Ss3 + "s",    Control = Ss3 + "s",    Alt = Ss3 + "s" } },
                { Keys.NumPad4,  new StandardMapping { Normal = Ss3 + "t",  Shift = Ss3 + "t",    Control = Ss3 + "t",    Alt = Ss3 + "t" } },
                { Keys.NumPad5,  new StandardMapping { Normal = Ss3 + "u",  Shift = Ss3 + "u",    Control = Ss3 + "u",    Alt = Ss3 + "u" } },
                { Keys.NumPad6,  new StandardMapping { Normal = Ss3 + "v",  Shift = Ss3 + "v",    Control = Ss3 + "v",    Alt = Ss3 + "v" } },
                { Keys.NumPad7,  new StandardMapping { Normal = Ss3 + "w",  Shift = Ss3 + "w",    Control = Ss3 + "w",    Alt = Ss3 + "w" } },
                { Keys.NumPad8,  new StandardMapping { Normal = Ss3 + "x",  Shift = Ss3 + "x",    Control = Ss3 + "x",    Alt = Ss3 + "x" } },
                { Keys.NumPad9,  new StandardMapping { Normal = Ss3 + "y",  Shift = Ss3 + "y",    Control = Ss3 + "y",    Alt = Ss3 + "y" } },
            };

        public static string ForKey(
            Keys keyCode,
            bool alt,
            bool control,
            bool shift,
            bool applicationCursorKeysMode)
        {
            Debug.Assert((keyCode & Keys.KeyCode) == keyCode, "No modifiers");

            if (alt && control)
            {
                //
                // AltGr - that's probably a composition.
                //
                return null;
            }
            if (applicationCursorKeysMode &&
                ApplicationCursorKeysModeTranslations.TryGetValue(keyCode, out var appMapping) &&
                appMapping.Apply(alt, control, shift) is string appSequence)
            {
                return appSequence;
            }
            else if (
                StandardTranslations.TryGetValue(keyCode, out var stdMapping) &&
                stdMapping.Apply(alt, control, shift) is string stdSequence)
            {
                return stdSequence;
            }
            else
            {
                return null;
            }
        }
    }
}
