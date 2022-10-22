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
    public class TestFileBrowserUi
    {
        [Test]
        public void TestUI() // TODO: Remove test case
        {
            using (var fileTypeCache = new FileTypeCache())
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

                    var root = new LocalFileItem(
                        new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Personal)),
                        fileTypeCache);

                    browser.NavigationFailed += (s, e) => MessageBox.Show(
                        form,
                        e.Exception.Message);

                    browser.Bind(
                        root,
                        item => Task.FromResult(((LocalFileItem)item).GetChildren()));

                    form.Controls.Add(browser);
                    form.ShowDialog();
                }
            }
        }

        private class LocalFileItem : ViewModelBase, IFileItem
        {
            private bool expanded;
            private readonly FileSystemInfo fileInfo;
            private readonly FileTypeCache fileTypeCache;

            public LocalFileItem(
                FileSystemInfo fileInfo,
                FileTypeCache fileTypeCache)
            {
                this.fileInfo = fileInfo;
                this.fileTypeCache = fileTypeCache;
            }

            public ObservableCollection<IFileItem> GetChildren()
            {
                var directories = (this.fileInfo as DirectoryInfo)?
                    .GetDirectories()
                    .EnsureNotNull()
                    .Select(f => new LocalFileItem(f, this.fileTypeCache))
                    .ToList();

                var files = (this.fileInfo as DirectoryInfo)?
                    .GetFiles()
                    .EnsureNotNull()
                    .Select(f => new LocalFileItem(f, this.fileTypeCache))
                    .ToList();

                return (new ObservableCollection<IFileItem>(directories.Concat(files)));
            }

            public string Name => this.fileInfo.Name;

            public bool IsFile => this.fileInfo is FileInfo;

            public FileAttributes Attributes => this.fileInfo.Attributes;

            public DateTime LastModified => this.fileInfo.LastWriteTimeUtc;

            public ulong Size => (ulong)((this.fileInfo as FileInfo)?.Length ?? 0);

            public FileType Type => this.fileTypeCache.Lookup(
                this.fileInfo.Name,
                this.fileInfo.Attributes, 
                FileType.IconFlags.None);

            public bool IsExpanded
            {
                get => this.expanded;
                set
                {
                    this.expanded = value;
                    RaisePropertyChange();
                }
            }
        }
    }
}
