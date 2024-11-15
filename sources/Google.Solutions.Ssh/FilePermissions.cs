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

using System;

namespace Google.Solutions.Ssh
{
    [Flags]
    public enum FilePermissions : uint
    {
        OwnerRead = 0x4 << 6,
        OwnerWrite = 0x2 << 6,
        OwnerExecute = 0x1 << 6,

        GroupRead = 0x4 << 3,
        GroupWrite = 0x2 << 3,
        GroupExecute = 0x1 << 3,

        OtherRead = 0x4,
        OtherWrite = 0x2,
        OtherExecute = 0x1,

        //
        // File type.
        //
        Fifo = 0x1000,
        CharacterDevice = 0x2000,
        Directory = 0x4000,
        BlockSpecial = 0x6000,
        Regular = 0x8000,
        SymbolicLink = 0xa000,
        Socket = 0xc000,

        None = 0x0,
    }

    public static class FilePermissionExtensions
    {
        private static readonly FilePermissions FormatMask = (FilePermissions)0xF000;

        public static bool IsRegular(this FilePermissions mode)
        {
            return (mode & FormatMask) == FilePermissions.Regular;
        }

        public static bool IsLink(this FilePermissions mode)
        {
            return (mode & FormatMask) == FilePermissions.SymbolicLink;
        }

        public static bool IsDirectory(this FilePermissions mode)
        {
            return (mode & FormatMask) == FilePermissions.Directory;
        }

        public static bool IsCharacterDevice(this FilePermissions mode)
        {
            return (mode & FormatMask) == FilePermissions.CharacterDevice;
        }

        public static bool IsBlockDevice(this FilePermissions mode)
        {
            return (mode & FormatMask) == FilePermissions.BlockSpecial;
        }

        public static bool IsFifo(this FilePermissions mode)
        {
            return (mode & FormatMask) == FilePermissions.Fifo;
        }

        public static bool IsSocket(this FilePermissions mode)
        {
            return (mode & FormatMask) == FilePermissions.Socket;
        }
    }
}
