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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        private static Mock<IFileItem> CreateDirectory(string name = "Item")
        {
            var file = new Mock<IFileItem>();
            file.SetupGet(i => i.Name).Returns(name);
            file.SetupGet(i => i.LastModified).Returns(DateTime.UtcNow);
            file.SetupGet(i => i.Type).Returns(DirectoryType);
            file.SetupGet(i => i.Size).Returns(1);
            file.SetupGet(i => i.IsExpanded).Returns(false);
            file.SetupGet(i => i.Type).Returns(FileType.Lookup(
                ".",
                System.IO.FileAttributes.Directory,
                FileType.IconFlags.None));

            return file;
        }

        private static Mock<IFileItem> CreateFile(string name = "Item")
        {
            var file = new Mock<IFileItem>();
            file.SetupGet(i => i.Name).Returns(name);
            file.SetupGet(i => i.LastModified).Returns(DateTime.UtcNow);
            file.SetupGet(i => i.Type).Returns(FileType);
            file.SetupGet(i => i.Size).Returns(1);
            file.SetupGet(i => i.IsExpanded).Returns(false);
            file.SetupGet(i => i.Type).Returns(FileType.Lookup(
                ".txt",
                System.IO.FileAttributes.Normal,
                FileType.IconFlags.None));

            return file;
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

        private static Mock<IFileSystem> CreateFileSystem(
            params IFileItem[] files)
        {
            var root = CreateDirectory();
            var fileCollection = new ObservableCollection<IFileItem>();

            foreach (var file in files)
            {
                fileCollection.Add(file);
            }

            var fileSystem = new Mock<IFileSystem>();
            fileSystem
                .SetupGet(fs => fs.Root)
                .Returns(root.Object);
            fileSystem
                .Setup(fs => fs.ListFilesAsync(It.IsIn(root.Object)))
                .ReturnsAsync(fileCollection);

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
                var fileSystem = CreateFileSystem().Object;

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

                Assert.That(browser.Directories.Nodes.Count, Is.EqualTo(1));
                Assert.That(browser.Directories.Nodes[0].Nodes.Count, Is.EqualTo(0));

                children.Add(CreateDirectory().Object);
                Application.DoEvents();

                Assert.That(browser.Directories.Nodes[0].Nodes.Count, Is.EqualTo(1));
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

                await browser.NavigateAsync(null);
                Application.DoEvents();

                // Root directory is empty.
                Assert.That(browser.Files.Items.Count, Is.EqualTo(0));

                children.Add(CreateDirectory().Object);
                Application.DoEvents();

                // Root directory contains 1 item..
                Assert.That(browser.Files.Items.Count, Is.EqualTo(1));
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
                var fileSystem = CreateFileSystem().Object;

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(browser);

                browser.Bind(
                    fileSystem,
                    new Mock<IBindingContext>().Object);
                Application.DoEvents();

                Assert.AreSame(fileSystem.Root, browser.CurrentDirectory);
                Assert.That(browser.CurrentPath, Is.EqualTo(string.Empty));
            }
        }

        //---------------------------------------------------------------------
        // CurrentDirectory.
        //---------------------------------------------------------------------

        [Test]
        public async Task Navigate_WhenPathInvalid_ThenRaisesEvent()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var fileSystem = CreateFileSystemWithInfinitelyNestedDirectories().Object;

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(browser);

                browser.Bind(
                    fileSystem,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                Exception? exception = null;
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
        public async Task Navigate_WhenPathIsNull_ThenNavigatesToRoot()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var fileSystem = CreateFileSystemWithInfinitelyNestedDirectories().Object;

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(browser);

                browser.Bind(
                    fileSystem,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                await browser
                    .NavigateAsync(new[] { "Item", "Item" })
                    .ConfigureAwait(true);

                await browser
                    .NavigateAsync(null)
                    .ConfigureAwait(true);

                Application.DoEvents();
                Assert.That(browser.CurrentPath, Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public async Task Navigate()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var fileSystem = CreateFileSystemWithInfinitelyNestedDirectories().Object;

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(browser);
                browser.Bind(
                    fileSystem,
                    new Mock<IBindingContext>().Object);

                IFileItem? currentDirectory = null;
                browser.CurrentDirectoryChanged += (s, e) =>
                {
                    currentDirectory = browser.CurrentDirectory;
                };

                form.Show();
                Application.DoEvents();

                await browser
                    .NavigateAsync(new[] { "Item", "Item" })
                    .ConfigureAwait(true);

                Application.DoEvents();
                Assert.IsNotNull(currentDirectory);
                Assert.That(browser.CurrentPath, Is.EqualTo("Item/Item"));
            }
        }

        //---------------------------------------------------------------------
        // NavigateUp.
        //---------------------------------------------------------------------

        [Test]
        public async Task NavigateUp()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var fileSystem = CreateFileSystemWithInfinitelyNestedDirectories().Object;

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(browser);
                browser.Bind(
                    fileSystem,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

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
                Assert.That(browser.CurrentPath, Is.EqualTo(string.Empty));
            }
        }

        //---------------------------------------------------------------------
        // Refresh.
        //---------------------------------------------------------------------

        [Test]
        public async Task Refresh()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var fileSystem = CreateFileSystem();

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(browser);
                browser.Bind(
                    fileSystem.Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                await browser
                    .RefreshAsync()
                    .ConfigureAwait(true);

                fileSystem.Verify(
                    f => f.ListFilesAsync(It.IsAny<IFileItem>()),
                    Times.Exactly(2));
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
                var fileSystem = CreateFileSystem(
                    CreateFile().Object,
                    CreateFile().Object,
                    CreateFile().Object);

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(browser);
                browser.Bind(
                    fileSystem.Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                var selectionEvents = 0;
                browser.SelectedFilesChanged += (s, a) => selectionEvents++;

                foreach (var item in browser.Files.Items.Cast<ListViewItem>())
                {
                    item.Selected = true;
                }

                Assert.That(browser.SelectedFiles.Count(), Is.EqualTo(3));
                Assert.That(selectionEvents, Is.EqualTo(3));
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
                var files = new[]
                {
                    CreateFile().Object,
                    CreateFile().Object,
                    CreateFile().Object
                };

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(browser);
                browser.Bind(
                     CreateFileSystem(files).Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                var selectionEvents = 0;
                browser.SelectedFilesChanged += (s, a) => selectionEvents++;

                browser.SelectedFiles = new[] { files[0], files[1] };

                Assert.That(browser.SelectedFiles.Count(), Is.EqualTo(2));
                Assert.That(selectionEvents, Is.EqualTo(2));
            }
        }

        //---------------------------------------------------------------------
        // CopySelectedFiles.
        //---------------------------------------------------------------------

        [Test]
        public void CopySelectedFiles_WhenDirectorySelected()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var files = new[]
                {
                    CreateDirectory().Object,
                    CreateFile().Object
                };

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(browser);
                browser.Bind(
                    CreateFileSystem(files).Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                browser.SelectedFiles = new[] { files[0] };

                using (var dataObject = browser.CopySelectedFiles())
                {
                    Assert.That(dataObject.Files.Count, Is.EqualTo(0));
                }
            }
        }

        [Test]
        public void CopySelectedFiles_WhenOpenStreamFails_ThenRaisesException()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var file = CreateFile().Object;
                var fileSystem = CreateFileSystem(new[]
                {
                    file
                });

                fileSystem
                    .Setup(fs => fs.OpenFileAsync(
                        It.IsAny<IFileItem>(),
                        FileAccess.Read))
                    .Throws<UnauthorizedAccessException>();

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(browser);
                browser.Bind(
                    fileSystem.Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                browser.SelectedFiles = new[] { file };

                using (var dataObject = browser.CopySelectedFiles())
                {
                    Exception? fileCopyException = null;
                    browser.FileCopyFailed += (_, args) => fileCopyException = args.Exception;

                    var ucomDataObject = (System.Runtime.InteropServices.ComTypes.IDataObject)dataObject;
                    var formatetc = new FORMATETC()
                    {
                        tymed = TYMED.TYMED_HGLOBAL,
                        cfFormat = (short)DataFormats.GetFormat(ShellDataFormats.CFSTR_FILECONTENTS).Id,
                        dwAspect = DVASPECT.DVASPECT_CONTENT,
                        lindex = 0
                    };
                    Assert.Throws<UnauthorizedAccessException>(
                        () => ucomDataObject.GetData(ref formatetc, out var medium));

                    Assert.IsInstanceOf<UnauthorizedAccessException>(fileCopyException);
                }
            }
        }

        [Test]
        public void CopySelectedFiles()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var files = new[]
                {
                    CreateDirectory().Object,
                    CreateFile().Object,
                    CreateFile().Object
                };
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(browser);
                browser.Bind(
                    CreateFileSystem(files).Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                browser.SelectedFiles = new[] { files[0], files[1], files[2] };

                using (var dataObject = browser.CopySelectedFiles())
                {
                    Assert.That(dataObject.Files.Count, Is.EqualTo(2));
                    Assert.IsTrue(dataObject.IsAsync);
                }
            }
        }

        //---------------------------------------------------------------------
        // CanPaste.
        //---------------------------------------------------------------------

        [Test]
        public void CanPaste_WhenFormatIncompatible()
        {
            var dataObject = new DataObject();
            dataObject.SetData("Unknown", "data");

            Assert.IsFalse(FileBrowser.CanPaste(dataObject));
        }

        [Test]
        public void CanPaste_WhenDataObjectContainsNonFiles(
            [Values("__doesnotexist.txt", "\\", "COM1")] string path)
        {
            var dataObject = new DataObject();
            dataObject.SetData(
                DataFormats.FileDrop,
                new string[] { path });

            Assert.IsFalse(FileBrowser.CanPaste(dataObject));
        }

        [Test]
        public void CanPaste_WhenDataObjectContainsFile()
        {
            var dataObject = new DataObject();
            dataObject.SetData(
                DataFormats.FileDrop,
                new string[] { Path.GetTempFileName() });

            Assert.IsTrue(FileBrowser.CanPaste(dataObject));
        }

        //---------------------------------------------------------------------
        // GetPastableFiles.
        //---------------------------------------------------------------------

        [Test]
        public void GetPastableFiles_WhenFormatIncompatible()
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
                browser.Bind(
                    CreateFileSystem().Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                var dataObject = new DataObject();
                dataObject.SetData("Unknown", "data");

                Assert.IsEmpty(browser.GetPastableFiles(dataObject, false));
            }
        }

        [Test]
        public void GetPastableFiles_WhenDataObjectContainsNonFiles(
            [Values("__doesnotexist.txt", "\\", "COM1")] string path)
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
                browser.Bind(
                    CreateFileSystem().Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                var dataObject = new DataObject();
                dataObject.SetData(
                    DataFormats.FileDrop,
                    new string[] { path });

                Assert.IsEmpty(browser.GetPastableFiles(dataObject, false));
            }
        }

        [Test]
        public void GetPastableFiles_WhenDataObjectContainsFileThatConflictsWithExistingDirectory(
            [Values(DialogResult.Cancel, DialogResult.Ignore)] DialogResult dialogResult)
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var sourceFile = new FileInfo(Path.GetTempFileName());
                var taskDialog = new Mock<ITaskDialog>();
                taskDialog
                    .Setup(d => d.ShowDialog(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<TaskDialogParameters>()))
                    .Returns(dialogResult);

                var existingDirectory = CreateDirectory(sourceFile.Name);
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill,
                    TaskDialog = taskDialog.Object
                };
                form.Controls.Add(browser);
                browser.Bind(
                    CreateFileSystem(existingDirectory.Object).Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                var dataObject = new DataObject();
                dataObject.SetData(
                    DataFormats.FileDrop,
                    new string[] { sourceFile.FullName });

                Assert.IsEmpty(browser.GetPastableFiles(dataObject, true));

                taskDialog.Verify(
                    d => d.ShowDialog(browser, It.IsAny<TaskDialogParameters>()),
                    Times.Once);
            }
        }

        [Test]
        public void GetPastableFiles_WhenDataObjectContainsFileToOverwriteAndUserConfirms()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var sourceFile = new FileInfo(Path.GetTempFileName());
                var taskDialog = new Mock<ITaskDialog>();
                taskDialog
                    .Setup(d => d.ShowDialog(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<TaskDialogParameters>()))
                    .Returns(DialogResult.OK);

                var existingFile = CreateFile(sourceFile.Name);
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill,
                    TaskDialog = taskDialog.Object
                };
                form.Controls.Add(browser);
                browser.Bind(
                    CreateFileSystem(existingFile.Object).Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                var dataObject = new DataObject();
                dataObject.SetData(
                    DataFormats.FileDrop,
                    new string[] { sourceFile.FullName });

                Assert.That(
                    browser
                        .GetPastableFiles(dataObject, true)
                        .FirstOrDefault()?
                        .Name, Is.EqualTo(sourceFile.Name));

                taskDialog.Verify(
                    d => d.ShowDialog(browser, It.IsAny<TaskDialogParameters>()),
                    Times.Once);
            }
        }

        [Test]
        public void GetPastableFiles_WhenDataObjectContainsFileToOverwriteAndUserDeclines()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var sourceFile = new FileInfo(Path.GetTempFileName());
                var taskDialog = new Mock<ITaskDialog>();
                taskDialog
                    .Setup(d => d.ShowDialog(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<TaskDialogParameters>()))
                    .Returns(DialogResult.Ignore);

                var existingFile = CreateFile(sourceFile.Name);
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill,
                    TaskDialog = taskDialog.Object
                };
                form.Controls.Add(browser);
                browser.Bind(
                    CreateFileSystem(existingFile.Object).Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                var dataObject = new DataObject();
                dataObject.SetData(
                    DataFormats.FileDrop,
                    new string[] { sourceFile.FullName });

                Assert.IsEmpty(browser.GetPastableFiles(dataObject, true));

                taskDialog.Verify(
                    d => d.ShowDialog(browser, It.IsAny<TaskDialogParameters>()),
                    Times.Once);
            }
        }

        //---------------------------------------------------------------------
        // PasteFiles.
        //---------------------------------------------------------------------

        [Test]
        public async Task PasteFiles_WhenUserCancels()
        {
            var cts = new CancellationTokenSource();

            var operation = new Mock<IOperation>();
            operation
                .SetupGet(o => o.CancellationToken)
                .Returns(cts.Token);
            var progressDialog = new Mock<IOperationProgressDialog>();
            progressDialog
                .Setup(d => d.StartCopyOperation(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<ulong>(),
                    It.IsAny<ulong>()))
                .Returns(operation.Object);

            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var sourceFile = new FileInfo(Path.GetTempFileName());
                var taskDialog = new Mock<ITaskDialog>();
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill,
                    TaskDialog = taskDialog.Object,
                    ProgressDialog = progressDialog.Object,
                };
                form.Controls.Add(browser);
                browser.Bind(
                    CreateFileSystem().Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                var dataObject = new DataObject();
                dataObject.SetData(
                    DataFormats.FileDrop,
                    new string[] { sourceFile.FullName });

                cts.Cancel();

                await browser
                    .PasteFilesAsync(dataObject)
                    .ConfigureAwait(true);

                operation.Verify(
                    o => o.OnItemCompleted(),
                    Times.Never);
                taskDialog.Verify(
                    d => d.ShowDialog(browser, It.IsAny<TaskDialogParameters>()),
                    Times.Never);
            }
        }

        [Test]
        public async Task PasteFiles_WhenCopyFails_ThenUserCanIgnore()
        {
            var sourceFiles = new[]
            {
                new FileInfo(Path.GetTempFileName()),
                new FileInfo(Path.GetTempFileName())
            };

            using (File.Open(   // Lock first file.
                sourceFiles[0].FullName,
                FileMode.Open,
                FileAccess.Write,
                FileShare.Write))
            using (File.Open(   // Lock second file.
                sourceFiles[1].FullName,
                FileMode.Open,
                FileAccess.Write,
                FileShare.Write))
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var taskDialog = new Mock<ITaskDialog>();
                taskDialog
                    .Setup(d => d.ShowDialog(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<TaskDialogParameters>()))
                    .Returns(DialogResult.Ignore);

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill,
                    TaskDialog = taskDialog.Object
                };
                form.Controls.Add(browser);
                browser.Bind(
                    CreateFileSystem().Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                var dataObject = new DataObject();
                dataObject.SetData(
                    DataFormats.FileDrop,
                    new string[]
                    {
                        sourceFiles[0].FullName,
                        sourceFiles[1].FullName
                    });

                await browser
                    .PasteFilesAsync(dataObject)
                    .ConfigureAwait(true);
                taskDialog.Verify(
                    d => d.ShowDialog(browser, It.IsAny<TaskDialogParameters>()),
                    Times.Exactly(2));
            }
        }

        [Test]
        public async Task PasteFiles_WhenCopyCancelled_ThenNoErrorShown()
        {
            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var taskDialog = new Mock<ITaskDialog>();
                taskDialog
                    .Setup(d => d.ShowDialog(
                        It.IsAny<IWin32Window>(),
                        It.IsAny<TaskDialogParameters>()))
                    .Returns(DialogResult.Ignore);

                var fileSystem = CreateFileSystem();
                fileSystem
                    .Setup(f => f.OpenFileAsync(
                        It.IsAny<IFileItem>(),
                        It.IsAny<string>(),
                        FileMode.Create,
                        FileAccess.Write))
                    .ThrowsAsync(new OperationCanceledException());

                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill,
                    TaskDialog = taskDialog.Object
                };
                form.Controls.Add(browser);
                browser.Bind(
                    fileSystem.Object,
                    new Mock<IBindingContext>().Object);

                form.Show();
                Application.DoEvents();

                var dataObject = new DataObject();
                dataObject.SetData(
                    DataFormats.FileDrop,
                    new string[]
                    {
                        Path.GetTempFileName()
                    });

                await browser
                    .PasteFilesAsync(dataObject)
                    .ConfigureAwait(true);
                taskDialog.Verify(
                    d => d.ShowDialog(browser, It.IsAny<TaskDialogParameters>()),
                    Times.Never);
                fileSystem.Verify(
                    fs => fs.ListFilesAsync(It.IsAny<IFileItem>()),
                    Times.Once);
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose_DisposesFileSystem()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem
                .SetupGet(fs => fs.Root)
                .Returns(CreateDirectory().Object);
            fileSystem
                .Setup(fs => fs.ListFilesAsync(It.IsAny<IFileItem>()))
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


                browser.Bind(
                    fileSystem.Object,
                    new Mock<IBindingContext>().Object);
                Application.DoEvents();

                form.Show();
            }

            fileSystem.Verify(f => f.Dispose(), Times.Once);
        }
    }
}
