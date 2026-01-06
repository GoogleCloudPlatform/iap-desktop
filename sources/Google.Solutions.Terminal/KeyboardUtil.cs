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
using System.Text;
using System.Windows.Forms;

namespace Google.Solutions.Terminal
{
    internal static class KeyboardUtil
    {
        /// <summary>
        /// Generate a WM_KEYDOWN/WM_CHAR/WM_KEYUp message sequence.
        /// </summary>
        /// <remarks>Uses the current keyboard state.</remarks>
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

            var hkl = NativeMethods.GetKeyboardLayout(0);
            var buffer = new StringBuilder(2);
            if (NativeMethods.ToUnicodeEx(
                virtualKeyCode,
                scanCode,
                keyboardState,
                buffer,
                buffer.Capacity,
                0,
                hkl) > 0)
            {
                //
                // NB. ToUnicodeEx can produce multiple characters for dead keys.
                //     We're ignoring that for simplicity as this is for testing.
                //
                yield return new Message()
                {
                    HWnd = hwnd,
                    Msg = (int)WindowMessage.WM_CHAR,
                    LParam = new IntPtr((scanCode & 0xFF) << 16),
                    WParam = new IntPtr(buffer[0]),
                };
            }

            yield return new Message()
            {
                HWnd = hwnd,
                Msg = (int)WindowMessage.WM_KEYUP,
                LParam = new IntPtr((scanCode & 0xFF) << 16),
                WParam = new IntPtr(virtualKeyCode),
            };
        }

        /// <summary>
        /// Map a virtual key to a char.
        /// </summary>
        /// <remarks>Uses the current keyboard state and layout.</remarks>
        public static string CharFromKeyCode(Keys key)
        {
            var keyboardState = new byte[255];
            if (!NativeMethods.GetKeyboardState(keyboardState))
            {
                return "";
            }

            var virtualKeyCode = (uint)key;
            var scanCode = NativeMethods.MapVirtualKey(virtualKeyCode, 0);
            var inputLocaleIdentifier = NativeMethods.GetKeyboardLayout(0);

            var result = new StringBuilder(10);
            _ = NativeMethods.ToUnicodeEx(
                virtualKeyCode,
                scanCode,
                keyboardState,
                result,
                result.Capacity,
                0,
                inputLocaleIdentifier);

            return result.ToString();
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

            [DllImport("user32.dll")]
            internal static extern IntPtr GetKeyboardLayout(uint idThread);

            [DllImport("user32.dll")]
            internal static extern int ToUnicodeEx(
                uint wVirtKey,
                uint wScanCode,
                byte[] lpKeyState,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
                int cchBuff,
                uint wFlags,
                IntPtr dwhkl);
        }
    }
}
