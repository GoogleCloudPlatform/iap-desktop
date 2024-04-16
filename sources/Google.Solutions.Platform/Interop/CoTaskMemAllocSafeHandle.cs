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


namespace Google.Solutions.Platform.Interop
{
    public sealed class CoTaskMemAllocSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static CoTaskMemAllocSafeHandle Zero = new CoTaskMemAllocSafeHandle(ownsHandle: false);

        private CoTaskMemAllocSafeHandle()
            : base(ownsHandle: true)
        {
        }

        private CoTaskMemAllocSafeHandle(bool ownsHandle)
            : base(ownsHandle)
        {
        }

        public static CoTaskMemAllocSafeHandle Alloc(int cb)
        {
            var handle = new CoTaskMemAllocSafeHandle();
            handle.SetHandle(Marshal.AllocCoTaskMem(cb));
            if (handle.IsInvalid)
            {
                handle.SetHandleAsInvalid();
                throw new OutOfMemoryException();
            }

            return handle;
        }

        public static CoTaskMemAllocSafeHandle Alloc(string s)
        {
            var handle = new CoTaskMemAllocSafeHandle();
            handle.SetHandle(Marshal.StringToCoTaskMemAnsi(s));
            return handle;
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeCoTaskMem(this.handle);
            return true;
        }
    }
}
