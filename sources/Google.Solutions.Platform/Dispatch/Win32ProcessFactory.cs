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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Google.Solutions.Platform.Dispatch
{
    /// <summary>
    /// Factory for Win32 processes.
    /// </summary>
    public interface IWin32ProcessFactory
    {
        /// <summary>
        /// Start a new process.
        /// 
        /// The process is created suspended and must be resumed explicitly.
        /// </summary>
        IWin32Process CreateProcess(
            string executable,
            string? arguments);

        /// <summary>
        /// Start a new process as a different user.
        /// 
        /// The process is created suspended and must be resumed explicitly.
        /// </summary>
        IWin32Process CreateProcessAsUser(
            string executable,
            string? arguments,
            LogonFlags flags,
            NetworkCredential credential);
    }

    [Flags]
    public enum LogonFlags : uint
    {
        WithProfile = 1,        // LOGON_WITH_PROFILE
        NetCredentialsOnly = 2  // LOGON_NETCREDENTIALS_ONLY
    }

    public class Win32ProcessFactory : IWin32ProcessFactory
    {
        private const int ExitCodeForFailedProcessCreation = 250;

        private static string Quote(string s)
        {
            return $"\"{s}\"";
        }

        /// <summary>
        /// Allow deriving classes to do something with the process
        /// before the factory returns it.
        /// </summary>
        private protected virtual void OnProcessCreated(Win32Process process)
        { }

        private void InvokeOnProcessCreated(Win32Process process)
        {
            try
            {
                OnProcessCreated(process);
            }
            catch (Exception e)
            {
                PlatformTraceSource.Log.TraceError(e);
                process.Terminate(ExitCodeForFailedProcessCreation);
                process.Dispose();
                throw;
            }
        }

        //---------------------------------------------------------------------
        // IWin32ProcessFactory.
        //---------------------------------------------------------------------

        public IWin32Process CreateProcess(
            string executable,
            string? arguments)
        {
            executable.ExpectNotEmpty(nameof(executable));

            using (PlatformTraceSource.Log.TraceMethod()
                .WithParameters(executable, arguments))
            {
                var startupInfo = new NativeMethods.STARTUPINFO()
                {
                    cb = Marshal.SizeOf<NativeMethods.STARTUPINFO>()
                };

                if (!NativeMethods.CreateProcess(
                    null,
                    $"{Quote(executable)} {arguments}",
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    NativeMethods.CREATE_SUSPENDED,
                    IntPtr.Zero,
                    null,
                    ref startupInfo,
                    out var processInfo))
                {
                    throw DispatchException.FromLastWin32Error(
                        $"Launching process for {executable} failed");
                }

                var process = new Win32Process(
                    new FileInfo(executable).Name,
                    processInfo.dwProcessId,
                    new SafeProcessHandle(processInfo.hProcess, true),
                    new SafeThreadHandle(processInfo.hThread, true));

                InvokeOnProcessCreated(process);
                return process;
            }
        }

        public IWin32Process CreateProcessAsUser(
            string executable,
            string? arguments,
            LogonFlags flags,
            NetworkCredential credential)
        {
            executable.ExpectNotEmpty(nameof(executable));
            credential.ExpectNotNull(nameof(credential));

            using (PlatformTraceSource.Log.TraceMethod()
                .WithParameters(executable, arguments, flags, credential.UserName))
            {
                Debug.Assert(
                    credential.UserName.Contains("@") ||
                    credential.Domain != null);

                var startupInfo = new NativeMethods.STARTUPINFO()
                {
                    cb = Marshal.SizeOf<NativeMethods.STARTUPINFO>()
                };

                //
                // NB. CreateProcessWithLogonW does not accept the
                // DOMAIN\user format.
                //
                if (credential.UserName.Contains('\\'))
                {
                    var usernameParts = credential.UserName.Split('\\');
                    credential = new NetworkCredential(
                        usernameParts[1],
                        credential.SecurePassword,
                        usernameParts[0]);
                }

                if (!NativeMethods.CreateProcessWithLogonW(
                    credential.UserName,
                    credential.Domain,
                    credential.Password,
                    flags,
                    null,
                    $"{Quote(executable)} {arguments}",
                    NativeMethods.CREATE_SUSPENDED,
                    IntPtr.Zero,
                    null,
                    ref startupInfo,
                    out var processInfo))
                {
                    throw DispatchException.FromLastWin32Error(
                        $"Launching process for {executable} failed");
                }

                var process = new Win32Process(
                    new FileInfo(executable).Name,
                    processInfo.dwProcessId,
                    new SafeProcessHandle(processInfo.hProcess, true),
                    new SafeThreadHandle(processInfo.hThread, true));

                InvokeOnProcessCreated(process);
                return process;
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            internal const uint CREATE_SUSPENDED = 0x00000004;
            internal const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
            internal const uint STARTF_USESTDHANDLES = 0x00000100;
            internal const uint PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct STARTUPINFO
            {
                public int cb;
                public string lpReserved;
                public string lpDesktop;
                public string lpTitle;
                public int dwX;
                public int dwY;
                public int dwXSize;
                public int dwYSize;
                public int dwXCountChars;
                public int dwYCountChars;
                public int dwFillAttribute;
                public uint dwFlags;
                public short wShowWindow;
                public short cbReserved2;
                public IntPtr lpReserved2;
                public IntPtr hStdInput;
                public IntPtr hStdOutput;
                public IntPtr hStdError;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct PROCESS_INFORMATION
            {
                public IntPtr hProcess;
                public IntPtr hThread;
                public uint dwProcessId;
                public uint dwThreadId;
            }

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CreateProcess(
                string? lpApplicationName,
                string lpCommandLine,
                IntPtr lpProcessAttributes,
                IntPtr lpThreadAttributes,
                bool bInheritHandles,
                uint dwCreationFlags,
                IntPtr lpEnvironment,
                string? lpCurrentDirectory,
                [In] ref STARTUPINFO lpStartupInfo,
                out PROCESS_INFORMATION lpProcessInformation);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CreateProcessWithLogonW(
                string userName,
                string? domain,
                string password,
                LogonFlags logonFlags,
                string? applicationName,
                string commandLine,
                uint dwCreationFlags,
                IntPtr environment,
                string? currentDirectory,
                [In] ref STARTUPINFO lpStartupInfo,
                out PROCESS_INFORMATION lpProcessInformation);
        }
    }
}
