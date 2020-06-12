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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Google.Solutions.IapDesktop.Windows
{
    internal class MainFormViewModel : ViewModelBase
    {
        // NB. This list is only access from the UI thread, so no locking required.
        private readonly LinkedList<BackgroundJob> backgroundJobs
            = new LinkedList<BackgroundJob>();

        private bool isBackgroundJobStatusVisible = false;

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsBackgroundJobStatusVisible
        {
            get => this.isBackgroundJobStatusVisible;
            private set
            {
                this.isBackgroundJobStatusVisible = value;
                RaisePropertyChange();
                RaisePropertyChange("BackgroundJobStatus");
            }
        }

        public string BackgroundJobStatus
        {
            get
            {
                var count = this.backgroundJobs.Count();
                if (count == 0)
                {
                    return null;
                }
                else if (count == 1)
                {
                    return this.backgroundJobs.First().Description.StatusMessage;
                }
                else
                {
                    return this.backgroundJobs.First().Description.StatusMessage +
                        $" (+{count - 1} more background jobs)";
                }
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public IJobUserFeedback CreateBackgroundJob(
            JobDescription jobDescription,
            CancellationTokenSource cancellationSource)
        {
            return new BackgroundJob(this, jobDescription, cancellationSource);
        }

        public void CancelBackgroundJobs()
        {
            // NB. Use ToList to create a snapshot of the list because Cancel() 
            // modifies the list while we are iterating it.
            foreach (var job in this.backgroundJobs.ToList())
            {
                job.Cancel();
            }
        }

        //---------------------------------------------------------------------
        // Helper classes.
        //---------------------------------------------------------------------

        private class BackgroundJob : IJobUserFeedback
        {
            private readonly MainFormViewModel viewModel;
            private readonly CancellationTokenSource cancellationSource;
            public JobDescription Description { get; }

            public bool IsShowing => true;

            public BackgroundJob(
                MainFormViewModel viewModel, 
                JobDescription jobDescription,
                CancellationTokenSource cancellationSource)
            {
                this.viewModel = viewModel;
                this.Description = jobDescription;
                this.cancellationSource = cancellationSource;
            }

            public void Cancel()
            {
                this.cancellationSource.Cancel();
                Finish();
            }

            public void Finish()
            {
                this.viewModel.backgroundJobs.Remove(this);
                this.viewModel.IsBackgroundJobStatusVisible 
                    = this.viewModel.backgroundJobs.Any();
            }

            public void Start()
            {
                this.viewModel.backgroundJobs.AddLast(this);
                this.viewModel.IsBackgroundJobStatusVisible = true;
            }
        }
    }
}
