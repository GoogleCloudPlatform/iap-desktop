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

using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh;
using Google.Solutions.Mvvm.Binding;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Download
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

        public void Bind(
            DownloadFileViewModel viewModel,
            IBindingContext bindingContext)
        {
            this.BindReadonlyObservableProperty(
                c => c.Text,
                viewModel,
                m => m.DialogText,
                bindingContext);
            this.targetDirectoryTextBox.BindObservableProperty(
                c => c.Text,
                viewModel,
                m => m.TargetDirectory,
                bindingContext);
            this.fileBrowser.BindObservableProperty(
                c => c.SelectedFiles,
                viewModel,
                m => m.SelectedFiles,
                bindingContext);
            this.downloadButton.BindReadonlyObservableProperty(
                c => c.Enabled,
                viewModel,
                m => m.IsDownloadButtonEnabled,
                bindingContext);
            this.fileBrowser.Bind(viewModel.FileSystem, bindingContext);

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
