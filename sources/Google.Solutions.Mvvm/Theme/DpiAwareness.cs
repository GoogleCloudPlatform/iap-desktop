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
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Google.Solutions.Mvvm.Theme
{
    public enum DpiAwarenessMode
    {
        DpiUnaware = 0,
        SystemAware = 1,
        PerMonitor = 2,
        PerMonitorV2 = 3,
        DpiUnawareGdiScaled = 4,
    }

    public static class DpiAwareness
    {
        private static DpiAwarenessMode currentMode = DpiAwarenessMode.DpiUnaware;

        public static SizeF DefaultDpi = new SizeF(96, 96);

        /// <summary>
        /// Gets or sets the high DPI mode of the process. 
        /// </summary>
        public static DpiAwarenessMode Mode
        {
            get => currentMode;
            set
            {
                var contextValue = value switch
                {
                    DpiAwarenessMode.DpiUnaware => NativeMethods.DPI_AWARENESS_CONTEXT.UNAWARE,
                    DpiAwarenessMode.SystemAware => NativeMethods.DPI_AWARENESS_CONTEXT.SYSTEM_AWARE,
                    DpiAwarenessMode.PerMonitor => NativeMethods.DPI_AWARENESS_CONTEXT.PER_MONITOR_AWARE,
                    DpiAwarenessMode.PerMonitorV2 => NativeMethods.DPI_AWARENESS_CONTEXT.PER_MONITOR_AWARE_V2,
                    DpiAwarenessMode.DpiUnawareGdiScaled => NativeMethods.DPI_AWARENESS_CONTEXT.UNAWARE_GDISCALED,
                    _ => throw new ArgumentException(nameof(value)),
                };

                //
                // NB. When enabling High DPI mode programmatically, WinForms won't
                // fire DpiChanged events.
                //

                if (!NativeMethods.SetProcessDpiAwarenessContext(contextValue))
                {
                    throw new Win32Exception();
                }
                currentMode = value;
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