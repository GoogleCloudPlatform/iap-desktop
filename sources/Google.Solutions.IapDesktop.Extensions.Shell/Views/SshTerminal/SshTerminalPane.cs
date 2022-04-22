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
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Controls;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Ssh;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{
    public class SshTerminalPane : TerminalPaneBase, ISshTerminalSession
    {
        private readonly SshTerminalPaneViewModel viewModel;

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
                    vmInstance,
                    endpoint,
                    authorizedKey,
                    language,
                    connectionTimeout))
        {
            this.viewModel = (SshTerminalPaneViewModel)this.ViewModel;
            this.viewModel.AuthenticationPrompt += OnAuthenticationPrompt;

            this.AllowDrop = true;
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.SshTerminalPane_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.SshTerminalPane_DragEnter);
        }

        private void OnAuthenticationPrompt(object sender, AuthenticationPromptEventArgs e)
        {
            Debug.Assert(!this.InvokeRequired);
            e.Response = SshAuthenticationPromptDialog.ShowPrompt(
                this,
                "2-step verification",
                e.Prompt,
                e.IsPasswordPrompt);
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
            return mainForm.MainPanel.ActiveDocument as SshTerminalPane;
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void SshTerminalPane_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                this.viewModel
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
                        var files = this.viewModel.GetDroppableFiles(
                            e.Data.GetData(DataFormats.FileDrop));

                        return this.viewModel.UploadFilesAsync(files);
                    },
                    "Uploading files")
                .ConfigureAwait(true);
        }
    }
}
