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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.Mvvm.Controls;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Download
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
            IExceptionDialog exceptionDialog,
            out IEnumerable<FileBrowser.IFileItem> sourceItems,
            out DirectoryInfo targetDirectory);
    }


    [Service(typeof(IDownloadFileDialog))]
    public class DownloadFileDialog : IDownloadFileDialog
    {
        public DialogResult SelectDownloadFiles(
            IWin32Window owner,
            string caption,
            FileBrowser.IFileSystem fileSystem,
            IExceptionDialog exceptionDialog,
            out IEnumerable<FileBrowser.IFileItem> sourceItems,
            out DirectoryInfo targetDirectory)
        {
            sourceItems = null;
            targetDirectory = null;

            using (var form = new DownloadFileWindow(fileSystem, exceptionDialog)
            {
                Text = caption
            })
            {
                var result = form.ShowDialog(owner);

                if (result == DialogResult.OK)
                {
                    targetDirectory = new DirectoryInfo(form.TargetDirectory);
                    sourceItems = form.SelectedFiles;
                }

                return result;
            }
        }
    }
}
