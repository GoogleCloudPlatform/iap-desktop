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

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Google.Solutions.Platform.Interop
{
    public abstract class Win32SafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
#if DEBUG
        private readonly string stackTrace = Environment.StackTrace;
#endif

        protected Win32SafeHandle(bool ownsHandle)
            : base(ownsHandle)
        {
        }

        protected Win32SafeHandle(IntPtr handle, bool ownsHandle)
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

        ~Win32SafeHandle()
        {

#if DEBUG
            Debug.Assert(
                this.IsClosed,
                "Win32 handle was not closed.\n\n" +
                "Constructor was called at:\n\n" +
                this.stackTrace);
#endif

            Dispose(disposing: false);
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
            internal static extern bool CloseHandle(IntPtr handle);
        }
    }
}
