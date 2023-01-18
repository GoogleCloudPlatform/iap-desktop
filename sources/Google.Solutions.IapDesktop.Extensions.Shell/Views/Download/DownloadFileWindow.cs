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

using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Download
{
    internal partial class DownloadFileWindow : Form, IThemedControl
    {
        private readonly DownloadFileViewModel viewModel;

        public DownloadFileWindow(
            FileBrowser.IFileSystem fileSystem,
            IExceptionDialog exceptionDialog)
        {
            InitializeComponent();

            SuspendLayout();

            this.viewModel = new DownloadFileViewModel();

            this.targetDirectoryTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.TargetDirectory,
                this.Container);
            this.fileBrowser.BindProperty(
                c => c.SelectedFiles,
                this.viewModel,
                m => m.SelectedFiles,
                this.Container);
            this.downloadButton.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsDownloadButtonEnabled,
                this.Container);
            this.fileBrowser.Bind(fileSystem);
            this.fileBrowser.NavigationFailed += (_, args) =>
                exceptionDialog.Show(this, "Navigation failed", args.Exception);

            this.browseButton.Click += (s, args) =>
            {
                using (var dialog = new FolderBrowserDialog()
                {
                    Description = "The files are downloaded and saved to this folder",
                    SelectedPath = this.viewModel.TargetDirectory.Value
                })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        this.viewModel.TargetDirectory.Value = dialog.SelectedPath;
                    }
                }
            };

            ResumeLayout();
        }

        public string TargetDirectory => this.targetDirectoryTextBox.Text;

        public IEnumerable<FileBrowser.IFileItem> SelectedFiles => this.viewModel
            .SelectedFiles
            .Value;

        //---------------------------------------------------------------------
        // IThemedControl.
        //---------------------------------------------------------------------

        public IControlTheme Theme
        {
            get => this.fileBrowser.Theme;
            set => this.fileBrowser.Theme = value;
        }
    }
}
