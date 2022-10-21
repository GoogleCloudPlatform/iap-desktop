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
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        [Test]
        public void _____() // TODO: Remove test case
        {
            var form = new Form();
            var browser = new FileBrowser()
            {
                Dock = DockStyle.Fill
            };

            browser.Bind(
                new File(new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Personal))),
                item => Task.FromResult(new ObservableCollection<IFileItem>(((File)item).GetChildren())));

            form.Controls.Add(browser);

            form.ShowDialog();
        }

        private class File : IFileItem
        {
            internal readonly FileSystemInfo fileInfo;

            public File(FileSystemInfo fileInfo)
            {
                this.fileInfo = fileInfo;
            }

            public IEnumerable<IFileItem> GetChildren()
                => this.GetDirectories().Concat(this.GetFiles());

            public IEnumerable<IFileItem> GetFiles()
                => (this.fileInfo as DirectoryInfo)?
                    .GetFiles()
                    .EnsureNotNull()
                    .Select(f => new File(f))
                    .ToList();

            public IEnumerable<IFileItem> GetDirectories()
                => (this.fileInfo as DirectoryInfo)?
                    .GetDirectories()
                    .EnsureNotNull()
                    .Select(f => new File(f))
                    .ToList();

            public string Name => this.fileInfo.Name;

            public bool IsFile => this.fileInfo is FileInfo;

            public FileAttributes Attributes => this.fileInfo.Attributes;

            public DateTime LastModified => this.fileInfo.LastWriteTimeUtc;

            public ulong Size => (ulong)((this.fileInfo as FileInfo)?.Length ?? 0);

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
