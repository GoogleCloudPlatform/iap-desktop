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

using Google.Apis.Util;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Google.Solutions.Mvvm.Shell
{
    public sealed class FileType : IDisposable
    {
        public Image FileIcon { get; }

        public string TypeName { get; }
        public string DisplayName { get; }

        public FileType(
            string typeName,
            string displayName,
            Image icon)
        {
            this.TypeName = typeName;
            this.DisplayName = displayName;
            this.FileIcon = icon;
        }

        /// <summary>
        /// Look up type information for a file or folder.
        /// </summary>
        public static FileType Lookup(
            string filePath,
            FileAttributes fileAttributes,
            IconSize size)
        {
            filePath.ThrowIfNull(nameof(filePath));

            //
            // NB. The file might not exist, so pass the
            // SHGFI_USEFILEATTRIBUTES flag.
            //
            var flags =
                  NativeMethods.SHGFI_USEFILEATTRIBUTES
                | NativeMethods.SHGFI_ICON
                | NativeMethods.SHGFI_DISPLAYNAME
                | NativeMethods.SHGFI_TYPEYNAME
                | (uint)size;

            var fileInfo = new NativeMethods.SHFILEINFO();
            if (NativeMethods.SHGetFileInfo(
                filePath,
                fileAttributes,
                ref fileInfo,
                (uint)Marshal.SizeOf<NativeMethods.SHFILEINFO>(),
                flags) == IntPtr.Zero)
            {
                throw new COMException(
                    $"Looking up the file type for file {filePath} failed");
            }

            Debug.Assert(fileInfo.hIcon != IntPtr.Zero);

            using (var icon = Icon.FromHandle(fileInfo.hIcon))
            {
                return new FileType(fileInfo.szTypeName, fileInfo.szDisplayName, icon.ToBitmap());
            }
        }

        public void Dispose()
        {
            this.FileIcon.Dispose();
        }

        public enum IconSize : uint
        {
            Large = NativeMethods.SHGFI_LARGEICON,
            Small = NativeMethods.SHGFI_SMALLICON
        }

        //---------------------------------------------------------------------
        // P/Invoke declations.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            internal const uint SHGFI_LARGEICON = 0x0;
            internal const uint SHGFI_SMALLICON = 0x1;
            internal const uint SHGFI_OPENICON = 0x2;
            internal const uint SHGFI_USEFILEATTRIBUTES = 0x10;
            internal const uint SHGFI_ICON = 0x100;
            internal const uint SHGFI_DISPLAYNAME = 0x200;
            internal const uint SHGFI_TYPEYNAME = 0x400;

            internal const int MAX_PATH = 260;
            internal const int NAMESIZE = 80;

            [StructLayout(LayoutKind.Sequential)]
            public struct SHFILEINFO
            {
                public IntPtr hIcon;
                public int iIcon;
                public uint dwAttributes;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
                public string szDisplayName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NAMESIZE)]
                public string szTypeName;
            };

            [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr SHGetFileInfo(
                [In] string pszPath,
                [In] FileAttributes dwFileAttributes,
                [In][Out] ref SHFILEINFO psfi,
                [In] uint cbFileInfo,
                [In] uint uFlags
            );
        }
    }
}
