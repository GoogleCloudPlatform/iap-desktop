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
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Google.Solutions.Mvvm.Controls.FileBrowser;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestFileBrowser
    {
        private static readonly FileType DirectoryType 
            = new FileType("Directory", false, SystemIcons.Application.ToBitmap());

        private static Mock<IFileItem> CreateDirectory()
        {
            var file = new Mock<IFileItem>();
            file.SetupGet(i => i.Name).Returns("Item");
            file.SetupGet(i => i.LastModified).Returns(DateTime.UtcNow);
            file.SetupGet(i => i.Type).Returns(DirectoryType);
            file.SetupGet(i => i.Size).Returns(1);
            file.SetupGet(i => i.IsExpanded).Returns(true);

            return file;
        }

        private static Mock<IFileSystem> CreateFileSystemWithEmptyRoot()
        {
            var root = CreateDirectory();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.Root).Returns(root.Object);
            fileSystem
                .Setup(fs => fs.ListFilesAsync(It.IsIn<IFileItem>(root.Object)))
                .ReturnsAsync(new ObservableCollection<IFileItem>());

            return fileSystem;
        }

        private static Mock<IFileSystem> CreateFileSystemWithInfinitelyNestedDirectories()
        {
            var root = CreateDirectory();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.Root).Returns(root.Object);
            fileSystem
                .Setup(fs => fs.ListFilesAsync(It.IsAny<IFileItem>()))
                .ReturnsAsync(new ObservableCollection<IFileItem>()
                {
                    CreateDirectory().Object
                });

            return fileSystem;
        }

        //---------------------------------------------------------------------
        // Bind.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBoundAlready_ThenBindThrowsException()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };

                var fileSystem = CreateFileSystemWithEmptyRoot().Object;

                browser.Bind(fileSystem);

                Assert.Throws<InvalidOperationException>(
                    () => browser.Bind(fileSystem));
            }
        }

        [Test]
        public void WhenDirectoryAdded_ThenDirectoryTreeIsUpdated()
        {
            var root = CreateDirectory();
            root.SetupGet(f => f.IsExpanded).Returns(true);

            var children = new ObservableCollection<IFileItem>();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.Root).Returns(root.Object);
            fileSystem
                .Setup(fs => fs.ListFilesAsync(It.IsIn<IFileItem>(root.Object)))
                .ReturnsAsync(children);
            fileSystem
                .Setup(fs => fs.ListFilesAsync(It.IsNotIn<IFileItem>(root.Object)))
                .ReturnsAsync(new ObservableCollection<IFileItem>());

            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };

                browser.NavigationFailed += (sender, args) => Assert.Fail();

                browser.Bind(fileSystem.Object);
                Application.DoEvents();

                Assert.AreEqual(1, browser.Directories.Nodes.Count);
                Assert.AreEqual(0, browser.Directories.Nodes[0].Nodes.Count);

                children.Add(CreateDirectory().Object);
                Application.DoEvents();

                Assert.AreEqual(1, browser.Directories.Nodes[0].Nodes.Count);
            }
        }

        [Test]
        public async Task WhenDirectoryAdded_ThenFileListIsUpdated()
        {
            var root = CreateDirectory();
            root.SetupGet(f => f.IsExpanded).Returns(true);

            var children = new ObservableCollection<IFileItem>();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.Root).Returns(root.Object);
            fileSystem
                .Setup(fs => fs.ListFilesAsync(It.IsIn<IFileItem>(root.Object)))
                .ReturnsAsync(children);
            fileSystem
                .Setup(fs => fs.ListFilesAsync(It.IsNotIn<IFileItem>(root.Object)))
                .ReturnsAsync(new ObservableCollection<IFileItem>());

            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };

                browser.NavigationFailed += (sender, args) => Assert.Fail();

                browser.Bind(fileSystem.Object);
                await browser.BrowseDirectoryAsync(null);
                Application.DoEvents();

                // Root directory is empty.
                Assert.AreEqual(0, browser.Files.Items.Count);

                children.Add(CreateDirectory().Object);
                Application.DoEvents();

                // Root directory contains 1 item..
                Assert.AreEqual(1, browser.Files.Items.Count);
            }
        }

        //---------------------------------------------------------------------
        // NavigationFailed.
        //---------------------------------------------------------------------

        [Test]
        public void WhenListingFilesFails_ThenNavigationFailedEventIsRaised()
        {
            var root = new Mock<IFileItem>();
            root.SetupGet(i => i.Name).Returns("Item");
            root.SetupGet(i => i.LastModified).Returns(DateTime.UtcNow);
            root.SetupGet(i => i.Type).Returns(DirectoryType);
            root.SetupGet(i => i.Size).Returns(1);
            root.SetupGet(i => i.IsExpanded).Returns(true);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.Root).Returns(root.Object);
            fileSystem
                .Setup(fs => fs.ListFilesAsync(It.IsIn<IFileItem>(root.Object)))
                .ThrowsAsync(new ApplicationException("TEST"));

            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };

                bool eventRaised = false;
                browser.NavigationFailed += (sender, args) =>
                {
                    Assert.AreSame(browser, sender);
                    Assert.IsInstanceOf<ApplicationException>(args.Exception.Unwrap());
                    eventRaised = true;
                };

                browser.Bind(fileSystem.Object);

                Application.DoEvents();
                Assert.IsTrue(eventRaised);
            }
        }

        //---------------------------------------------------------------------
        // CurrentDirectory.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBound_ThenCurrentDirectorySetToRoot()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };

                var fileSystem = CreateFileSystemWithEmptyRoot().Object;
                browser.Bind(fileSystem);
                Application.DoEvents();

                Assert.AreSame(fileSystem.Root, browser.CurrentDirectory);
            }
        }

        [Test]
        public async Task WhenBrowsingSubfolder_ThenCurrentDirectoryIsUpdated()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };

                var fileSystem = CreateFileSystemWithInfinitelyNestedDirectories().Object;
                browser.Bind(fileSystem);
                Application.DoEvents();

                IFileItem currentDirectory = null;
                browser.CurrentDirectoryChanged += (s, e) =>
                {
                    currentDirectory = browser.CurrentDirectory;
                };

                await browser
                    .BrowseDirectoryAsync(new[] { "Item", "Item" })
                    .ConfigureAwait(true);

                Application.DoEvents();
                Assert.IsNotNull(currentDirectory);
                Assert.AreEqual("/Item/Item/Item", browser.CurrentPath);
            }
        }
    }
}
