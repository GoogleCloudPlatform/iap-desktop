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

using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Windows.RemoteDesktop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Application.Services.Windows
{
    [ComVisible(false)]
    public partial class DebugWindow : ToolWindow
    {
        private readonly IJobService jobService;
        private readonly IEventService eventService;
        private readonly IRemoteDesktopService rdpService;
        private readonly DockPanel dockPanel;
        private readonly IServiceProvider serviceProvider;

        public DebugWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            this.jobService = serviceProvider.GetService<IJobService>();
            this.eventService = serviceProvider.GetService<IEventService>();
            this.rdpService = serviceProvider.GetService<IRemoteDesktopService>();

            this.TabText = this.Text;

            this.eventService.BindHandler<StatusUpdatedEvent>(
                e =>
                {
                    this.label.Text = e.Status;
                });
            this.eventService.BindAsyncHandler<StatusUpdatedEvent>(
                async e =>
                {
                    await Task.Delay(10);
                    Debug.WriteLine("Delayed in event handler");
                });


            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;
            this.serviceProvider = serviceProvider;
        }

        public void ShowWindow()
        {
            Show(this.dockPanel, DockState.DockRightAutoHide);
        }

        public class StatusUpdatedEvent
        {
            public string Status { get; }

            public StatusUpdatedEvent(string status)
            {
                this.Status = status;
            }
        }

        private async void slowOpButton_Click(object sender, EventArgs e)
        {
            this.spinner.Visible = true;
            try
            {
                await this.jobService.RunInBackground<object>(
                    new JobDescription("This takes a while, but can be cancelled..."),
                    async token =>
                    {
                        Debug.WriteLine("Starting delay...");
                        await this.eventService.FireAsync(new StatusUpdatedEvent("Starting delay..."));

                        await Task.Delay(5000, token);

                        Debug.WriteLine("Delay over");
                        await this.eventService.FireAsync(new StatusUpdatedEvent("Done"));

                        return null;
                    });
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            this.spinner.Visible = false;
        }

        private async void slowNonCanelOpButton_Click(object sender, EventArgs e)
        {
            this.spinner.Visible = true;
            try
            {
                await this.jobService.RunInBackground<object>(
                    new JobDescription("This takes a while, and cannot be cancelled..."),
                    async token =>
                    {
                        Debug.WriteLine("Starting delay...");
                        await this.eventService.FireAsync(new StatusUpdatedEvent("Starting delay..."));

                        await Task.Delay(5000);

                        Debug.WriteLine("Delay over");
                        await this.eventService.FireAsync(new StatusUpdatedEvent("Done"));

                        return null;
                    });
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            this.spinner.Visible = false;
        }

        private async void throwExceptionButton_Click(object sender, EventArgs e)
        {
            this.spinner.Visible = true;
            try
            {
                await this.jobService.RunInBackground<object>(
                    new JobDescription("This takes a while, and cannot be cancelled..."),
                    token =>
                    {
                        throw new ApplicationException("bang!");
                    });
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            this.spinner.Visible = false;
        }

        private async void reauthButton_Click(object sender, EventArgs e)
        {

            this.spinner.Visible = true;
            try
            {
                await this.jobService.RunInBackground<object>(
                    new JobDescription("This takes a while, and cannot be cancelled..."),
                    token =>
                    {
                        throw new TokenResponseException(new TokenErrorResponse()
                        {
                            Error = "invalid_grant"
                        });
                    });
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            this.spinner.Visible = false;
        }

        private void connectButton_Click(object sender, EventArgs args)
        {
            var server = this.serverTextBox.Text.Split(':');

            try
            {
                this.rdpService.Connect(
                    null,
                    server[0],
                    (ushort)(server.Length > 1 ? int.Parse(server[1]) : 3389),
                    new VmInstanceConnectionSettings()
                    {

                    });
            }
            catch (Exception e)
            {
                this.serviceProvider
                    .GetService<IExceptionDialog>()
                    .Show(this, "RDP failed", e);
            }
        }
    }
}
