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
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Google.Solutions.Platform.IO
{
    /// <summary>
    /// Win32 anonmous pipe.
    /// </summary>
    public class AnonymousPipe : DisposableBase
    {
        internal SafeFileHandle ReadSideHandle { get; }
        internal SafeFileHandle WriteSideHandle { get; }

        private readonly object streamLock = new object();
        private Stream? readSideStream;
        private Stream? writeSideStream;

        public AnonymousPipe(bool inheritReadSide, bool inheritWriteSide)
        {
            var securityAttributes = new NativeMethods.SECURITY_ATTRIBUTES()
            {
                nLength = Marshal.SizeOf<NativeMethods.SECURITY_ATTRIBUTES>(),
                bInheritHandle = (inheritReadSide || inheritWriteSide),
                lpSecurityDescriptor = IntPtr.Zero
            };

            if (!NativeMethods.CreatePipe(
                out var readSideHandle,
                out var writeSideHandle,
                ref securityAttributes,
                0) ||
                readSideHandle == null || 
                writeSideHandle == null)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "failed to create pipe");
            }

            if (inheritReadSide)
            {
                //
                // Mark write side as non-inheritable.
                //
                NativeMethods.SetHandleInformation(
                    writeSideHandle,
                    NativeMethods.HANDLE_FLAGS.INHERIT,
                    0);
            }

            if (inheritWriteSide)
            {
                //
                // Mark read side as non-inheritable.
                //
                NativeMethods.SetHandleInformation(
                    readSideHandle,
                    NativeMethods.HANDLE_FLAGS.INHERIT,
                    0);
            }

            this.ReadSideHandle = readSideHandle;
            this.WriteSideHandle = writeSideHandle;
        }

        public AnonymousPipe() : this(false, false)
        { }

        public Stream ReadSide
        {
            get
            {
                Debug.Assert(!this.ReadSideHandle.IsClosed);
                lock (this.streamLock)
                {
                    if (this.readSideStream == null)
                    {
                        this.readSideStream = new FileStream(this.ReadSideHandle, FileAccess.Read);
                    }
                }

                return this.readSideStream;
            }
        }

        public Stream WriteSide
        {
            get
            {
                Debug.Assert(!this.WriteSideHandle.IsClosed);
                lock (this.streamLock)
                {
                    if (this.writeSideStream == null)
                    {
                        this.writeSideStream = new FileStream(this.WriteSideHandle, FileAccess.Write);
                    }
                }

                return this.writeSideStream;
            }
        }

        public void CloseWriteSide()
        {
            this.WriteSideHandle.Close();
        }

        public void CloseReadSide()
        {
            this.ReadSideHandle.Close();
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            this.ReadSideHandle.Dispose();
            this.WriteSideHandle.Dispose();
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct SECURITY_ATTRIBUTES
            {
                public int nLength;
                public IntPtr lpSecurityDescriptor;
                public bool bInheritHandle;
            }

            [Flags]
            internal enum HANDLE_FLAGS : uint
            {
                None = 0,
                INHERIT = 1,
                PROTECT_FROM_CLOSE = 2
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool SetHandleInformation(
                SafeHandle hObject,
                HANDLE_FLAGS dwMask,
                HANDLE_FLAGS dwFlags);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern bool CreatePipe(
                out SafeFileHandle hReadPipe,
                out SafeFileHandle hWritePipe,
                IntPtr lpPipeAttributes,
                int nSize);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern bool CreatePipe(
                out SafeFileHandle hReadPipe,
                out SafeFileHandle hWritePipe,
                ref SECURITY_ATTRIBUTES lpPipeAttributes,
                int nSize);
        }
    }
}
