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

using MSTSCLib;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services.Windows.RemoteDesktop
{
    internal static class RdpClientExtensions
    {
        internal unsafe static void SendKeys(
            this IMsRdpClientNonScriptable5 nonScriptable,
            params Keys[] keyCodes)
        {
            if (keyCodes.Length > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(keyCodes));
            }

            short[] keyUp = new short[keyCodes.Length * 2];
            int[] keyData = new int[keyCodes.Length * 2];

            for (int i = 0; i < keyCodes.Length; i++)
            {
                var virtualKeyCode = (int)UnsafeNativeMethods.MapVirtualKey((uint)keyCodes[i], 0);

                // Generate DOWN key presses.
                keyUp[i] = 0;
                keyData[i] = virtualKeyCode;

                // Generate UP key presses (in reverse order).
                keyUp[keyUp.Length - 1 - i] = 1;
                keyData[keyData.Length - 1 - i] = virtualKeyCode;
            }

            fixed (short* keyUpPtr = keyUp)
            fixed (int* keyDataPtr = keyData)
            {
                nonScriptable.SendKeysUnsafe(keyData.Length, (bool*)keyUpPtr, keyDataPtr);
            }
        }

        private unsafe static void SendKeysUnsafe(
            this IMsRdpClientNonScriptable5 nonScriptable,
            int keyDataLength,
            bool* keyUpPtr,
            int* keyDataPtr
            )
        {
            // There is something about wrapping this key in a special method.
            // Without the wrapper method, marshaling does not work properly.
            nonScriptable.SendKeys(keyDataLength, ref *keyUpPtr, ref *keyDataPtr);
        }
    }
}
