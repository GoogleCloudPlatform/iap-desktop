//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Download;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{
    public class SshTerminalPane : TerminalPaneBase, ISshTerminalSession
    {
        private readonly SshTerminalPaneViewModel viewModel;
        private readonly ViewFactory<SshAuthenticationPromptView, SshAuthenticationPromptViewModel> promptFactory;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public SshTerminalPane()
        {
            // Constructor is for designer only.
        }

        internal SshTerminalPane(
            IServiceProvider serviceProvider,
            InstanceLocator vmInstance,
            IPEndPoint endpoint,
            AuthorizedKeyPair authorizedKey,
            CultureInfo language,
            TimeSpan connectionTimeout)
            : base(
                  serviceProvider,
                  new SshTerminalPaneViewModel(
                    serviceProvider.GetService<IEventService>(),
                    serviceProvider.GetService<IJobService>(),
                    serviceProvider.GetService<IConfirmationDialog>(),
                    serviceProvider.GetService<IOperationProgressDialog>(),
                    serviceProvider.GetService<IDownloadFileDialog>(),
                    serviceProvider.GetService<IExceptionDialog>(),
                    serviceProvider.GetService<IQuarantineAdapter>(),
                    vmInstance,
                    endpoint,
                    authorizedKey,
                    language,
                    connectionTimeout))
        {
            this.viewModel = (SshTerminalPaneViewModel)this.ViewModel;
            this.viewModel.View = this;
            this.viewModel.AuthenticationPrompt += OnAuthenticationPrompt;

            this.promptFactory = serviceProvider.GetViewFactory<SshAuthenticationPromptView, SshAuthenticationPromptViewModel>();
            this.promptFactory.Theme = serviceProvider.GetService<IThemeService>().DialogTheme;

            this.AllowDrop = true;
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.SshTerminalPane_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.SshTerminalPane_DragEnter);
        }

        private void OnAuthenticationPrompt(object sender, AuthenticationPromptEventArgs e)
        {
            Debug.Assert(!this.InvokeRequired);

            var prompt = this.promptFactory.Create(); //TODO: Test manually
            prompt.ViewModel.Title = "2-step verification";
            prompt.ViewModel.Description = e.Prompt;
            prompt.ViewModel.IsPasswordMasked = e.IsPasswordPrompt;

            if (prompt.ShowDialog(this) == DialogResult.OK)
            {
                e.Response = prompt.ViewModel.Input;
            }
            else
            {
                throw new OperationCanceledException();
            }
        }

        //---------------------------------------------------------------------
        // ISshTerminalSession.
        //---------------------------------------------------------------------

        public Task DownloadFilesAsync()
        {
            return this.viewModel.DownloadFilesAsync();
        }

        //---------------------------------------------------------------------
        // Statics.
        //---------------------------------------------------------------------

        public static SshTerminalPane TryGetExistingPane(
            IMainForm mainForm,
            InstanceLocator vmInstance)
        {
            return mainForm.MainPanel
                .Documents
                .EnsureNotNull()
                .OfType<SshTerminalPane>()
                .Where(pane => pane.Instance == vmInstance && !pane.IsFormClosing)
                .FirstOrDefault();
        }

        public static SshTerminalPane TryGetActivePane(
            IMainForm mainForm)
        {
            //
            // NB. The active content might be in a float window.
            //
            return mainForm.MainPanel.ActivePane?.ActiveContent as SshTerminalPane;
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void SshTerminalPane_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                SshTerminalPaneViewModel
                    .GetDroppableFiles(e.Data.GetData(DataFormats.FileDrop))
                    .Any())
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private async void SshTerminalPane_DragDrop(object sender, DragEventArgs e)
        {
            await InvokeActionAsync(
                    () =>
                    {
                        var files = SshTerminalPaneViewModel
                            .GetDroppableFiles(e.Data.GetData(DataFormats.FileDrop));

                        return this.viewModel.UploadFilesAsync(files);
                    },
                    "Uploading files")
                .ConfigureAwait(true);
        }
    }
}
