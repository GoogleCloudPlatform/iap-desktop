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

using Google.Solutions.Common.Runtime;
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

        private static IntPtr ToDpiAwarenessContext(DpiAwarenessMode mode)
        {
            var contextValue = mode switch
            {
                DpiAwarenessMode.DpiUnaware => NativeMethods.DPI_AWARENESS_CONTEXT.UNAWARE,
                DpiAwarenessMode.SystemAware => NativeMethods.DPI_AWARENESS_CONTEXT.SYSTEM_AWARE,
                DpiAwarenessMode.PerMonitor => NativeMethods.DPI_AWARENESS_CONTEXT.PER_MONITOR_AWARE,
                DpiAwarenessMode.PerMonitorV2 => NativeMethods.DPI_AWARENESS_CONTEXT.PER_MONITOR_AWARE_V2,
                DpiAwarenessMode.DpiUnawareGdiScaled => NativeMethods.DPI_AWARENESS_CONTEXT.UNAWARE_GDISCALED,
                _ => throw new ArgumentException(nameof(mode)),
            };

            return new IntPtr((int)contextValue);
        }

        /// <summary>
        /// Windows uses 96 DPI by default.
        /// </summary>
        public static readonly SizeF DefaultDpi = new SizeF(96, 96);

        /// <summary>
        /// Default font size used by the Designer.
        /// </summary>
        public static readonly SizeF DefaultFontSize = new SizeF(6F, 13F);

        /// <summary>
        /// Check if the Windows version supports DPI awareness.
        /// </summary>
        public static bool IsSupported
        {
            get
            {
                //
                // SetThreadDpiAwarenessContext requires Windows 10 1607,
                // GDI scaling requires 1703 (= build 15063).
                // Use that as baseline.
                //
                var osVersion = Environment.OSVersion.Version;
                return osVersion.Major > 10 ||
                       (osVersion.Major == 10 && osVersion.Build >= 15063);
            }
        }

        private static void CheckSupported()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(
                    "DPI awareness requires Windows 10 1703 or a later version of Windows");
            }
        }

        /// <summary>
        /// Gets or sets the high DPI mode of the process. 
        /// </summary>
        public static DpiAwarenessMode ProcessMode
        {
            get => currentMode;
            set
            {
                CheckSupported();

                //
                // NB. When enabling High DPI mode programmatically, WinForms won't
                // fire DpiChanged events.
                //
                if (!NativeMethods.SetProcessDpiAwarenessContext(ToDpiAwarenessContext(value)))
                {
                    throw new Win32Exception();
                }

                currentMode = value;
            }
        }

        /// <summary>
        /// Temporarily enter the given mode. Restores the original
        /// DPI awareness mode when the returned object is disposed.
        /// </summary>
        public static IDisposable EnterThreadMode(DpiAwarenessMode mode)
        {
            CheckSupported();

            var original = NativeMethods.SetThreadDpiAwarenessContext(
                ToDpiAwarenessContext(mode));
            if (original == IntPtr.Zero)
            { 
                throw new Win32Exception(); 
            }

            return Disposable.For(() =>
            {
                if (NativeMethods.SetThreadDpiAwarenessContext(original) == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
            });
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [DllImport("User32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetProcessDpiAwarenessContext(
                [In] IntPtr context);

            [DllImport("User32", SetLastError = true)]
            public static extern IntPtr GetThreadDpiAwarenessContext();

            [DllImport("User32", SetLastError = true)]
            public static extern IntPtr SetThreadDpiAwarenessContext(
                [In] IntPtr dpiContext);

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