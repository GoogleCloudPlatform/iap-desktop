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

namespace Google.Solutions.Platform
{
    public static class ProcessEnvironment
    {
        private static Architecture ArchitectureFromMachineType(
            int machineType)
        {
            return machineType switch
            {
                NativeMethods.IMAGE_FILE_MACHINE_I386 => Architecture.X86,
                NativeMethods.IMAGE_FILE_MACHINE_AMD64 => Architecture.X64,
                NativeMethods.IMAGE_FILE_MACHINE_ARM64 => Architecture.Arm64,
                _ => Architecture.Unknown
            };
        }

        /// <summary>
        /// Native architecture of the operating system.
        /// </summary>
        public static Architecture NativeArchitecture
        {
            get
            {
                try
                {
                    if (NativeMethods.IsWow64Process2(
                        Process.GetCurrentProcess().Handle,
                        out var _,
                        out var nativeMachine))
                    {
                        return ArchitectureFromMachineType(nativeMachine);
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    //
                    // IsWow64Process2 requires Windows 10 1709.
                    //
                }

                //
                // If IsWow64Process2 didn't work, or isn't available,
                // then it's unlikely that we're running on ARM hardware.
                //
                return Environment.Is64BitOperatingSystem 
                    ? Architecture.X64 
                    : Architecture.X86;
            }
        }

        /// <summary>
        /// Architecture of the process. This might differ from
        /// the native architecture if it's a WOW process.
        /// </summary>
        public static Architecture ProcessArchitecture
        {
            get
            {
                try
                { 
                    if (NativeMethods.IsWow64Process2(
                        Process.GetCurrentProcess().Handle,
                        out var processMachine,
                        out var nativeMachine))
                    {
                        if (processMachine == NativeMethods.IMAGE_FILE_MACHINE_UNKNOWN)
                        {
                            if (nativeMachine == NativeMethods.IMAGE_FILE_MACHINE_ARM64 &&
                                "amd64".Equals(
                                    Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"), 
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                //
                                // Windows 11 22H2 incorrectly returns
                                // IMAGE_FILE_MACHINE_UNKNOWN when emulating
                                // x64 processes on ARM64.
                                //
                                return Architecture.X64;
                            }

                            //
                            // Not a WOW process, refer to the native machine type.
                            //
                            return ArchitectureFromMachineType(nativeMachine);
                        }
                        else
                        {
                            //
                            // WOW process.
                            //
                            Debug.Assert(processMachine != nativeMachine);
                            return ArchitectureFromMachineType(processMachine);
                        }
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    //
                    // IsWow64Process2 requires Windows 10 1709.
                    //
                }

                //
                // Fall back to using the less reliable approach of using
                // environment variables.
                //
                return Environment
                    .GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?
                    .ToLower() switch
                {
                    "amd64" => Architecture.X64,
                    "x86" => Architecture.X86,
                    "arm64" => Architecture.Arm64,
                    _ => Architecture.Unknown
                };
            }
        }

        /// <summary>
        /// P/Invoke declarations.
        /// </summary>
        private static class NativeMethods
        {
            internal const int IMAGE_FILE_MACHINE_UNKNOWN = 0;
            internal const int IMAGE_FILE_MACHINE_I386 = 0x014c;
            internal const int IMAGE_FILE_MACHINE_AMD64 = 0x8664;
            internal const int IMAGE_FILE_MACHINE_ARM64 = 0xAA64;

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsWow64Process2(
                [In] IntPtr hProcess,
                [Out, MarshalAs(UnmanagedType.U2)] out ushort pProcessMachine,
                [Out, MarshalAs(UnmanagedType.U2)] out ushort pNativeMachine);
        }
    }
}
