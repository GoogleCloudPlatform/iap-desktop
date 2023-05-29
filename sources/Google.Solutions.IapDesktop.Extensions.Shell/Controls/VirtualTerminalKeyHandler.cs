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

using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using VtNetCore.VirtualTerminal;

namespace Google.Solutions.IapDesktop.Extensions.Session.Controls
{
    /// <summary>
    /// Key handler, replacement for vtnetcore's own key handling
    /// logic in VirtualTerminalController.
    /// </summary>
    internal class VirtualTerminalKeyHandler
    {
        private readonly VirtualTerminalController controller;

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

            //
            // Instead of passing the key to vtnetcore to translate, use
            // our own translation logic (which is more complete than
            // vtnetcore's).
            //
            var translation = VirtualTerminalKeyTranslation.ForKey(
                keyCode,
                alt,
                control,
                shift,
                this.controller.CursorState.ApplicationCursorKeysMode,
                this.controller.ModifyOtherKeys);

            if (translation != null)
            {
                //
                // This key translated into some escape sequence. Send
                // translated sequence and stop processing the key.
                //
                Send(translation);
                return true;
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
