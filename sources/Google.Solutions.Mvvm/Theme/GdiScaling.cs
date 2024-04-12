//
// Copyright 2023 Google LLC
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

using System.Runtime.InteropServices;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Helper class to toggle between "Unaware" and "GDI scaled" DPI mode.
    /// </summary>
    public static class GdiScaling // TODO: use SetHighDpiMode instead.
    {
        private static bool active;

        /// <summary>
        /// Determine if the current process is subject to DPI
        /// virtualization.
        /// </summary>
        public static bool IsEnabled
        {
            //
            // NB. There's no way to query Windows whether GDI scaling
            // is active or not.
            //
            get => active;
            set
            {
                NativeMethods.SetProcessDpiAwarenessContext(value
                    ? NativeMethods.DPI_AWARENESS_CONTEXT.UNAWARE_GDISCALED
                    : NativeMethods.DPI_AWARENESS_CONTEXT.UNAWARE);
                active = value;
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [DllImport("User32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetProcessDpiAwarenessContext(
                [In] DPI_AWARENESS_CONTEXT context);

            public enum DPI_AWARENESS_CONTEXT : int
            {
                UNAWARE = -1,
                SYSTEM_AWARE = -2,
                PER_MONITOR_AWARE = -3,
                PER_MONITOR_AWARE_V2 = -4,
                UNAWARE_GDISCALED = -5,
            }
        }
    }
}
