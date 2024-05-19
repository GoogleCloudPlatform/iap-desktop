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

using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.ObjectModel;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Threading;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Ssh
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestDownloadFileDialog
    {
        private static Mock<FileBrowser.IFileItem> CreateFileItem(
            string name,
            bool directory)
        {
            var fileType = new FileType(
                "Type",
                !directory,
                SystemIcons.Application.ToBitmap());

            var file = new Mock<FileBrowser.IFileItem>();
            file.SetupGet(i => i.Name).Returns(name);
            file.SetupGet(i => i.LastModified).Returns(DateTime.UtcNow);
            file.SetupGet(i => i.Type).Returns(fileType);
            file.SetupGet(i => i.Size).Returns(0);
            file.SetupGet(i => i.IsExpanded).Returns(false);

            return file;
        }

        [RequiresInteraction]
        [Test]
        public void TestUI()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            var directoryType = FileType.Lookup(
                ".",
                FileAttributes.Directory,
                FileType.IconFlags.None);

            var root = CreateFileItem("Root", true);
            var fileSystem = new Mock<FileBrowser.IFileSystem>();
            fileSystem.SetupGet(f => f.Root).Returns(root.Object);
            fileSystem.Setup(f => f.ListFilesAsync(It.IsIn(root.Object)))
                .ReturnsAsync(new ObservableCollection<FileBrowser.IFileItem>()
                {
                    CreateFileItem("Directory", true).Object,
                    CreateFileItem("File", false).Object
                });

            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.AddMock<IExceptionDialog>();
            serviceRegistry.AddTransient<DownloadFileView>();
            serviceRegistry.AddTransient<DownloadFileViewModel>();

            var dialog = new DownloadFileDialog(serviceRegistry);
            dialog.SelectDownloadFiles(
                null,
                "Test",
                fileSystem.Object,
                out var sourceItems,
                out var targetDirectory);
        }
    }
}
