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

using Google.Solutions.Common.Interop;
using Google.Solutions.Platform.Interop;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Google.Solutions.Mvvm.Shell
{
    public static class KnownFolders
    {
        private static string GetKnownFolderPath(
            Guid guid)
        {
            var hr = NativeMethods.SHGetKnownFolderPath(
                guid,
                0,
                IntPtr.Zero,
                out var pathHandle);
            if (hr != 0)
            {
                throw new Win32Exception(hr);
            }

            using (pathHandle)
            {
                return Marshal.PtrToStringUni(pathHandle.DangerousGetHandle());
            }
        }

        public static string Downloads
            => GetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"));

        //---------------------------------------------------------------------
        // P/Invoke declarations.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static extern int SHGetKnownFolderPath(
                [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
                uint dwFlags,
                IntPtr hToken,
                out CoTaskMemAllocSafeHandle ppszPath);
        }
    }
}
