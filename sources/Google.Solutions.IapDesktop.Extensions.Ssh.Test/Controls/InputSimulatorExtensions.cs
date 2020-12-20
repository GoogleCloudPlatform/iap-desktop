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

using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Test.Controls
{
    internal static class InputSimulatorExtensions
    {
        public static void SendKeySequence(
            this IWin32Window window,
            params UnsafeNativeMethods.VirtualKeyShort[] keys)
        {
            UnsafeNativeMethods.SetForegroundWindow(window.Handle);

            var sequence = new UnsafeNativeMethods.INPUT[keys.Length * 2];
            for (int i = 0; i < keys.Length; i++)
            {
                sequence[i] = new UnsafeNativeMethods.INPUT();
                sequence[i].type = 1; // Keyboard.
                sequence[i].U.ki.wVk = keys[i];
                sequence[i].U.ki.dwFlags = 0;

                sequence[sequence.Length - 1 - i] = new UnsafeNativeMethods.INPUT();
                sequence[sequence.Length - 1 - i].type = 1; // Keyboard.
                sequence[sequence.Length - 1 - i].U.ki.wVk = keys[i];
                sequence[sequence.Length - 1 - i].U.ki.dwFlags = UnsafeNativeMethods.KEYEVENTF.KEYUP;
            }

            UnsafeNativeMethods.SendInput(
                (uint)sequence.Length,
                sequence,
                UnsafeNativeMethods.INPUT.Size);
        }

        public static void SendControlC(this IWin32Window window)
        {
            SendKeySequence(
                window,
                new[]
                {
                    UnsafeNativeMethods.VirtualKeyShort.CONTROL,
                    UnsafeNativeMethods.VirtualKeyShort.KEY_C
                });
        }

        public static void SendControlD(this IWin32Window window)
        {
            SendKeySequence(
                window,
                new[]
                {
                    UnsafeNativeMethods.VirtualKeyShort.CONTROL,
                    UnsafeNativeMethods.VirtualKeyShort.KEY_D
                });
        }
    }
}
