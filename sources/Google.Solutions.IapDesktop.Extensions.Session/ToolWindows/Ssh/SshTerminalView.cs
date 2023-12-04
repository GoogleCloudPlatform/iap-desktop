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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh
{
    [Service]
    public class SshTerminalView : TerminalViewBase, ISshTerminalSession, IView<SshTerminalViewModel>
    {
        private SshTerminalViewModel viewModel;
        private readonly ViewFactory<SshAuthenticationPromptView, SshAuthenticationPromptViewModel> promptFactory;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public SshTerminalView(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.promptFactory = serviceProvider.GetViewFactory<SshAuthenticationPromptView, SshAuthenticationPromptViewModel>();
            this.promptFactory.Theme = serviceProvider.GetService<IThemeService>().DialogTheme;
        }

        public void Bind(
            SshTerminalViewModel viewModel,
            IBindingContext bindingContext)
        {
            base.Bind(viewModel, bindingContext);

            this.viewModel = viewModel;
            this.viewModel.AuthenticationPrompt += OnAuthenticationPrompt;

            this.AllowDrop = true;
            this.DragDrop += new System.Windows.Forms.DragEventHandler(SshTerminalPane_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(SshTerminalPane_DragEnter);
        }

        private void OnAuthenticationPrompt(object sender, AuthenticationPromptEventArgs e)
        {
            Debug.Assert(!this.InvokeRequired);

            using (var prompt = this.promptFactory.CreateDialog())
            {
                prompt.ViewModel.Title = e.Name;
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

        public static SshTerminalView TryGetExistingPane(
            IMainWindow mainForm,
            InstanceLocator vmInstance)
        {
            return mainForm.MainPanel
                .Documents
                .EnsureNotNull()
                .OfType<SshTerminalView>()
                .Where(pane => pane.Instance == vmInstance && !pane.IsFormClosing)
                .FirstOrDefault();
        }

        public static SshTerminalView TryGetActivePane(
            IMainWindow mainForm)
        {
            //
            // NB. The active content might be in a float window.
            //
            return mainForm.MainPanel.ActivePane?.ActiveContent as SshTerminalView;
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void SshTerminalPane_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                SshTerminalViewModel
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
                        var files = SshTerminalViewModel
                            .GetDroppableFiles(e.Data.GetData(DataFormats.FileDrop));

                        return this.viewModel.UploadFilesAsync(files);
                    },
                    "Uploading files")
                .ConfigureAwait(true);
        }
    }
}
