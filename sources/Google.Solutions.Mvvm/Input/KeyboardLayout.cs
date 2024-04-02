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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Input
{
    public class KeyboardLayout
    {
        private readonly IntPtr hkl;

        private KeyboardLayout(IntPtr hkl)
        {
            this.hkl = hkl;
        }

        public static KeyboardLayout Current
        {
            get => new KeyboardLayout(NativeMethods.GetKeyboardLayout(0));
        }

        /// <summary>
        /// Convert a string into a sequence of virtual key chords.
        /// </summary>
        /// <returns>Virtual keys, with modifiers</returns>
        public IEnumerable<Keys> ToVirtualKeys(string text)
        {
            foreach (var ch in text)
            {
                var result = NativeMethods.VkKeyScanEx(ch, this.hkl);

                if (result == ushort.MaxValue)
                {
                    throw new FormatException(
                        $"The character '{ch}' cannot be mapped to a virtual key " +
                        $"using the current keyboard layout");
                }

                //
                // The low-order byte of the return value contains the virtual-key code.
                //
                var vk = (Keys)(result & 0xFF);

                //
                // The high-order byte contains the shift state.
                //
                var shiftState = (ShiftState)((result & 0xFF00) >> 8);

                //
                // Combine the two.
                //
                vk |= shiftState.HasFlag(ShiftState.Shift) ? Keys.Shift : Keys.None;
                vk |= shiftState.HasFlag(ShiftState.Control) ? Keys.Control : Keys.None;
                vk |= shiftState.HasFlag(ShiftState.Alt) ? Keys.Alt : Keys.None;

                yield return vk;
            }
        }

        /// <summary>
        /// Translate modifiers into corresponding virtual keys.
        /// </summary>
        public static IEnumerable<Keys> TranslateModifiers(Keys chord) 
        {
            if (chord.HasFlag(Keys.Shift))
            {
                yield return Keys.ShiftKey;
            }

            if (chord.HasFlag(Keys.Control))
            {
                yield return Keys.ControlKey;
            }

            if (chord.HasFlag(Keys.Alt))
            {
                yield return Keys.Menu;

            }

            yield return chord & ~(Keys.Shift | Keys.Control | Keys.Alt);
        }

        /// <summary>
        /// Convert a virtual key chords into a sequence of scan codes.
        /// </summary>
        public IEnumerable<uint> ToScanCodes(Keys chord)
        {
            foreach (var vk in TranslateModifiers(chord))
            {
                Debug.Assert((((int)vk) & 0xFF00) >> 8 == 0, "No modifiers");

                yield return NativeMethods.MapVirtualKey((uint)vk, MAPVK.VK_TO_VSC);
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke definitions.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            public const uint E_UNEXPECTED = 0x8000ffff;

            [DllImport("user32.dll")]
            internal static extern uint MapVirtualKey(
                [In] uint uCode,
                [In] MAPVK uMapType);

            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            internal static extern ushort VkKeyScanEx(
                [In] char ch,
                [In] IntPtr hKeyboardLayout);

            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            internal static extern IntPtr GetKeyboardLayout(
                [In] uint threadId);
        }

        private enum MAPVK : uint
        {
            VK_TO_VSC = 0,
            VSC_TO_VK = 1,
            VK_TO_CHAR = 2,
            VSC_TO_VK_EX = 3,
            VK_TO_VSC_EX = 4,
        }

        /// <summary>
        /// Shift state as returned by VkKeyScanEx.
        /// </summary>
        [Flags]
        private enum ShiftState : byte
        {
            Shift = 1,
            Control = 2,
            Alt = 4,
            Hankaku = 8,
        }
    }
}
