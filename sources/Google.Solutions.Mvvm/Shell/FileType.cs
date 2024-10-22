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

using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Google.Solutions.Mvvm.Shell
{
    public sealed class FileType : IDisposable
    {
        /// <summary>
        /// Icon for file or directory.
        /// </summary>
        public Image FileIcon { get; }

        /// <summary>
        /// Type of file, for ex "Text document".
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Check if this is a file (as opposed to a directory).
        /// </summary>
        public bool IsFile { get; }

        public FileType(
            string typeName,
            bool isFile,
            Image icon)
        {
            this.TypeName = typeName;
            this.IsFile = isFile;
            this.FileIcon = icon;
        }

        /// <summary>
        /// Look up type information for a file or folder.
        /// </summary>
        public static FileType Lookup(
            string filePath,
            FileAttributes fileAttributes,
            IconFlags iconFlags)
        {
            filePath.ExpectNotNull(nameof(filePath));

            //
            // NB. The file might not exist, so pass the
            // SHGFI_USEFILEATTRIBUTES flag.
            //
            var flags =
                  NativeMethods.SHGFI_USEFILEATTRIBUTES
                | NativeMethods.SHGFI_ICON
                | NativeMethods.SHGFI_SMALLICON
                | NativeMethods.SHGFI_TYPENAME
                | (uint)iconFlags;

            var fileInfo = new NativeMethods.SHFILEINFOA();
            if (NativeMethods.SHGetFileInfo(
                filePath,
                fileAttributes,
                ref fileInfo,
                (uint)Marshal.SizeOf<NativeMethods.SHFILEINFOA>(),
                flags) == IntPtr.Zero)
            {
                throw new COMException(
                    $"Looking up the file type for file {filePath} failed");
            }

            Debug.Assert(fileInfo.hIcon != IntPtr.Zero);

            using (var icon = Icon.FromHandle(fileInfo.hIcon))
            {
                return new FileType(
                    fileInfo.szTypeName,
                    !fileAttributes.HasFlag(FileAttributes.Directory),
                    icon.ToBitmap());
            }
        }

        public void Dispose()
        {
            this.FileIcon.Dispose();
        }

        [Flags]
        public enum IconFlags : uint
        {
            None = 0,
            Open = NativeMethods.SHGFI_OPENICON,
        }

        //---------------------------------------------------------------------
        // P/Invoke declarations.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            internal const uint SHGFI_LARGEICON = 0x0;
            internal const uint SHGFI_SMALLICON = 0x1;
            internal const uint SHGFI_OPENICON = 0x2;
            internal const uint SHGFI_SHELLICONSIZE = 0x4;
            internal const uint SHGFI_USEFILEATTRIBUTES = 0x10;
            internal const uint SHGFI_ICON = 0x100;
            internal const uint SHGFI_DISPLAYNAME = 0x200;
            internal const uint SHGFI_TYPENAME = 0x400;

            private const int MAX_PATH = 260;
            private const int NAMESIZE = 80;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct SHFILEINFOA
            {
                /// <summary>
                /// A handle to the icon that represents the file. 
                /// </summary>
                public IntPtr hIcon;

                /// <summary>
                /// The index of the icon image within the system image list.
                /// </summary>
                public int iIcon;

                /// <summary>
                /// An array of values that indicates the attributes of the
                /// file object. 
                /// </summary>
                public uint dwAttributes;

                /// <summary>
                /// A string that contains the name of the file as it appears
                /// in the Windows Shell
                /// </summary>
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
                public string szDisplayName;

                /// <summary>
                /// A string that describes the type of file.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NAMESIZE)]
                public string szTypeName;
            };

            [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr SHGetFileInfo(
                [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
                [In] FileAttributes dwFileAttributes,
                [In][Out] ref SHFILEINFOA psfi,
                [In] uint cbFileInfo,
                [In] uint uFlags
            );
        }
    }
}
