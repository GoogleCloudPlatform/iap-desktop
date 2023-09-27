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

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#if NETFRAMEWORK

namespace Google.Solutions.Mvvm
{
    public enum HighDpiMode
    {
        DpiUnaware = 0,
        SystemAware = 1,
        PerMonitor = 2,
        PerMonitorV2 = 3,
        DpiUnawareGdiScaled	= 4,
    }

    public static class ApplicationExtensions
    {
        /// <summary>
        /// Sets the high DPI mode of the process. This extension method
        /// emulates the behavior of the .NET 3.0+ method.
        /// </summary>
        public static bool SetHighDpiMode(HighDpiMode highDpiMode)
        {
            NativeMethods.DPI_AWARENESS_CONTEXT mode;

            switch (highDpiMode)
            {
                case HighDpiMode.DpiUnaware:
                    mode = NativeMethods.DPI_AWARENESS_CONTEXT.UNAWARE;
                    break;
                case HighDpiMode.SystemAware: 
                    mode = NativeMethods.DPI_AWARENESS_CONTEXT.SYSTEM_AWARE;
                    break;
                case HighDpiMode.PerMonitor:  
                    mode = NativeMethods.DPI_AWARENESS_CONTEXT.PER_MONITOR_AWARE;
                    break;
                case HighDpiMode.PerMonitorV2: 
                    mode = NativeMethods.DPI_AWARENESS_CONTEXT.PER_MONITOR_AWARE_V2;
                    break;
                case HighDpiMode.DpiUnawareGdiScaled:
                    mode = NativeMethods.DPI_AWARENESS_CONTEXT.UNAWARE_GDISCALED;
                    break;
                default:
                    throw new ArgumentException(nameof(highDpiMode));
            };

            //
            // NB. When enabling High DPI mode programmatically, WinForms won't
            // fire DpiChanged events.
            //

            return NativeMethods.SetProcessDpiAwarenessContext(mode);
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

#endif