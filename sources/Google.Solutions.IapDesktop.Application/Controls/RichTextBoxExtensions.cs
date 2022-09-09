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
using System.Windows.Forms;

#pragma warning disable CA1806 // Do not ignore method results

namespace Google.Solutions.IapDesktop.Application.Controls
{
    public static class RichTextBoxExtensions
    {
        public static void SetPadding(this RichTextBox textBox, int padding)
        {
            var rect = new UnsafeNativeMethods.RECT();
            UnsafeNativeMethods.SendMessageRect(
                textBox.Handle,
                UnsafeNativeMethods.EM_GETRECT,
                0,
                ref rect);

            var newRect = new UnsafeNativeMethods.RECT(
                padding,
                padding,
                rect.Right - padding * 2,
                rect.Bottom - padding * 2);

            UnsafeNativeMethods.SendMessageRect(
                textBox.Handle,
                UnsafeNativeMethods.EM_SETRECT,
                0,
                ref newRect);
        }

        //---------------------------------------------------------------------
        // P/Invoke definitions.
        //---------------------------------------------------------------------

        private static class UnsafeNativeMethods
        {
            internal const int EM_GETRECT = 0xB2;
            internal const int EM_SETRECT = 0xB3;

            [StructLayout(LayoutKind.Sequential)]
            internal struct RECT
            {
                public readonly int Left;
                public readonly int Top;
                public readonly int Right;
                public readonly int Bottom;

                internal RECT(int left, int top, int right, int bottom)
                {
                    this.Left = left;
                    this.Top = top;
                    this.Right = right;
                    this.Bottom = bottom;
                }
            }

            [DllImport("user32.dll", EntryPoint = @"SendMessage", CharSet = CharSet.Auto)]
            internal static extern int SendMessageRect(IntPtr hWnd, uint msg, int wParam, ref RECT rect);
        }
    }
}
