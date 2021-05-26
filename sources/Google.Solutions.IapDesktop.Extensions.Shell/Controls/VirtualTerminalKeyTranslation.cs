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

        /// <summary>
        /// Standard Xterm key translations.
        /// </summary>
        private static readonly Dictionary<Keys, StandardMapping> StandardTranslations =
            new Dictionary<Keys, StandardMapping>
            {
                //
                // Function keys.
                //
                { Keys.F1,       new StandardMapping { Normal = "\u001b[11~", Shift = "\u001b[23~", Control = "\u001b[11~", Alt = "\u001b\u001b[11~" } },
                { Keys.F2,       new StandardMapping { Normal = "\u001b[12~", Shift = "\u001b[24~", Control = "\u001b[12~", Alt = "\u001b\u001b[12~" } },
                { Keys.F3,       new StandardMapping { Normal = "\u001b[13~", Shift = "\u001b[25~", Control = "\u001b[13~", Alt = "\u001b\u001b[13~" } },
                { Keys.F4,       new StandardMapping { Normal = "\u001b[14~", Shift = "\u001b[26~", Control = "\u001b[14~", Alt = "\u001b\u001b[14~" } },
                { Keys.F5,       new StandardMapping { Normal = "\u001b[15~", Shift = "\u001b[28~", Control = "\u001b[15~", Alt = "\u001b\u001b[15~" } },
                { Keys.F6,       new StandardMapping { Normal = "\u001b[17~", Shift = "\u001b[29~", Control = "\u001b[17~", Alt = "\u001b\u001b[17~" } },
                { Keys.F7,       new StandardMapping { Normal = "\u001b[18~", Shift = "\u001b[31~", Control = "\u001b[18~", Alt = "\u001b\u001b[18~" } },
                { Keys.F8,       new StandardMapping { Normal = "\u001b[19~", Shift = "\u001b[32~", Control = "\u001b[19~", Alt = "\u001b\u001b[19~" } },
                { Keys.F9,       new StandardMapping { Normal = "\u001b[20~", Shift = "\u001b[33~", Control = "\u001b[20~", Alt = "\u001b\u001b[20~" } },
                { Keys.F10,      new StandardMapping { Normal = "\u001b[21~", Shift = "\u001b[24~", Control = "\u001b[21~", Alt = "\u001b\u001b[21~" } },
                { Keys.F11,      new StandardMapping { Normal = "\u001b[23~", Shift = "\u001b[23~", Control = "\u001b[23~", Alt = "\u001b\u001b[23~" } },
                { Keys.F12,      new StandardMapping { Normal = "\u001b[24~", Shift = "\u001b[24~", Control = "\u001b[24~", Alt = "\u001b\u001b[24~" } },

                //
                // Arrow keys.
                //
                { Keys.Up,       new StandardMapping { Normal = "\u001b[A",   Shift = "\u001bOA", Control = "\u001bOA",     Alt = "\u001b\u001b[A" } },
                { Keys.Down,     new StandardMapping { Normal = "\u001b[B",   Shift = "\u001bOB", Control = "\u001bOB",     Alt = "\u001b\u001b[B" } },
                { Keys.Right,    new StandardMapping { Normal = "\u001b[C",   Shift = "\u001bOC", Control = "\u001bOC",     Alt = "\u001b\u001b[C" } },
                { Keys.Left,     new StandardMapping { Normal = "\u001b[D",   Shift = "\u001bOD", Control = "\u001bOD",     Alt = "\u001b\u001b[D" } },
                { Keys.Home,     new StandardMapping { Normal = "\u001b[1~",  Shift = "\u001b[1~",                          Alt = "\u001b\u001b[1~" } },
                { Keys.Insert,   new StandardMapping { Normal = "\u001b[2~",                                                Alt = "\u001b\u001b[2~" } },
                { Keys.Delete,   new StandardMapping { Normal = "\u001b[3~",  Shift = "\u001b[3~",                          Alt = "\u001b\u001b[3~" } },
                { Keys.End,      new StandardMapping { Normal = "\u001b[4~",  Shift = "\u001b[4~",                          Alt = "\u001b\u001b[4~" } },
                { Keys.PageUp,   new StandardMapping { Normal = "\u001b[5~",  Shift = "\u001b[5~",                          Alt = "\u001b\u001b[5~" } },
                { Keys.PageDown, new StandardMapping { Normal = "\u001b[6~",  Shift = "\u001b[6~",                          Alt = "\u001b\u001b[6~" } },

                //
                // Main keyboard.
                //
                { Keys.Back,    new StandardMapping { Normal = "\u007F",      Shift = "\b",           Control = "\u007F" } },
                { Keys.Tab,     new StandardMapping { Normal = "\t",          Shift = "\u001b[Z",     } },
                { Keys.Return,  new StandardMapping { Normal = "\r",          Shift = "\r",           Control = "\r",       Alt = "\u001b\r" } },
                { Keys.Escape,  new StandardMapping { Normal = "\u001b\u001b",Shift = "\u001b\u001b", Control = "\u001b\u001b" } },
                { Keys.Space,   new StandardMapping { Control = "\u0000",                                                   Alt = "\u001b " } },
                { Keys.A,       new StandardMapping { Control = "\u0001",     Alt = "\u001ba" } },
                { Keys.B,       new StandardMapping { Control = "\u0002",     Alt = "\u001bb" } },
                { Keys.C,       new StandardMapping { Control = "\u0003",     Alt = "\u001bc" } },
                { Keys.D,       new StandardMapping { Control = "\u0004",     Alt = "\u001bd" } },
                { Keys.E,       new StandardMapping { Control = "\u0005",     Alt = "\u001be" } },
                { Keys.F,       new StandardMapping { Control = "\u0006",     Alt = "\u001bf" } },
                { Keys.G,       new StandardMapping { Control = "\u0007",     Alt = "\u001bg" } },
                { Keys.H,       new StandardMapping { Control = "\u0008",     Alt = "\u001bh" } },
                { Keys.I,       new StandardMapping { Control = "\u0009",     Alt = "\u001bi" } },
                { Keys.J,       new StandardMapping { Control = "\u000a",     Alt = "\u001bj" } },
                { Keys.K,       new StandardMapping { Control = "\u000b",     Alt = "\u001bk" } },
                { Keys.L,       new StandardMapping { Control = "\u000c",     Alt = "\u001bl" } },
                { Keys.M,       new StandardMapping { Control = "\u000d",     Alt = "\u001bm" } },
                { Keys.N,       new StandardMapping { Control = "\u000e",     Alt = "\u001bn" } },
                { Keys.O,       new StandardMapping { Control = "\u000f",     Alt = "\u001bo" } },
                { Keys.P,       new StandardMapping { Control = "\u0010",     Alt = "\u001bp" } },
                { Keys.Q,       new StandardMapping { Control = "\u0011",     Alt = "\u001bq" } },
                { Keys.R,       new StandardMapping { Control = "\u0012",     Alt = "\u001br" } },
                { Keys.S,       new StandardMapping { Control = "\u0013",     Alt = "\u001bs" } },
                { Keys.T,       new StandardMapping { Control = "\u0014",     Alt = "\u001bt" } },
                { Keys.U,       new StandardMapping { Control = "\u0015",     Alt = "\u001bu" } },
                { Keys.V,       new StandardMapping { Control = "\u0016",     Alt = "\u001bv" } },
                { Keys.W,       new StandardMapping { Control = "\u0017",     Alt = "\u001bw" } },
                { Keys.X,       new StandardMapping { Control = "\u0018",     Alt = "\u001bx" } },
                { Keys.Y,       new StandardMapping { Control = "\u0019",     Alt = "\u001by" } },
                { Keys.Z,       new StandardMapping { Control = "\u001a",     Alt = "\u001bz" } },
            };

        /// <summary>
        /// Extra translations to apply in application cursor keys mode.
        /// </summary>
        private static readonly Dictionary<Keys, StandardMapping> ApplicationCursorKeysModeTranslations
            = new Dictionary<Keys, StandardMapping>
            {
                { Keys.Up,       new StandardMapping { Normal = "\u001bOA" } },
                { Keys.Down,     new StandardMapping { Normal = "\u001bOB" } },
                { Keys.Right,    new StandardMapping { Normal = "\u001bOC" } },
                { Keys.Left,     new StandardMapping { Normal = "\u001bOD" } },
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
