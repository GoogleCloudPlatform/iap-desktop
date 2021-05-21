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
    /// <summary>
    /// Key handler, replacement for vtnetcore's own key handling
    /// logic in VirtualTerminalController.
    /// </summary>
    internal class VirtualTerminalKeyHandler
    {
        private readonly VirtualTerminalController controller;

        private static string NameFromKey(Keys key)
        {
            // Return name that is compatible with vtnetcore's KeyboardTranslation
            switch (key)
            {
                case Keys.Next: // Alias for PageDown
                    return "PageDown";

                case Keys.Prior:   // Alias for PageUp
                    return "PageUp";

                default:
                    return key.ToString();
            }
        }

        private void Send(string data)
        {
            this.controller.SendData?.Invoke(
                this.controller,
                new VtNetCore.VirtualTerminal.SendDataEventArgs()
                {
                    Data = Encoding.UTF8.GetBytes(data)
                });
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public VirtualTerminalKeyHandler(VirtualTerminalController controller)
        {
            this.controller = controller;
        }

        //---------------------------------------------------------------------
        //
        //---------------------------------------------------------------------

        public bool IsKeySequence(Keys keyCode, bool control, bool shift)
        {
            return this.controller.GetKeySequence(
                NameFromKey(keyCode), 
                control, 
                shift) != null;
        }

        public bool KeyDown(Keys keyData)
        {
            return KeyDown(
                keyData & Keys.KeyCode,
                keyData.HasFlag(Keys.Alt),
                keyData.HasFlag(Keys.Control),
                keyData.HasFlag(Keys.Shift));
        }

        public bool KeyDown(Keys keyCode, bool alt, bool control, bool shift)
        {
            Debug.Assert((keyCode & Keys.KeyCode) == keyCode, "No modifiers");

            if (!alt && IsKeySequence(keyCode, control, shift))
            {
                //
                // This is a key sequence that needs to be
                // translated to some VT sequence.
                //
                // NB. If Alt is pressed, it cannot be a key sequence. 
                // Otherwise, it might.
                //
                return this.controller.KeyPressed(
                    NameFromKey(keyCode),
                    control,
                    shift);
            }
            else if (alt && control)
            {
                //
                // AltGr - let KeyPress handle the composition.
                //
                return false;
            }
            else if (alt)
            {
                //
                // Somewhat non-standard, emulate the behavior
                // of other terminals and escape the character.
                //
                // This enables applications like midnight 
                // commander which rely on Alt+<char> keyboard
                // shortcuts.
                //
                var ch = KeyUtil.CharFromKeyCode(keyCode);
                if (ch.Length > 0)
                {
                    Send("\u001b" + ch);
                    return true;
                }
                else
                {
                    //
                    // This is a stray Alt press, could be part
                    // of an Alt+Tab action. Do not handle this
                    // as it might screw up subsequent input.
                    //
                    return false;
                }
            }
            else
            {
                //
                // This is a plain character. Defer handling to 
                // KeyPress so that Windows does the nasty key
                // composition and dead key handling for us.
                //
                return false;
            }
        }

        public bool KeyPressed(char keyChar)
        {
            return this.controller.KeyPressed(
                    keyChar.ToString(),
                    false,
                    false);
        }
    }
}
