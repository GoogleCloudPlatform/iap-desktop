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
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Platform.Interop;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#pragma warning disable CS0067 // The event ... is never used

namespace Google.Solutions.Terminal
{
    /// <summary>
    /// Implements IFileSystem on a SFTP channel.
    /// </summary>
    internal sealed class SftpFileSystem : IFileSystem, IDisposable
    {
        private readonly FileTypeCache fileTypeCache;
        private readonly ISftpChannel channel;

        private static readonly Regex configFileNamePattern = new Regex("co?ni?f(ig)?$");

        /// <summary>
        /// Default permissions to apply to new files.
        /// </summary>
        public FilePermissions DefaultFilePermissions { get; set; } =
            FilePermissions.OwnerRead | FilePermissions.OwnerWrite;

        private static readonly DateTime Epoch =
            DateTimeOffset.FromUnixTimeSeconds(0).DateTime;

        /// <summary>
        /// The file system root (/) on the server.
        /// </summary>
        internal IFileItem Drive { get; }

        /// <summary>
        /// The user's home directory.
        /// </summary>
        internal IFileItem Home { get; }

        /// <summary>
        /// Map SFTP file attributes to a file type.
        /// </summary>
        internal FileType MapFileType(SftpFileInfo sftpFile)
        {
            if (sftpFile.IsDirectory)
            {
                return this.fileTypeCache.Lookup(
                    sftpFile.Name,
                    FileAttributes.Directory,
                    FileType.IconFlags.None);
            }
            else if (sftpFile.Permissions.HasFlag(FilePermissions.SymbolicLink))
            {
                //
                // Treat like an LNK file.
                //
                // NB. We can't tell whether the symlink points to a directory
                // or not, that would require resolving the link. So we treat
                // all symlinks like files.
                //
                return this.fileTypeCache.Lookup(
                    ".lnk",
                    FileAttributes.Normal,
                    FileType.IconFlags.None);
            }
            else if (sftpFile.Permissions.HasFlag(FilePermissions.OwnerExecute) ||
                     sftpFile.Permissions.HasFlag(FilePermissions.GroupExecute) ||
                     sftpFile.Permissions.HasFlag(FilePermissions.OtherExecute))
            {
                //
                // Treat like an EXE file.
                //
                return this.fileTypeCache.Lookup(
                    ".exe",
                    FileAttributes.Normal,
                    FileType.IconFlags.None);
            }
            else if (configFileNamePattern.IsMatch(sftpFile.Name))
            {
                //
                // Treat like an INI file.
                //
                return this.fileTypeCache.Lookup(
                    ".ini",
                    FileAttributes.Normal,
                    FileType.IconFlags.None);
            }
            else
            {
                //
                // Lookup file type using Shell.
                //
                return this.fileTypeCache.Lookup(
                    Win32Filename.IsValidFilename(sftpFile.Name) ? sftpFile.Name : "file",
                    FileAttributes.Normal,
                    FileType.IconFlags.None);
            }
        }

        /// <summary>
        /// Translate SFTP file attributes to Win32 attributes.
        /// </summary>
        internal static FileAttributes MapFileAttributes(
            string name,
            bool isDirectory,
            FilePermissions permissions)
        {
            var attributes = (FileAttributes)0;

            if (isDirectory)
            {
                attributes |= FileAttributes.Directory;
            }

            if (name.StartsWith("."))
            {
                attributes |= FileAttributes.Hidden;
            }

            if (permissions.IsLink())
            {
                attributes |= FileAttributes.ReparsePoint;
            }

            if (permissions.IsSocket() ||
                permissions.IsFifo() ||
                permissions.IsCharacterDevice() ||
                permissions.IsBlockDevice())
            {
                attributes |= FileAttributes.Device;
            }

            return attributes == 0 ? FileAttributes.Normal : attributes;
        }

