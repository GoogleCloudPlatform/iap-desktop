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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Google.Solutions.Mvvm.Theme
{
    public static class DpiVirtualization
    {
        static DpiVirtualization()
        {
            //
            // GetDpiForSystem returns the virtualized DPI.
            // GetSystemDpiForProcess returns the actual DPI.
            //
            // If the two differ, we know that virtualization
            // (with or without GDI scaling) is in play.
            //

            var systemDpi = NativeMethods.GetDpiForSystem();
            var processDpi = NativeMethods.GetSystemDpiForProcess(
                Process.GetCurrentProcess().Handle);

            IsActive = processDpi != systemDpi;
        }

        /// <summary>
        /// Determine if the current process is subject to DPI
        /// virtualization.
        /// </summary>
        public static bool IsActive { get; }

        private static class NativeMethods
        {
            [DllImport("user32")]
            public static extern uint GetSystemDpiForProcess(
                IntPtr process);

            [DllImport("user32")]
            public static extern uint GetDpiForSystem();
        }
    }
}
