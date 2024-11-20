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

using Google.Solutions.Ssh.Native;
using System;

namespace Google.Solutions.Ssh
{
    /// <summary>
    /// Information about an SFTP file or directory.
    /// </summary>
    public readonly struct SftpFileInfo
    {
        private readonly LIBSSH2_SFTP_ATTRIBUTES attributes;

        /// <summary>
        /// Name of file (without path).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// File attributes.
        /// </summary>
        public FilePermissions Permissions
        {
            get => this.attributes.permissions;
        }

        /// <summary>
        /// Indicates if this is a directory.
        /// </summary>
        public bool IsDirectory
        {
            get => this.Permissions.IsDirectory();
        }

        /// <summary>
        /// Owner user ID.
        /// </summary>
        public uint UserId
        {
            get => this.attributes.uid;
        }

        /// <summary>
        /// Owner group ID.
        /// </summary>
        public uint GroupId
        {
            get => this.attributes.gid;
        }

        /// <summary>
        /// Time of last access.
        /// </summary>
        public DateTime LastAccessDate
        {
            get => DateTimeOffset.FromUnixTimeSeconds(this.attributes.atime).DateTime;
        }

        /// <summary>
        /// Time of last change.
        /// </summary>
        public DateTime LastModifiedDate
        {
            get => DateTimeOffset.FromUnixTimeSeconds(this.attributes.mtime).DateTime;
        }

        /// <summary>
        /// Size of the file.
        /// </summary>
        public ulong Size
        {
            get => this.attributes.filesize;
        }

        internal SftpFileInfo(
            string name,
            LIBSSH2_SFTP_ATTRIBUTES attributes)
        {
            this.Name = name;
            this.attributes = attributes;
        }

        public SftpFileInfo(
            string name,
            FilePermissions permissions)
            : this(
                name,
                new LIBSSH2_SFTP_ATTRIBUTES()
                {
                    permissions = permissions
                })
        {
        }
    }
}
