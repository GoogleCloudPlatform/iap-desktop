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

using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.Dispatch
{
    /// <summary>
    /// A Win32 process.
    /// </summary>
    public interface IWin32Process : IDisposable
    {
        /// <summary>
        /// Image name, without path.
        /// </summary>
        string ImageName { get; }

        /// <summary>
        /// Process ID.
        /// </summary>
        uint Id { get; }

        /// <summary>
        /// Handle for awaiting process termination.
        /// </summary>
        WaitHandle WaitHandle { get; }

        /// <summary>
        /// Process handle.
        /// </summary>
        SafeProcessHandle Handle { get; }

        /// <summary>
        /// Resume the process.
        /// </summary>
        void Resume();

        /// <summary>
        /// Send WM_CLOSE to process and wait for the process to
        /// terminate gracefully. Otherwise, terminate forcefully.
        /// 
        /// Returns true if the process terminated gracefully.
        /// </summary>
        Task<bool> CloseAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken);

        /// <summary>
        /// Wait for process to terminate.
        /// </summary>
        /// <returns>the exit code.</returns>
        Task<uint> WaitAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken);

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

        /// <summary>
        /// The NT/WTS session that this process is running in.
        /// </summary>
        IWtsSession Session { get; }
    }


    internal class Win32Process : DisposableBase, IWin32Process
    {
        private readonly string imageName;
        private readonly uint processId;
        private readonly SafeProcessHandle process;
        private readonly SafeThreadHandle mainThread;

        public Win32Process(
            string imageName,
            uint processId,
            SafeProcessHandle process,
            SafeThreadHandle mainThread)
        {
            this.imageName = imageName.ExpectNotNull(nameof(imageName));
            this.processId = processId;
            this.process = process.ExpectNotNull(nameof(process));
            this.mainThread = mainThread.ExpectNotNull(nameof(mainThread));

            Debug.Assert(!imageName.Contains("\\"), "Name does not contain path");
        }

        private void EnumerateTopLevelWindows(Action<IntPtr> action)
        {
            bool callback(IntPtr hwnd, int _)
            {
                if (!NativeMethods.IsWindowVisible(hwnd))
                {
                    //
                    // Window is top-level, but hidden. Ignore.
                    //
                }
                else if (NativeMethods.GetWindowThreadProcessId(
                        hwnd,
                        out var ownerProcessId) != 0 &&
                    ownerProcessId == this.processId)
                {
                    //
                    // This window belongs to our process. 
                    //
                    action(hwnd);
                }

                //
                // NB. There might be more top-level windows, so continue
                // the search.
                //

                return true;
            }

            //
            // Enumerate all top-level windows.
            //
            // Ignore ERROR_INVALID_PARAMETER errors as those are expected
            // in non-interactive sessions.
            //
            if (!NativeMethods.EnumWindows(callback, IntPtr.Zero) &&
                Marshal.GetLastWin32Error() is int lastError &&
                lastError != NativeMethods.ERROR_SUCCESS &&
                lastError != NativeMethods.ERROR_INVALID_PARAMETER)
            {
                throw DispatchException.FromLastWin32Error(
                    $"{this.imageName}: Enumerating windows failed");
            }
        }

        //---------------------------------------------------------------------
        // IProcess.
        //---------------------------------------------------------------------

        public SafeProcessHandle Handle => this.process;

        public string ImageName => this.imageName;

        public WaitHandle WaitHandle => this.process.ToWaitHandle(false);

        public uint Id => this.processId;

        public IWtsSession Session => WtsSession.FromProcessId(this.processId);

        public bool IsRunning
        {
            get =>
                !this.process.IsClosed &&
                NativeMethods.GetExitCodeProcess(this.process, out var exitCode) &&
                exitCode == NativeMethods.STILL_ACTIVE;
        }

        public int WindowCount
        {
            get
            {
                var windowCount = 0;
                EnumerateTopLevelWindows(_ => windowCount++);

                return windowCount;
            }
        }

        public async Task<uint> WaitAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using (var waitHandle = this.process.ToWaitHandle(false))
            {
                if (await waitHandle
                    .WaitAsync(timeout, cancellationToken)
                    .ConfigureAwait(false))
                {
                    //
                    // Terminated.
                    //
                    NativeMethods.GetExitCodeProcess(this.process, out var exitCode);
                    return exitCode;
                }
                else
                {
                    throw new TimeoutException(
                        "The process did not terminate within the allotted timeout");
                }
            }
        }

        public async Task<bool> CloseAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            if (!this.IsRunning)
            {
                return true;
            }

            //
            // Attempt to gracefully close the process by sending a WM_CLOSE message.
            //
            // See https://web.archive.org/web/20150311053121/http://support.microsoft.com/kb/178893
            // for details.
            //
            var messagesPosted = 0;

            EnumerateTopLevelWindows(hwnd =>
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
            });

            if (messagesPosted > 0)
            {
                //
                // Give the process some time to digest the messages.
                //
                using (var waitHandle = this.process.ToWaitHandle(false))
                {
                    if (await waitHandle
                        .WaitAsync(timeout, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        //
                        // Process exited gracefully within the timeout.
                        //
                        return true;
                    }
                }
            }

            //
            // Use force.
            //
            Terminate(0);

            //
            // If we posted a message and got here anyway, then it wasn't
            // a graceful close.
            //
            return messagesPosted == 0;
        }

        public void Resume()
        {
            if (NativeMethods.ResumeThread(this.mainThread) < 0)
            {
                throw DispatchException.FromLastWin32Error(
                    $"{this.imageName}: Resuming the process failed");
            }
        }

        public void Terminate(uint exitCode)
        {
            if (!NativeMethods.TerminateProcess(this.process, exitCode))
            {
                throw DispatchException.FromLastWin32Error(
                    $"{this.imageName}: Terminating the process failed");
            }
        }

        public override string ToString()
        {
            return $"{this.ImageName} (PID {this.processId})";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.mainThread.Close();
            this.process.Close();
        }

        //---------------------------------------------------------------------
        // Factory methods.
        //---------------------------------------------------------------------

        public static Win32Process FromProcessId(uint processId)
        {
            //
            // Open process.
            //
            var process = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION | NativeMethods.SYNCHRONIZE,
                false,
                processId);
            if (process.IsInvalid)
            {
                throw DispatchException.FromLastWin32Error(
                    $"The process with ID {processId} does not exist or is inaccessible");
            }

            //
            // Get image name.
            //
            var imageNameBuffer = new StringBuilder(260);
            var imageNameBufferLength = imageNameBuffer.Capacity;
            if (!NativeMethods.QueryFullProcessImageName(
                process,
                0,
                imageNameBuffer,
                ref imageNameBufferLength))
            {
                process.Dispose();

                throw DispatchException.FromLastWin32Error(
                    $"Querying the image name of the process with ID {processId} failed");
            }

            return new Win32Process(
                new FileInfo(imageNameBuffer.ToString()).Name,
                processId,
                process,
                new SafeThreadHandle(IntPtr.Zero, true));
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            internal const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
            internal const uint SYNCHRONIZE = 0x00100000;

            internal const uint WM_CLOSE = 0x0010;
            internal const int STILL_ACTIVE = 259;
            internal const int ERROR_SUCCESS = 0;
            internal const int ERROR_INVALID_PARAMETER = 87;

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern int ResumeThread(
                SafeThreadHandle hThread);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool TerminateProcess(
                SafeProcessHandle hProcess,
                uint exitCode);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetExitCodeProcess(
                SafeProcessHandle hProcess,
                out uint lpExitCode);

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

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern SafeProcessHandle OpenProcess(
                uint processAccess,
                bool bInheritHandle,
                uint processId);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool QueryFullProcessImageName(
                [In] SafeProcessHandle hProcess,
                [In] int dwFlags,
                [Out] StringBuilder lpExeName,
                ref int lpdwSize); [DllImport("user32.dll")]

            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindowVisible(IntPtr hWnd);
        }
    }

    internal class SafeThreadHandle : SafeWin32Handle
    {
        public SafeThreadHandle(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }
    }
}
