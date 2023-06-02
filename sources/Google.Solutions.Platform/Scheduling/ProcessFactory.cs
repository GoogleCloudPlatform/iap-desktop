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


using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace Google.Solutions.Platform.Scheduling
{
    /// <summary>
    /// Creates processes.
    /// </summary>
    public interface IProcessFactory
    {
        /// <summary>
        /// Start a new process.
        /// 
        /// The process is created suspended and must be resumed explicitly.
        /// </summary>
        IProcess CreateProcess(
            string executable,
            string arguments);

        IProcess CreateProcessAsUser(
            string executable,
            string arguments,
            // flags...
            NetworkCredential credential);
    }

    public interface IProcess : IDisposable
    {
        /// <summary>
        /// Image name, without path.
        /// </summary>
        string ImageName { get; }

        /// <summary>
        /// Process handle.
        /// </summary>
        SafeHandle Handle { get; }

        /// <summary>
        /// Resume the process.
        /// </summary>
        void Resume();

        /// <summary>
        /// Request an orderly close by sending a a WM_CLOSE
        /// message to the process. This only works for GUI
        /// processes.
        /// 
        /// Returns true if at least one window was found.
        /// </summary>
        bool Close();

        /// <summary>
        /// Forcefully terminate the process.
        /// </summary>
        void Terminate(uint exitCode);

        /// <summary>
        /// Indicates whether the process is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Numer of top-level windows owned by this process.
        /// </summary>
        int WindowCount { get; }
    }

    public class ProcessFactory : IProcessFactory
    {
        private static string Quote (string s)
        {
            return $"\"{s}\"";
        }

        public IProcess CreateProcess(
            string executable, 
            string arguments)
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
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    $"Launching process for {executable} failed");
            }

            return new Process(
                new FileInfo(executable).Name,
                processInfo.dwProcessId,
                new SafeProcessHandle(processInfo.hProcess, true),
                new SafeThreadHandle(processInfo.hThread, true));
        }

        public IProcess CreateProcessAsUser(
            string executable, 
            string arguments, 
            NetworkCredential credential)
        {
            throw new NotImplementedException();
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class Process : DisposableBase, IProcess
        {
            private readonly string name;
            private readonly uint processId;
            private readonly SafeProcessHandle process;
            private readonly SafeThreadHandle mainThread;

            public Process(
                string name,
                uint processId,
                SafeProcessHandle process,
                SafeThreadHandle mainThread)
            {
                this.name = name.ExpectNotNull(nameof(name));
                this.processId = processId;
                this.process = process.ExpectNotNull(nameof(process));
                this.mainThread = mainThread.ExpectNotNull(nameof(mainThread));
            }

            private void ThrowLastError(string message)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    $"{this.name}: {message}");
            }

            //-----------------------------------------------------------------
            // IProcess.
            //---------------------------------------------------------------------

            public SafeHandle Handle => this.process;

            public string ImageName => this.name;

            public bool IsRunning
            {
                get => 
                    NativeMethods.GetExitCodeProcess(this.process, out var exitCode) &&
                    exitCode == NativeMethods.STILL_ACTIVE;
            }

            public int WindowCount
            {
                get
                {
                    int windowCount = 0;

                    NativeMethods.EnumWindowsProc callback = (hwnd, _) =>
                    {
                        //
                        // Lookup the process ID that owns this window.
                        //
                        if (NativeMethods.GetWindowThreadProcessId(
                                hwnd,
                                out var ownerProcessId) != 0 &&
                            ownerProcessId == this.processId)
                        {
                            //
                            // This window belongs to our process. 
                            //
                            windowCount++;
                        }

                        return true;
                    };

                    //
                    // Enumerate all top-level windows.
                    //
                    if (!NativeMethods.EnumWindows(callback, IntPtr.Zero))
                    {
                        ThrowLastError("Enumerating windows failed");
                    }

                    return windowCount;
                }
            }

            public bool Close()
            {
                //
                // Attempt to gracefully close the process by sending a WM_CLOSE message.
                //
                // See https://web.archive.org/web/20150311053121/http://support.microsoft.com/kb/178893
                // for details.
                //
                int messagesPosted = 0;

                NativeMethods.EnumWindowsProc callback = (hwnd, _) =>
                {
                    //
                    // Lookup the process ID that owns this window.
                    //
                    if (NativeMethods.GetWindowThreadProcessId(
                            hwnd, 
                            out var ownerProcessId) != 0 &&
                        ownerProcessId == this.processId)
                    {
                        //
                        // This window belongs to our process. Post a message to 
                        // tell it to close.
                        //
                        NativeMethods.PostMessage(
                            hwnd, 
                            NativeMethods.WM_CLOSE, 
                            IntPtr.Zero, 
                            IntPtr.Zero);
                        
                        messagesPosted++;

                        //
                        // NB. There might be more top-level windows, so continue
                        // the search.
                        //
                    }

                    return true;
                };

                //
                // Enumerate all top-level windows.
                //
                if (!NativeMethods.EnumWindows(callback, IntPtr.Zero))
                {
                    ThrowLastError(
                        "Gracefully closing process failed");
                }

                return messagesPosted > 0;
            }

            public void Resume()
            {
                if (NativeMethods.ResumeThread(this.mainThread) < 0)
                {
                    ThrowLastError("Resuming the process failed"); // TODO: Add test
                }
            }

            public void Terminate(uint exitCode)
            {
                if (!NativeMethods.TerminateProcess(this.process, exitCode))
                {
                    ThrowLastError("Terminating the process failed");
                }
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                this.mainThread.Close();
                this.process.Close();
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            internal const uint WM_CLOSE = 0x0010;
            internal const int STILL_ACTIVE = 259;

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

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern int ResumeThread(
                SafeHandle hThread);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool TerminateProcess(
                SafeHandle hProcess,
                uint exitCode);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetExitCodeProcess(
                SafeHandle hProcess, 
                out uint lpExitCode);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CreateProcess(
                string lpApplicationName,
                string lpCommandLine,
                IntPtr lpProcessAttributes,
                IntPtr lpThreadAttributes,
                bool bInheritHandles,
                uint dwCreationFlags,
                IntPtr lpEnvironment,
                string lpCurrentDirectory,
                [In] ref STARTUPINFO lpStartupInfo,
                out PROCESS_INFORMATION lpProcessInformation);

            [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
            internal static extern bool CloseHandle(IntPtr handle);

            internal delegate bool EnumWindowsProc(
                IntPtr hwnd, 
                int lParam);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool EnumWindows(
                EnumWindowsProc lpEnumFunc, 
                IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern uint GetWindowThreadProcessId(
                IntPtr hWnd, 
                out uint lpdwProcessId);

            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            internal static extern bool PostMessage(
                IntPtr hWnd, 
                uint msg, 
                IntPtr wParam, 
                IntPtr lParam);
        }

        private class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeThreadHandle(IntPtr handle, bool ownsHandle) 
                : base(ownsHandle)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                var closed = NativeMethods.CloseHandle(this.handle);
                Debug.Assert(closed);
                return closed;
            }
        }
    }
}