        public SftpFileSystem(ISftpChannel channel)
        {
            this.channel = channel.ExpectNotNull(nameof(channel));
            this.fileTypeCache = new FileTypeCache();

            //
            // Initialize pseudo-directories.
            //
            this.Root = new FileItem(
                null,
                new FileType(
                    "Server",
                    false,
                    StockIcons.GetIcon(
                        StockIcons.IconId.Server, 
                        StockIcons.IconSize.Small)),
                "Server",
                FileAttributes.Directory | FileAttributes.ReadOnly,
                Epoch,
                0)
            {
                IsExpanded = true
            };
            this.Home = new FileItem(
                (FileItem)this.Root,
                new FileType(
                    "Home",
                    false,
                    StockIcons.GetIcon(
                        StockIcons.IconId.Folder, 
                        StockIcons.IconSize.Small)),
                "Home",
                ".",
                FileAttributes.Directory,
                Epoch,
                0);
            this.Drive = new FileItem(
                (FileItem)this.Root,
                new FileType(
                    "Drive",
                    false,
                    StockIcons.GetIcon(
                        StockIcons.IconId.DriveFixed,
                        StockIcons.IconSize.Small)),
                "File system root",
                "/.",
                FileAttributes.Directory,
                Epoch,
                0);
        }

        //---------------------------------------------------------------------
        // IFileSystem.
        //---------------------------------------------------------------------

        /// <summary>
        /// The "Server" node, root of the virtual file system.
        /// </summary>
        public IFileItem Root { get; }

        public async Task<ObservableCollection<IFileItem>> ListFilesAsync(
            IFileItem directory)
        {
            directory.ExpectNotNull(nameof(directory));
            Debug.Assert(!directory.Type.IsFile);

            if (directory == this.Root)
            {
                //
                // Return a pseudo-directory listing.
                //
                return new ObservableCollection<IFileItem>()
                {
                    this.Home,
                    this.Drive,
                };
            }
            else
            {
                var sftpFiles = await this.channel
                    .ListFilesAsync(directory.Path)
                    .ConfigureAwait(false);

                //
                // NB. SFTP returns files/directories in arbitrary order.
                //

                var filteredSftpFiles = sftpFiles
                    .Where(f => f.Name != "." && f.Name != "..")
                    .OrderBy(f => !f.IsDirectory).ThenBy(f => f.Name)
                    .Select(f => new FileItem(
                        (FileItem)directory,
                        MapFileType(f),
                        f.Name,
                        MapFileAttributes(f.Name, f.IsDirectory, f.Permissions),
                        f.LastModifiedDate,
                        f.Size))
                    .ToList();

                return new ObservableCollection<IFileItem>(filteredSftpFiles);
            }
        }

        public Task<Stream> OpenFileAsync(
            IFileItem file,
            FileAccess access)
        {
            if (file == this.Root)
            {
                throw new UnauthorizedAccessException();
            }

            Precondition.Expect(file.Type.IsFile, $"{file.Name} is not a file");

            return this.channel.CreateFileAsync(
                file.Path,
                FileMode.Open,
                access,
                FilePermissions.None);
        }

        public Task<Stream> OpenFileAsync(
            IFileItem directory,
            string name,
            FileMode mode,
            FileAccess access)
        {
            if (directory == this.Root)
            {
                throw new UnauthorizedAccessException();
            }

            Precondition.Expect(!directory.Type.IsFile, $"{directory.Name} is not a directory");
            Precondition.Expect(!name.Contains("/"), "Name must not be a path");

            return this.channel.CreateFileAsync(
                $"{directory.Path}/{name}",
                mode,
                access,
                this.DefaultFilePermissions);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.fileTypeCache.Dispose();
            this.channel?.Dispose();
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class FileItem : IFileItem
        {
            private readonly FileItem? parent;
            public event PropertyChangedEventHandler? PropertyChanged;

            internal FileItem(
                FileItem? parent,
                FileType type,
                string name,
                string path,
                FileAttributes attributes,
                DateTime lastModified,
                ulong size)
            {
                this.parent = parent;
                this.Type = type;
                this.Name = name;
                this.Path = path;
                this.Attributes = attributes;
                this.LastModified = lastModified;
                this.Size = size;
            }

            internal FileItem(
                FileItem? parent,
                FileType type,
                string name,
                FileAttributes attributes,
                DateTime lastModified,
                ulong size)
                : this(
                      parent,
                      type,
                      name,
                      parent != null
                        ? $"{parent.Path}/{name}"
                        : name,
                      attributes,
                      lastModified, size) 
            { }

            public FileType Type { get; }

            public string Name { get; }

            public FileAttributes Attributes { get; }

            public DateTime LastModified { get; }

            public ulong Size { get; }

            public string Path { get; }

            public bool IsExpanded { get; set; }
        }
    }
}
