//
// Copyright 2022 Google LLC
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Interop
{
    /// <summary>
    /// Safe handle for LocalAlloc memory allocations.
    /// </summary>
    public sealed class LocalAllocSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static readonly LocalAllocSafeHandle Zero = new LocalAllocSafeHandle(ownsHandle: false);

        private LocalAllocSafeHandle()
            : base(ownsHandle: true)
        {
        }

        private LocalAllocSafeHandle(bool ownsHandle)
            : base(ownsHandle)
        {
        }

        [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types")]
        public static LocalAllocSafeHandle LocalAlloc(uint cb)
        {
            LocalAllocSafeHandle safeLocalFree = UnsafeNativeMethods.LocalAlloc(
                0, 
                (UIntPtr)cb);
            if (safeLocalFree.IsInvalid)
            {
                safeLocalFree.SetHandleAsInvalid();
                throw new OutOfMemoryException();
            }

            return safeLocalFree;
        }

        protected override bool ReleaseHandle()
        {
            UnsafeNativeMethods.LocalFree(this.handle);
            SetHandleAsInvalid();
            return true;
        }

        //---------------------------------------------------------------------
        // P/Invoke definitions.
        //---------------------------------------------------------------------

        private class UnsafeNativeMethods
        {
            [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
            internal static extern LocalAllocSafeHandle LocalAlloc(int uFlags, UIntPtr sizetdwBytes);

            [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
            internal static extern IntPtr LocalFree(IntPtr handle);
        }
    }
}