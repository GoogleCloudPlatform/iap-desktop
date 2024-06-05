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
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh
{
    public interface IDownloadFileDialog
    {
        /// <summary>
        /// Show file browser to select files to download, and the
        /// directory to download them to.
        /// </summary>
        DialogResult SelectDownloadFiles(
            IWin32Window owner,
            string caption,
            FileBrowser.IFileSystem fileSystem,
            out IEnumerable<FileBrowser.IFileItem>? sourceItems,
            out DirectoryInfo? targetDirectory);
    }

    [Service(typeof(IDownloadFileDialog))]
    public class DownloadFileDialog : IDownloadFileDialog
    {
        private readonly WindowActivator<DownloadFileView, DownloadFileViewModel, IDialogTheme> downloadWindow;

        public DownloadFileDialog(
            WindowActivator<DownloadFileView, DownloadFileViewModel, IDialogTheme> downloadWindow)
        {
            this.downloadWindow = downloadWindow;
        }

        public DialogResult SelectDownloadFiles(
            IWin32Window? owner,
            string caption,
            FileBrowser.IFileSystem fileSystem,
            out IEnumerable<FileBrowser.IFileItem>? sourceItems,
            out DirectoryInfo? targetDirectory)
        {
            sourceItems = null;
            targetDirectory = null;

            using (var dialog = this.downloadWindow.CreateDialog(new DownloadFileViewModel(fileSystem)))
            {
                dialog.ViewModel.DialogText.Value = caption;

                var result = dialog.ShowDialog(owner);

                if (result == DialogResult.OK)
                {
                    targetDirectory = new DirectoryInfo(dialog.ViewModel.TargetDirectory.Value);
                    sourceItems = dialog.ViewModel.SelectedFiles.Value;
                }

                return result;
            }
        }
    }
}
