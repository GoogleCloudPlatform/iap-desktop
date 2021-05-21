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

        public bool KeyPressed(Keys keyCode, bool control, bool shift)
        {
            return this.controller.KeyPressed(
                    NameFromKey(keyCode),
                    control,
                    shift);
        }

        public bool KeyPressed(char keyChar, bool control, bool shift)
        {
            return this.controller.KeyPressed(
                    keyChar.ToString(),
                    control,
                    shift);
        }
    }
}
