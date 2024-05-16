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

using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Shell;
using Moq;
using NUnit.Framework;
using System;
using System.Drawing;
using System.IO;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Ssh
{
    [TestFixture]
    public class TestDownloadFileViewModel
    {
        private static Mock<FileBrowser.IFileSystem> CreateFileSystem()
        {
            return new Mock<FileBrowser.IFileSystem>();
        }

        private static Mock<FileBrowser.IFileItem> CreateFileItem(
            string name,
            FileAttributes attributes)
        {
            var fileType = new FileType(
                "Type",
                !attributes.HasFlag(FileAttributes.Directory),
                SystemIcons.Application.ToBitmap());

            var file = new Mock<FileBrowser.IFileItem>();
            file.SetupGet(i => i.Name).Returns(name);
            file.SetupGet(i => i.LastModified).Returns(DateTime.UtcNow);
            file.SetupGet(i => i.Type).Returns(fileType);
            file.SetupGet(i => i.Size).Returns(0);
            file.SetupGet(i => i.Attributes).Returns(attributes);
            file.SetupGet(i => i.IsExpanded).Returns(false);

            return file;
        }

        //---------------------------------------------------------------------
        // IsDownloadButtonEnabled.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoFileSelected_ThenIsDownloadButtonEnabledReturnsFalse()
        {
            var viewModel = new DownloadFileViewModel(CreateFileSystem().Object);
            Assert.IsFalse(viewModel.IsDownloadButtonEnabled.Value);
        }

        [Test]
        public void WhenDirectorySelected_ThenIsDownloadButtonEnabledReturnsFalse()
        {
            var viewModel = new DownloadFileViewModel(CreateFileSystem().Object);
            viewModel.SelectedFiles.Value = new[]
            {
                CreateFileItem("Directory", FileAttributes.Directory).Object,
                CreateFileItem("File", FileAttributes.Normal).Object
            };

            Assert.IsFalse(viewModel.IsDownloadButtonEnabled.Value);
        }

        [Test]
        public void WhenLinkSelected_ThenIsDownloadButtonEnabledReturnsFalse()
        {
            var viewModel = new DownloadFileViewModel(CreateFileSystem().Object);
            viewModel.SelectedFiles.Value = new[]
            {
                CreateFileItem("Link", FileAttributes.ReparsePoint).Object,
                CreateFileItem("File", FileAttributes.Normal).Object
            };

            Assert.IsFalse(viewModel.IsDownloadButtonEnabled.Value);
        }

        [Test]
        public void WhenMultipleFilesSelected_ThenIsDownloadButtonEnabledReturnsTrue()
        {
            var viewModel = new DownloadFileViewModel(CreateFileSystem().Object);
            viewModel.SelectedFiles.Value = new[]
            {
                CreateFileItem("File 1", FileAttributes.Normal).Object,
                CreateFileItem("File 2", FileAttributes.Normal).Object
            };

            Assert.IsTrue(viewModel.IsDownloadButtonEnabled.Value);
        }
    }
}
