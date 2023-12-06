//
// Copyright 2023 Google LLC
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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Diagnostics.ToolWindows
{
    [Service]
    public class DebugJobServiceViewModel : ViewModelBase
    {
        private readonly IJobService jobService;
        private readonly IEventQueue eventService;

        public DebugJobServiceViewModel(
            IJobService jobService,
            IEventQueue eventService)
        {
            this.jobService = jobService;
            this.eventService = eventService;

            this.StatusText = ObservableProperty.Build(string.Empty);
            this.IsSpinnerVisible = ObservableProperty.Build(false);
            this.IsBackgroundJob = ObservableProperty.Build(false);


            this.eventService.Subscribe<StatusUpdatedEvent>(
                e =>
                {
                    this.StatusText.Value = e.Status;
                });
            this.eventService.Subscribe<StatusUpdatedEvent>(
                async e =>
                {
                    await Task.Delay(10).ConfigureAwait(true);
                    System.Diagnostics.Debug.WriteLine("Delayed in event handler");
                });
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableProperty<string> StatusText { get; }
        public ObservableProperty<bool> IsSpinnerVisible { get; }
        public ObservableProperty<bool> IsBackgroundJob { get; }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        private JobDescription CreateJobDescription(string text)
        {
            return new JobDescription(
                text,
                this.IsBackgroundJob.Value
                    ? JobUserFeedbackType.BackgroundFeedback
                    : JobUserFeedbackType.ForegroundFeedback);
        }

        public async Task RunJobAsync()
        {
            this.IsSpinnerVisible.Value = true;

            try
            {
                await this.jobService
                    .RunAsync(
                        CreateJobDescription("This takes a while, but can be cancelled..."),
                        async token =>
                        {
                            System.Diagnostics.Debug.WriteLine("Starting delay...");
                            await this.eventService
                                .PublishAsync(new StatusUpdatedEvent("Starting delay..."))
                                .ConfigureAwait(true);

                            await Task
                            .Delay(5000, token)
                                .ConfigureAwait(true);

                            System.Diagnostics.Debug.WriteLine("Delay over");

                            await this.eventService
                                .PublishAsync(new StatusUpdatedEvent("Done"))
                                .ConfigureAwait(true);
                        })
                    .ConfigureAwait(true);
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.View, ex.Message);
            }

            this.IsSpinnerVisible.Value = false;
        }

        public async Task RunCancellableJobAsync()
        {
            this.IsSpinnerVisible.Value = true;

            try
            {
                await this.jobService
                    .RunAsync(
                        CreateJobDescription("This takes a while, and cannot be cancelled..."),
                        async token =>
                        {
                            System.Diagnostics.Debug.WriteLine("Starting delay...");
                            await this.eventService
                                .PublishAsync(new StatusUpdatedEvent("Starting delay..."))
                                .ConfigureAwait(true);

                            await Task
                                .Delay(5000)
                                .ConfigureAwait(true);

                            System.Diagnostics.Debug.WriteLine("Delay over");

                            await this.eventService
                                .PublishAsync(new StatusUpdatedEvent("Done"))
                                .ConfigureAwait(true);
                        })
                    .ConfigureAwait(true);
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.View, ex.Message);
            }

            this.IsSpinnerVisible.Value = false;
        }

        public async Task RunFailingJobAsync()
        {
            this.IsSpinnerVisible.Value = true;

            try
            {
                await this.jobService
                    .RunAsync(
                        CreateJobDescription("This takes a while, and cannot be cancelled..."),
                        token =>
                        {
                            throw new ApplicationException("bang!");
                        })
                    .ConfigureAwait(true);
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.View, ex.Message);
            }

            this.IsSpinnerVisible.Value = false;
        }

        public async Task TriggerReauthAsync()
        {
            this.IsSpinnerVisible.Value = true;

            try
            {
                await this.jobService
                    .RunAsync(
                        CreateJobDescription("This takes a while, and cannot be cancelled..."),
                        token =>
                        {
                            throw new TokenResponseException(new TokenErrorResponse()
                            {
                                Error = "invalid_grant"
                            });
                        })
                    .ConfigureAwait(true);
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Task cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.View, ex.Message);
            }

            this.IsSpinnerVisible.Value = false;
        }

        //---------------------------------------------------------------------
        // Helper classes.
        //---------------------------------------------------------------------

        public class StatusUpdatedEvent
        {
            public string Status { get; }

            public StatusUpdatedEvent(string status)
            {
                this.Status = status;
            }
        }
    }
}
