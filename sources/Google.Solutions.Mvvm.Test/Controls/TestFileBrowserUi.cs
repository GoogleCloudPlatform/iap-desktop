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

using Google.Solutions.Common.Linq;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestFileBrowserUi
    {
        [RequiresInteraction]
        [Test]
        public void TestUI()
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

                var fileSystem = new LocalFileSystem(
                    new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Personal)));

                browser.NavigationFailed += (s, e) => MessageBox.Show(
                    form,
                    e.Exception.Message);

                var bindingContext = new Mock<IBindingContext>().Object;
                browser.Bind(fileSystem, bindingContext);
                browser.OnControlPropertyChange(
                    b => b.CurrentDirectory,
                    path => form.Text = browser.CurrentPath,
                    bindingContext);

                form.Controls.Add(browser);
                form.ShowDialog();
            }
        }

        private class LocalFileSystem : IFileSystem
        {
            private readonly FileTypeCache fileTypeCache = new FileTypeCache();

            public IFileItem Root { get; }

            public LocalFileSystem(DirectoryInfo directory)
            {
                this.Root = new LocalFileItem(directory, this.fileTypeCache);
            }

            public async Task<ObservableCollection<IFileItem>> ListFilesAsync(IFileItem folder)
            {
                var localFolder = (LocalFileItem)folder;
                var directories = (localFolder.FileInfo as DirectoryInfo)?
                    .GetDirectories()
                    .EnsureNotNull()
                    .Select(f => new LocalFileItem(f, this.fileTypeCache))
                    .ToList();

                var files = (localFolder.FileInfo as DirectoryInfo)?
                    .GetFiles()
                    .EnsureNotNull()
                    .Select(f => new LocalFileItem(f, this.fileTypeCache))
                    .ToList();

                await Task.Delay(750);
                return (new ObservableCollection<IFileItem>(directories.Concat(files)));
            }

            public void Dispose()
            {
            }
        }

        private class LocalFileItem : ViewModelBase, IFileItem
        {
            private bool expanded;
            private readonly FileTypeCache fileTypeCache;

            internal FileSystemInfo FileInfo { get; }

            public LocalFileItem(
                FileSystemInfo fileInfo,
                FileTypeCache fileTypeCache)
            {
                this.FileInfo = fileInfo;
                this.fileTypeCache = fileTypeCache;
            }

            public string Name
            {
                get => this.FileInfo.Name;
            }

            public string Path
            {
                get => this.FileInfo.FullName;
            }

            public bool IsFile
            {
                get => this.FileInfo is FileInfo;
            }

            public FileAttributes Attributes
            {
                get => this.FileInfo.Attributes;
            }

            public DateTime LastModified
            {
                get => this.FileInfo.LastWriteTimeUtc;
            }

            public ulong Size
            {
                get => (ulong)((this.FileInfo as FileInfo)?.Length ?? 0);
            }

            public FileType Type
            {
                get => this.fileTypeCache.Lookup(
                    this.FileInfo.Name,
                    this.FileInfo.Attributes,
                    FileType.IconFlags.None);
            }

            public bool IsExpanded
            {
                get => this.expanded;
                set
                {
                    this.expanded = value;
                    RaisePropertyChange();
                }
            }

            public Stream Open(FileAccess access)
            {
                Precondition.Expect(this.IsFile, "Not a file");
                return File.Open(this.FileInfo.FullName, FileMode.Open, access);
            }

            public Stream Create(
                string name,
                FileMode mode,
                FileAccess access)
            {
                Precondition.Expect(!this.IsFile, "Not a directory");

                return File.Open(
                    System.IO.Path.Combine(this.FileInfo.FullName, name), 
                    FileMode.OpenOrCreate,
                    access);
            }
        }
    }
}
