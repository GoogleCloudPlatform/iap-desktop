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
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Shell;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
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
        private static readonly FileType FileType
            = new FileType("File", false, SystemIcons.Application.ToBitmap());

        private static Mock<IFileItem> CreateDirectory()
        {
            var file = new Mock<IFileItem>();
            file.SetupGet(i => i.Name).Returns("Item");
            file.SetupGet(i => i.LastModified).Returns(DateTime.UtcNow);
            file.SetupGet(i => i.Type).Returns(DirectoryType);
            file.SetupGet(i => i.Size).Returns(1);
            file.SetupGet(i => i.IsExpanded).Returns(false);

            return file;
        }

        private static Mock<IFileItem> CreateFile()
        {
            var file = new Mock<IFileItem>();
            file.SetupGet(i => i.Name).Returns("Item");
            file.SetupGet(i => i.LastModified).Returns(DateTime.UtcNow);
            file.SetupGet(i => i.Type).Returns(FileType);
            file.SetupGet(i => i.Size).Returns(1);
            file.SetupGet(i => i.IsExpanded).Returns(false);

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
        public void Bind_WhenBoundAlready_ThenBindThrowsException()
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
                form.Controls.Add(browser);

                var bindingContext = new Mock<IBindingContext>().Object;
                var fileSystem = CreateFileSystemWithEmptyRoot().Object;

                browser.Bind(fileSystem, bindingContext);

                Assert.Throws<InvalidOperationException>(
                    () => browser.Bind(fileSystem, bindingContext));
            }
        }

        [Test]
        public void Bind_WhenListingFilesFails_ThenBindRaisesNavigationFailedEvent()
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
                form.Controls.Add(browser);

                var eventRaised = false;
                browser.NavigationFailed += (sender, args) =>
                {
                    Assert.AreSame(browser, sender);
                    Assert.IsInstanceOf<ApplicationException>(args.Exception.Unwrap());
                    eventRaised = true;
                };

                browser.Bind(
                    fileSystem.Object,
                    new Mock<IBindingContext>().Object);

                Application.DoEvents();
                Assert.IsTrue(eventRaised);
            }
        }

        //---------------------------------------------------------------------
        // Bind - observation.
        //---------------------------------------------------------------------

        [Test]
        public void Bind_WhenDirectoryAdded_ThenDirectoryTreeIsUpdated()
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
                form.Controls.Add(browser);

                browser.NavigationFailed += (sender, args) => Assert.Fail();

                browser.Bind(
                    fileSystem.Object,
                    new Mock<IBindingContext>().Object);
                Application.DoEvents();

                Assert.AreEqual(1, browser.Directories.Nodes.Count);
                Assert.AreEqual(0, browser.Directories.Nodes[0].Nodes.Count);

                children.Add(CreateDirectory().Object);
                Application.DoEvents();

                Assert.AreEqual(1, browser.Directories.Nodes[0].Nodes.Count);
            }
        }

        [Test]
        public async Task Bind_WhenDirectoryAdded_ThenFileListIsUpdated()
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
                form.Controls.Add(browser);

                browser.NavigationFailed += (sender, args) => Assert.Fail();

                browser.Bind(
                    fileSystem.Object,
                    new Mock<IBindingContext>().Object);

                await browser.NavigateAsync((IEnumerable<string>)null);
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
        // CurrentDirectory.
        //---------------------------------------------------------------------

        [Test]
        public void CurrentDirectory_WhenBound_ThenCurrentDirectorySetToRoot()
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
                form.Controls.Add(browser);

                var fileSystem = CreateFileSystemWithEmptyRoot().Object;
                browser.Bind(
                    fileSystem,
                    new Mock<IBindingContext>().Object);
                Application.DoEvents();

                Assert.AreSame(fileSystem.Root, browser.CurrentDirectory);
                Assert.AreEqual(string.Empty, browser.CurrentPath);
            }
        }

        [Test]
        public async Task CurrentDirectory_WhenPathInvalid_ThenNavigateRaisesNavigationFailedEvent()
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
                form.Controls.Add(browser);

                var fileSystem = CreateFileSystemWithInfinitelyNestedDirectories().Object;
                browser.Bind(
                    fileSystem,
                    new Mock<IBindingContext>().Object);
                Application.DoEvents();

                form.Show();

                Exception exception = null;
                browser.NavigationFailed += (s, e) =>
                {
                    exception = e.Exception;
                };

                try
                {
                    await browser
                        .NavigateAsync(new[] { "Item", "Does not exist" })
                        .ConfigureAwait(true);
                    Assert.Fail("Expected exception");
                }
                catch (ArgumentException)
                { }
            }
        }

        [Test]
        public async Task CurrentDirectory_WhenBrowsedToSubfolder_ThenCurrentDirectoryAndPathIsUpdated()
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
                form.Controls.Add(browser);

                var fileSystem = CreateFileSystemWithInfinitelyNestedDirectories().Object;
                browser.Bind(
                    fileSystem,
                    new Mock<IBindingContext>().Object);
                Application.DoEvents();

                IFileItem currentDirectory = null;
                browser.CurrentDirectoryChanged += (s, e) =>
                {
                    currentDirectory = browser.CurrentDirectory;
                };

                form.Show();

                await browser
                    .NavigateAsync(new[] { "Item", "Item" })
                    .ConfigureAwait(true);

                Application.DoEvents();
                Assert.IsNotNull(currentDirectory);
                Assert.AreEqual("Item/Item", browser.CurrentPath);
            }
        }

        [Test]
        public async Task CurrentDirectory_WhenNavigatedToSubfolder_ThenNavigateUpGoesBackToRoot()
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
                form.Controls.Add(browser);

                var fileSystem = CreateFileSystemWithInfinitelyNestedDirectories().Object;
                browser.Bind(
                    fileSystem,
                    new Mock<IBindingContext>().Object);
                Application.DoEvents();

                form.Show();

                await browser
                    .NavigateAsync(new[] { "Item", "Item" })
                    .ConfigureAwait(true);

                for (var i = 0; i < 5; i++)
                {
                    await browser
                        .NavigateUpAsync()
                        .ConfigureAwait(false);
                }

                Application.DoEvents();
                Assert.AreEqual(string.Empty, browser.CurrentPath);
            }
        }

        [Test]
        public async Task CurrentDirectory_WhenNavigatedToSubfolder_ThenNavigateNullGoesBackToRoot()
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
                form.Controls.Add(browser);

                var fileSystem = CreateFileSystemWithInfinitelyNestedDirectories().Object;
                browser.Bind(
                    fileSystem,
                    new Mock<IBindingContext>().Object);
                Application.DoEvents();

                form.Show();

                await browser
                    .NavigateAsync(new[] { "Item", "Item" })
                    .ConfigureAwait(true);

                await browser
                    .NavigateAsync(null)
                    .ConfigureAwait(true);

                Application.DoEvents();
                Assert.AreEqual(string.Empty, browser.CurrentPath);
            }
        }

        //---------------------------------------------------------------------
        // SelectedFiles.
        //---------------------------------------------------------------------

        [Test]
        public void SelectedFiles_WhenFilesSelectedOneByOne_ThenSelectedFileIsSet()
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
                form.Controls.Add(browser);

                var root = CreateDirectory();

                var fileSystem = new Mock<IFileSystem>();
                fileSystem.SetupGet(fs => fs.Root).Returns(root.Object);
                fileSystem
                    .Setup(fs => fs.ListFilesAsync(It.IsIn(root.Object)))
                    .ReturnsAsync(new ObservableCollection<IFileItem>()
                    {
                        CreateFile().Object,
                        CreateFile().Object,
                        CreateFile().Object
                    });

                browser.Bind(
                    fileSystem.Object,
                    new Mock<IBindingContext>().Object);
                Application.DoEvents();

                form.Show();

                var selectionEvents = 0;
                browser.SelectedFilesChanged += (s, a) => selectionEvents++;

                foreach (var item in browser.Files.Items.Cast<ListViewItem>())
                {
                    item.Selected = true;
                }

                Assert.AreEqual(3, browser.SelectedFiles.Count());
                Assert.AreEqual(3, selectionEvents);
            }
        }

        [Test]
        public void SelectedFiles_WhenFilesSelected_ThenSelectedFileIsSet()
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
                form.Controls.Add(browser);

                var root = CreateDirectory();
                var files = new ObservableCollection<IFileItem>()
                {
                    CreateFile().Object,
                    CreateFile().Object,
                    CreateFile().Object
                };
                var fileSystem = new Mock<IFileSystem>();
                fileSystem.SetupGet(fs => fs.Root).Returns(root.Object);
                fileSystem
                    .Setup(fs => fs.ListFilesAsync(It.IsIn(root.Object)))
                    .ReturnsAsync(files);

                browser.Bind(
                    fileSystem.Object,
                    new Mock<IBindingContext>().Object);
                Application.DoEvents();

                form.Show();

                var selectionEvents = 0;
                browser.SelectedFilesChanged += (s, a) => selectionEvents++;

                browser.SelectedFiles = new[] { files[0], files[1] };

                Assert.AreEqual(2, browser.SelectedFiles.Count());
                Assert.AreEqual(2, selectionEvents);
            }
        }
    }
}
