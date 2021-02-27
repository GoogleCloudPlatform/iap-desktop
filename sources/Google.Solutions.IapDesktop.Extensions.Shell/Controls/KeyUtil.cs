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
using System.Text;
using System.Windows.Forms;

#pragma warning disable CA1806 // return code ignored

namespace Google.Solutions.IapDesktop.Extensions.Shell.Controls
{
    internal static class KeyUtil
    {
        public static string CharFromKeyCode(Keys key)
        {
            byte[] keyboardState = new byte[255];
            if (!UnsafeNativeMethods.GetKeyboardState(keyboardState))
            {
                return "";
            }

            uint virtualKeyCode = (uint)key;
            uint scanCode = UnsafeNativeMethods.MapVirtualKey(virtualKeyCode, 0);
            IntPtr inputLocaleIdentifier = UnsafeNativeMethods.GetKeyboardLayout(0);

            StringBuilder result = new StringBuilder(10);
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
    }
}
