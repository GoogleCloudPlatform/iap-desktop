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
using System.Runtime.InteropServices;

namespace Google.Solutions.Common.Interop
{
    /// <summary>
    /// Safe handle for GlobalAlloc memory allocations.
    /// </summary>
    public sealed class GlobalAllocSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static GlobalAllocSafeHandle Zero = new GlobalAllocSafeHandle(ownsHandle: false);

        private GlobalAllocSafeHandle()
            : base(ownsHandle: true)
        {
        }

        private GlobalAllocSafeHandle(bool ownsHandle)
            : base(ownsHandle)
        {
        }

        public static GlobalAllocSafeHandle GlobalAlloc(uint cb)
        {
            var handle = new GlobalAllocSafeHandle();
            handle.SetHandle(Marshal.AllocHGlobal(new IntPtr(cb)));
            if (handle.IsInvalid)
            {
                handle.SetHandleAsInvalid();
                throw new OutOfMemoryException();
            }

            return handle;
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(this.handle);
            SetHandleAsInvalid();
            return true;
        }
    }
}
