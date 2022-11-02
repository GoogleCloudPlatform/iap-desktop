﻿//
// Copyright 2021 Google LLC
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

using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.SshTerminal
{
    [TestFixture]
    public class TestSftpFileSystem
    {
        private SftpFileSystem CreateFileSystem(params SshSftpFileInfo[] files)
        {
            return new SftpFileSystem(
                path => Task.FromResult<IReadOnlyCollection<SshSftpFileInfo>>(
                    new ReadOnlyCollection<SshSftpFileInfo>(files)));
        }

        private SshSftpFileInfo CreateFile(string name, FilePermissions permissions)
        {
            return new SshSftpFileInfo(
                name,
                new LIBSSH2_SFTP_ATTRIBUTES()
                {
                    permissions = (uint)permissions
                });
        }

        //---------------------------------------------------------------------
        // Root.
        //---------------------------------------------------------------------

        [Test]
        public void RootIsDirectory()
        {
            using (var fs = CreateFileSystem())
            {
                var root = fs.Root;

                Assert.IsNotNull(root);
                Assert.IsFalse(root.Type.IsFile);
                Assert.IsTrue(root.IsExpanded);
                Assert.AreEqual(string.Empty, root.Name);
            }
        }

        //---------------------------------------------------------------------
        // ListFiles.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenServerReturnsUnorderedList_ThenListFilesReturnsOrderedList()
        {
            using (var fs = CreateFileSystem(
                CreateFile("dir-2", FilePermissions.Directory),
                CreateFile("file-2", FilePermissions.OwnerRead),
                CreateFile("file-1", FilePermissions.OwnerRead),
                CreateFile("dir-1", FilePermissions.Directory)))
            {
                var files = await fs
                    .ListFilesAsync(fs.Root)
                    .ConfigureAwait(false);

                CollectionAssert.AreEqual(
                    new[]
                    {
                        "dir-1",
                        "dir-2",
                        "file-1",
                        "file-2"
                    },
                    files.Select(f => f.Name));
            }
        }

        [Test]
        public async Task WhenFileIsDotLink_ThenListFilesIgnoresFile()
        {
            using (var fs = CreateFileSystem(
                CreateFile(".", FilePermissions.SymbolicLink),
                CreateFile("..", FilePermissions.SymbolicLink)))
            {
                var files = await fs
                    .ListFilesAsync(fs.Root)
                    .ConfigureAwait(false);

                CollectionAssert.IsEmpty(files.Select(f => f.Name));
            }
        }

        [Test]
        public async Task WhenFileStartsWithDot_ThenListFilesAppliesAttribute()
        {
            using (var fs = CreateFileSystem(
                CreateFile(".hidden", FilePermissions.OtherRead)))
            {
                var files = await fs
                    .ListFilesAsync(fs.Root)
                    .ConfigureAwait(false);

                Assert.AreEqual(
                    FileAttributes.Normal | FileAttributes.Hidden,
                    files.First().Attributes);
            }
        }

        [Test]
        public async Task WhenFileIsSymlink_ThenListFilesAppliesAttribute()
        {
            using (var fs = CreateFileSystem(
                CreateFile("symlink", FilePermissions.SymbolicLink)))
            {
                var files = await fs
                    .ListFilesAsync(fs.Root)
                    .ConfigureAwait(false);

                Assert.AreEqual(
                    FileAttributes.Normal | FileAttributes.ReparsePoint,
                    files.First().Attributes);
            }
        }

        [Test]
        public async Task WhenFileIsDevice_ThenListFilesAppliesAttribute(
            [Values(
                FilePermissions.Socket,
                FilePermissions.Fifo,
                FilePermissions.CharacterDevice,
                FilePermissions.BlockSpecial)] FilePermissions permissions)
        {
            using (var fs = CreateFileSystem(
                CreateFile("devide", FilePermissions.OtherRead | permissions)))
            {
                var files = await fs
                    .ListFilesAsync(fs.Root)
                    .ConfigureAwait(false);

                Assert.AreEqual(
                    FileAttributes.Normal | FileAttributes.Device,
                    files.First().Attributes);
            }
        }

        //---------------------------------------------------------------------
        // TranslateFileType.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFileIsExecutable_ThenFileTypeIsSpecial()
        {
            using (var fs = CreateFileSystem())
            {
                var regularType = fs.TranslateFileType(
                    CreateFile(
                        "file",
                        FilePermissions.OtherRead));
                var exeType = fs.TranslateFileType(
                    CreateFile(
                        "file",
                        FilePermissions.OtherRead | FilePermissions.OwnerExecute));

                Assert.IsTrue(exeType.IsFile);
                Assert.AreNotEqual(regularType.TypeName, exeType.TypeName);
            }
        }

        [Test]
        public void WheFileIsSymlink_ThenFileTypeIsIsSpecial()
        {
            using (var fs = CreateFileSystem())
            {
                var regularType = fs.TranslateFileType(
                    CreateFile(
                        "file",
                        FilePermissions.OtherRead));
                var linkType = fs.TranslateFileType(
                    CreateFile(
                        "file",
                        FilePermissions.SymbolicLink));

                Assert.IsTrue(linkType.IsFile);
                Assert.AreNotEqual(regularType.TypeName, linkType.TypeName);
            }
        }

        [Test]
        public void WheFileIsConfigFile_ThenFileTypeIsIsSpecial()
        {
            using (var fs = CreateFileSystem())
            {
                var regularType = fs.TranslateFileType(
                    CreateFile(
                        "file",
                        FilePermissions.OtherRead));
                var iniType = fs.TranslateFileType(
                    CreateFile(
                        "file.conf",
                        FilePermissions.OtherRead | FilePermissions.OwnerExecute));

                Assert.IsTrue(iniType.IsFile);
                Assert.AreNotEqual(regularType.TypeName, iniType.TypeName);
            }
        }

        [Test]
        public void WhenFileIsDirectory_ThenFileTypeIsDirectory()
        {
            using (var fs = CreateFileSystem())
            {
                var dirType = fs.TranslateFileType(
                    CreateFile(
                        "file",
                        FilePermissions.Directory));

                Assert.IsTrue(!dirType.IsFile);
            }
        }
    }
}
