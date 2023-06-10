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
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

#pragma warning disable CA1806 // return code ignored

namespace Google.Solutions.IapDesktop.Extensions.Session.Controls
{
    internal static class KeyUtil
    {
        public static string CharFromKeyCode(Keys key)
        {
            var keyboardState = new byte[255];
            if (!UnsafeNativeMethods.GetKeyboardState(keyboardState))
            {
                return "";
            }

            var virtualKeyCode = (uint)key;
            var scanCode = UnsafeNativeMethods.MapVirtualKey(virtualKeyCode, 0);
            var inputLocaleIdentifier = UnsafeNativeMethods.GetKeyboardLayout(0);

            var result = new StringBuilder(10);
            UnsafeNativeMethods.ToUnicodeEx(
                virtualKeyCode,
                scanCode,
                keyboardState,
                result,
                (int)result.Capacity,
                0,
                inputLocaleIdentifier);

            return result.ToString();
        }

        //---------------------------------------------------------------------
        // P/Invoke definitions.
        //---------------------------------------------------------------------

        private static class UnsafeNativeMethods
        {
            //---------------------------------------------------------------------
            // Caret.
            //---------------------------------------------------------------------

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;

                public POINT(int x, int y)
                {
                    this.X = x;
                    this.Y = y;
                }

                public static implicit operator System.Drawing.Point(POINT p)
                {
                    return new System.Drawing.Point(p.X, p.Y);
                }

                public static implicit operator POINT(System.Drawing.Point p)
                {
                    return new POINT(p.X, p.Y);
                }
            }

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
