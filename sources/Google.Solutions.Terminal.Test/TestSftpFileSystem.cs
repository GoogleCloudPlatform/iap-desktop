//
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

using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.Terminal.Test
{
    [TestFixture]
    public class TestSftpFileSystem
    {
        private static Libssh2SftpFileInfo CreateFile(string name, FilePermissions permissions)
        {
            return new Libssh2SftpFileInfo(name, permissions);
        }

        //---------------------------------------------------------------------
        // Root.
        //---------------------------------------------------------------------

        [Test]
        public void Root_IsDirectory()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
            {
                var root = fs.Root;

                Assert.IsNotNull(root);
                Assert.IsFalse(root.Type.IsFile);
                Assert.IsTrue(root.IsExpanded);
                Assert.AreEqual("/", root.Name);
            }
        }

        //---------------------------------------------------------------------
        // ListFiles.
        //---------------------------------------------------------------------

        [Test]
        public async Task ListFiles_WhenServerReturnsUnorderedList_ThenListFilesReturnsOrderedList()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            sftpChannel
                .Setup(c => c.ListFilesAsync(It.IsAny<string>()))
                .ReturnsAsync(new[]
                {
                    CreateFile("dir-2", FilePermissions.Directory),
                    CreateFile("file-2", FilePermissions.OwnerRead),
                    CreateFile("file-1", FilePermissions.OwnerRead),
                    CreateFile("dir-1", FilePermissions.Directory)
                });

            using (var fs = new SftpFileSystem(sftpChannel.Object))
            {
                var files = await fs
                    .ListFilesAsync(fs.Root)
                    .ConfigureAwait(false);

                var expected = new[]
                {
                    "dir-1",
                    "dir-2",
                    "file-1",
                    "file-2"
                };

                CollectionAssert.AreEqual(
                    expected,
                    files.Select(f => f.Name));
            }
        }

        [Test]
        public async Task ListFiles_WhenFileIsDotLink_ThenListFilesIgnoresFile()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            sftpChannel
                .Setup(c => c.ListFilesAsync(It.IsAny<string>()))
                .ReturnsAsync(new[]
                {
                    CreateFile(".", FilePermissions.SymbolicLink),
                    CreateFile("..", FilePermissions.SymbolicLink)
                });

            using (var fs = new SftpFileSystem(sftpChannel.Object))
            {
                var files = await fs
                    .ListFilesAsync(fs.Root)
                    .ConfigureAwait(false);

                CollectionAssert.IsEmpty(files.Select(f => f.Name));
            }
        }

        [Test]
        public async Task ListFiles_WhenFileStartsWithDot_ThenListFilesAppliesAttribute()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            sftpChannel
                .Setup(c => c.ListFilesAsync(It.IsAny<string>()))
                .ReturnsAsync(new[]
                {
                    CreateFile(".hidden", FilePermissions.OtherRead)
                });

            using (var fs = new SftpFileSystem(sftpChannel.Object))
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
        public async Task ListFiles_WhenFileIsSymlink_ThenListFilesAppliesAttribute()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            sftpChannel
                .Setup(c => c.ListFilesAsync(It.IsAny<string>()))
                .ReturnsAsync(new[]
                {
                    CreateFile("symlink", FilePermissions.SymbolicLink)
                });

            using (var fs = new SftpFileSystem(sftpChannel.Object))
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
        public async Task ListFiles_WhenFileIsDevice_ThenListFilesAppliesAttribute(
            [Values(
                FilePermissions.Socket,
                FilePermissions.Fifo,
                FilePermissions.CharacterDevice,
                FilePermissions.BlockSpecial)] FilePermissions permissions)
        {
            var sftpChannel = new Mock<ISftpChannel>();
            sftpChannel
                .Setup(c => c.ListFilesAsync(It.IsAny<string>()))
                .ReturnsAsync(new[]
                {
                    CreateFile("device", FilePermissions.OtherRead | permissions)
                });

            using (var fs = new SftpFileSystem(sftpChannel.Object))
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
        public void TranslateFileType_WhenFileIsExecutable_ThenFileTypeIsSpecial()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
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
        public void TranslateFileType_WhenFileIsSymlink_ThenFileTypeIsIsSpecial()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
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
        public void TranslateFileType_WhenFileIsConfigFile_ThenFileTypeIsSpecial()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
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
        public void TranslateFileType_WhenFileIsDirectory_ThenFileTypeIsDirectory()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
            {
                var dirType = fs.TranslateFileType(
                    CreateFile(
                        "file",
                        FilePermissions.Directory));

                Assert.IsTrue(!dirType.IsFile);
            }
        }

        [Test]
        public void TranslateFileType_WhenFileNameContainsInvalidCharacters_ThenFileTypeIsPlain(
            [Values("file?", "<file", "COM1", "file.")] string fileName)
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
            {
                var regularType = fs.TranslateFileType(
                    CreateFile(
                        fileName,
                        FilePermissions.OtherRead));
                var iniType = fs.TranslateFileType(
                    CreateFile(
                        "file",
                        FilePermissions.OtherRead | FilePermissions.OwnerExecute));

                Assert.IsTrue(iniType.IsFile);
                Assert.AreNotEqual(regularType.TypeName, iniType.TypeName);
            }
        }
    }
}
