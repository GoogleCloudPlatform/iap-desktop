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

using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Ssh;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.Terminal.Test
{
    [TestFixture]
    public class TestSftpFileSystem
    {
        private static SftpFileInfo CreateFile(string name, FilePermissions permissions)
        {
            return new SftpFileInfo(name, permissions);
        }

        //---------------------------------------------------------------------
        // Root.
        //---------------------------------------------------------------------

        [Test]
        public void Root()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
            {
                var root = fs.Root;

                Assert.IsNotNull(root);
                Assert.IsFalse(root.Type.IsFile);
                Assert.IsTrue(root.IsExpanded);
                Assert.AreEqual("Server", root.Name);
                Assert.AreEqual(string.Empty, root.Access);
            }
        }

        //---------------------------------------------------------------------
        // Drive.
        //---------------------------------------------------------------------

        [Test]
        public void Drive()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
            {
                var drive = fs.Drive;

                Assert.IsNotNull(drive);
                Assert.IsFalse(drive.Type.IsFile);
                Assert.IsFalse(drive.IsExpanded);
                Assert.AreEqual("File system root", drive.Name);
                Assert.AreEqual("/.", drive.Path);
                Assert.AreEqual(string.Empty, drive.Access);
            }
        }

        //---------------------------------------------------------------------
        // Home.
        //---------------------------------------------------------------------

        [Test]
        public void Home()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
            {
                var home = fs.Home;

                Assert.IsNotNull(home);
                Assert.IsFalse(home.Type.IsFile);
                Assert.IsFalse(home.IsExpanded);
                Assert.AreEqual("Home", home.Name);
                Assert.AreEqual(".", home.Path);
                Assert.AreEqual(string.Empty, home.Access);
            }
        }

        //---------------------------------------------------------------------
        // ListFiles.
        //---------------------------------------------------------------------

        [Test]
        public async Task ListFiles_ReturnsOrderedList()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            sftpChannel
                .Setup(c => c.ListFilesAsync("/."))
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
                    .ListFilesAsync(fs.Drive)
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
        public async Task ListFiles_IgnoresDotLinks()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            sftpChannel
                .Setup(c => c.ListFilesAsync("/."))
                .ReturnsAsync(new[]
                {
                    CreateFile(".", FilePermissions.SymbolicLink),
                    CreateFile("..", FilePermissions.SymbolicLink)
                });

            using (var fs = new SftpFileSystem(sftpChannel.Object))
            {
                var files = await fs
                    .ListFilesAsync(fs.Drive)
                    .ConfigureAwait(false);

                CollectionAssert.IsEmpty(files.Select(f => f.Name));
            }
        }

        [Test]
        public async Task ListFiles_WhenListingRoot_ReturnsPseudoDirectories()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            using (var fs = new SftpFileSystem(sftpChannel.Object))
            {
                var files = await fs
                    .ListFilesAsync(fs.Root)
                    .ConfigureAwait(false);

                CollectionAssert.AreEqual(
                    new[] { fs.Home, fs.Drive },
                    files);
            }
        }

        [Test]
        public async Task ListFiles_MapsPermissions()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            sftpChannel
                .Setup(c => c.ListFilesAsync("/."))
                .ReturnsAsync(new[]
                {
                    CreateFile(
                        "dir",
                        FilePermissions.Directory |
                            FilePermissions.OwnerRead |
                            FilePermissions.OwnerExecute |
                            FilePermissions.OtherRead)
                });

            using (var fs = new SftpFileSystem(sftpChannel.Object))
            {
                var files = await fs
                    .ListFilesAsync(fs.Drive)
                    .ConfigureAwait(false);

                var dir = files.First();
                Assert.AreEqual("dir", dir.Name);
                Assert.IsFalse(dir.Type.IsFile);
                Assert.IsTrue(dir.Attributes.HasFlag(FileAttributes.Directory));
                Assert.AreEqual("dr-x---r--", dir.Access);
            }
        }

        //---------------------------------------------------------------------
        // MapFileType.
        //---------------------------------------------------------------------

        [Test]
        public void MapFileType_WhenFileIsExecutable_ThenFileTypeIsSpecial()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
            {
                var regularType = fs.MapFileType(
                    CreateFile(
                        "file",
                        FilePermissions.OtherRead));
                var exeType = fs.MapFileType(
                    CreateFile(
                        "file",
                        FilePermissions.OtherRead | FilePermissions.OwnerExecute));

                Assert.IsTrue(exeType.IsFile);
                Assert.AreNotEqual(regularType.TypeName, exeType.TypeName);
            }
        }

        [Test]
        public void MapFileType_WhenFileIsSymlink_ThenFileTypeIsIsSpecial()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
            {
                var regularType = fs.MapFileType(
                    CreateFile(
                        "file",
                        FilePermissions.OtherRead));
                var linkType = fs.MapFileType(
                    CreateFile(
                        "file",
                        FilePermissions.SymbolicLink));

                Assert.IsTrue(linkType.IsFile);
                Assert.AreNotEqual(regularType.TypeName, linkType.TypeName);
            }
        }

        [Test]
        public void MapFileType_WhenFileIsConfigFile_ThenFileTypeIsSpecial()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
            {
                var regularType = fs.MapFileType(
                    CreateFile(
                        "file",
                        FilePermissions.OtherRead));
                var iniType = fs.MapFileType(
                    CreateFile(
                        "file.conf",
                        FilePermissions.OtherRead | FilePermissions.OwnerExecute));

                Assert.IsTrue(iniType.IsFile);
                Assert.AreNotEqual(regularType.TypeName, iniType.TypeName);
            }
        }

        [Test]
        public void MapFileType_WhenFileIsDirectory_ThenFileTypeIsDirectory()
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
            {
                var dirType = fs.MapFileType(
                    CreateFile(
                        "file",
                        FilePermissions.Directory));

                Assert.IsTrue(!dirType.IsFile);
            }
        }

        [Test]
        public void MapFileType_WhenFileNameContainsInvalidCharacters_ThenFileTypeIsPlain(
            [Values("file?", "<file", "COM1", "file.")] string fileName)
        {
            using (var fs = new SftpFileSystem(new Mock<ISftpChannel>().Object))
            {
                var regularType = fs.MapFileType(
                    CreateFile(
                        fileName,
                        FilePermissions.OtherRead));
                var iniType = fs.MapFileType(
                    CreateFile(
                        "file",
                        FilePermissions.OtherRead | FilePermissions.OwnerExecute));

                Assert.IsTrue(iniType.IsFile);
                Assert.AreNotEqual(regularType.TypeName, iniType.TypeName);
            }
        }

        //---------------------------------------------------------------------
        // MapFileAttributes.
        //---------------------------------------------------------------------

        [Test]
        public void MapFileAttributes_WhenNormalDirectory()
        {
            var attributes = SftpFileSystem.MapFileAttributes(
                "/",
                true,
                FilePermissions.OwnerRead);
            Assert.AreEqual(
                FileAttributes.Directory,
                attributes);
        }

        [Test]
        public void MapFileAttributes_WhenHiddenDirectory(
            [Values(".hidden", "..hidden")] string name)
        {
            var attributes = SftpFileSystem.MapFileAttributes(
                name,
                true,
                FilePermissions.OwnerRead);
            Assert.AreEqual(
                FileAttributes.Directory | FileAttributes.Hidden,
                attributes);
        }

        [Test]
        public void MapFileAttributes_WhenSymlink()
        {
            var attributes = SftpFileSystem.MapFileAttributes(
                "link",
                false,
                FilePermissions.OwnerRead | FilePermissions.SymbolicLink);
            Assert.AreEqual(
                FileAttributes.ReparsePoint,
                attributes);
        }

        [Test]
        public void MapFileAttributes_WhenDevice(
            [Values(
            FilePermissions.BlockSpecial,
            FilePermissions.CharacterDevice,
            FilePermissions.Fifo,
            FilePermissions.Socket)] FilePermissions permissions)
        {
            var attributes = SftpFileSystem.MapFileAttributes(
                "device",
                false,
                FilePermissions.OwnerRead | permissions);
            Assert.AreEqual(
                FileAttributes.Device,
                attributes);
        }

        [Test]
        public void MapFileAttributes_WhenFile()
        {
            var attributes = SftpFileSystem.MapFileAttributes(
                "file",
                false,
                FilePermissions.OwnerRead);
            Assert.AreEqual(
                FileAttributes.Normal,
                attributes);
        }

        //---------------------------------------------------------------------
        // OpenFile - by item.
        //---------------------------------------------------------------------

        [Test]
        public void OpenFile_ByFileItem_WhenRoot()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            using (var fs = new SftpFileSystem(sftpChannel.Object))
            {
                ExceptionAssert.ThrowsAggregateException<UnauthorizedAccessException>(
                    () => fs.OpenFileAsync(fs.Root, FileAccess.ReadWrite).Wait());
            }
        }

        [Test]
        public async Task OpenFile_ByFileItem_OpensFile()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            using (var fs = new SftpFileSystem(sftpChannel.Object))
            {
                var file = new Mock<IFileItem>();
                file.SetupGet(f => f.Path).Returns("file.txt");
                file.SetupGet(f => f.Type).Returns(new FileType("file", true, null!));

                await fs
                    .OpenFileAsync(file.Object, FileAccess.ReadWrite)
                    .ConfigureAwait(false);
            }

            sftpChannel.Verify(
                c => c.CreateFileAsync(
                    "file.txt",
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FilePermissions.None),
                Times.Once);
        }

        //---------------------------------------------------------------------
        // OpenFile - by name.
        //---------------------------------------------------------------------

        [Test]
        public void OpenFile_ByName_WhenInRoot()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            using (var fs = new SftpFileSystem(sftpChannel.Object))
            {
                ExceptionAssert.ThrowsAggregateException<UnauthorizedAccessException>(
                    () => fs.OpenFileAsync(
                        fs.Root,
                        "file.txt",
                        FileMode.CreateNew,
                        FileAccess.ReadWrite).Wait());
            }
        }

        [Test]
        public async Task OpenFile_ByName_CreatesFile()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            using (var fs = new SftpFileSystem(sftpChannel.Object))
            {
                await fs
                    .OpenFileAsync(
                        fs.Drive,
                        "file.txt",
                        FileMode.CreateNew,
                        FileAccess.ReadWrite)
                    .ConfigureAwait(false);
            }

            sftpChannel.Verify(
                c => c.CreateFileAsync(
                    "/./file.txt",
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FilePermissions.OwnerWrite | FilePermissions.OwnerRead),
                Times.Once);
        }

        [Test]
        public async Task OpenFile_ByName_UsesDefaultPermission()
        {
            var sftpChannel = new Mock<ISftpChannel>();
            using (var fs = new SftpFileSystem(sftpChannel.Object))
            {
                fs.DefaultFilePermissions = FilePermissions.OwnerExecute;
                await fs
                    .OpenFileAsync(
                        fs.Drive,
                        "file.txt",
                        FileMode.CreateNew,
                        FileAccess.ReadWrite)
                    .ConfigureAwait(false);
            }

            sftpChannel.Verify(
                c => c.CreateFileAsync(
                    "/./file.txt",
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FilePermissions.OwnerExecute),
                Times.Once);
        }
    }
}
