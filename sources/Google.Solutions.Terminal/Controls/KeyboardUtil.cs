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

using Google.Solutions.Mvvm.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    internal static class KeyboardUtil
    {
        public static IEnumerable<Message> ToMessageSequence(
            IntPtr hwnd,
            Keys key)
        {
            var keyboardState = new byte[255];
            if (!NativeMethods.GetKeyboardState(keyboardState))
            {
                throw new InvalidOperationException();
            }

            var virtualKeyCode = (uint)key;
            var scanCode = NativeMethods.MapVirtualKey(virtualKeyCode, 0);

            yield return new Message()
            {
                HWnd = hwnd,
                Msg = (int)WindowMessage.WM_KEYDOWN,
                LParam = new IntPtr((scanCode & 0xFF) << 16),
                WParam = new IntPtr(virtualKeyCode),
            };


            yield return new Message()
            {
                HWnd = hwnd,
                Msg = (int)WindowMessage.WM_CHAR,
                LParam = new IntPtr((scanCode & 0xFF) << 16),
                WParam = new IntPtr(virtualKeyCode),
            };


            yield return new Message()
            {
                HWnd = hwnd,
                Msg = (int)WindowMessage.WM_KEYUP,
                LParam = new IntPtr((scanCode & 0xFF) << 16),
                WParam = new IntPtr(virtualKeyCode),
            };
        }

        //---------------------------------------------------------------------
        // P/Invoke definitions.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            internal static extern bool GetKeyboardState(byte[] lpKeyState);

            [DllImport("user32.dll")]
            internal static extern uint MapVirtualKey(uint uCode, uint uMapType);
        }
    }
}
