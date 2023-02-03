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
using Google.Solutions.Mvvm.Binding;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Download
{
    [Service]
    internal partial class DownloadFileView : Form, IView<DownloadFileViewModel>
    {
        private readonly IExceptionDialog exceptionDialog;

        public DownloadFileView(IExceptionDialog exceptionDialog)
        {
            this.exceptionDialog = exceptionDialog;

            InitializeComponent();
        }

        public void Bind(DownloadFileViewModel viewModel)
        {
            this.BindReadonlyProperty(
                c => c.Text,
                viewModel,
                m => m.DialogText,
                this.Container);
            this.targetDirectoryTextBox.BindProperty(
                c => c.Text,
                viewModel,
                m => m.TargetDirectory,
                this.Container);
            this.fileBrowser.BindProperty(
                c => c.SelectedFiles,
                viewModel,
                m => m.SelectedFiles,
                this.Container);
            this.downloadButton.BindReadonlyProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsDownloadButtonEnabled,
                this.Container);
            this.fileBrowser.Bind(viewModel.FileSystem);

            this.fileBrowser.NavigationFailed += (_, args) =>
                this.exceptionDialog.Show(this, "Navigation failed", args.Exception);

            this.browseButton.Click += (s, args) =>
            {
                using (var dialog = new FolderBrowserDialog()
                {
                    Description = "The files are downloaded and saved to this folder",
                    SelectedPath = viewModel.TargetDirectory.Value
                })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        viewModel.TargetDirectory.Value = dialog.SelectedPath;
                    }
                }
            };
        }
    }
}
