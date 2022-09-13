﻿//
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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.Mvvm.Commands;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Views.SerialOutput
{
    internal class SerialOutputViewModel
        : ModelCachingViewModelBase<IProjectModelNode, SerialOutputModel>
    {
        private const int ModelCacheCapacity = 10;
        internal const string DefaultWindowTitle = "Serial log";

        internal CancellationTokenSource TailCancellationTokenSource = null;

        private readonly ushort serialPortNumber;
        private readonly IJobService jobService;
        private readonly Service<IComputeEngineAdapter> computeEngineAdapter;

        private bool isOutputBoxEnabled = false;
        private bool isEnableTailingButtonEnabled = false;
        private bool isTailEnabled = true;
        private bool isTailBlocked = true;
        private string windowTitle = DefaultWindowTitle;

        public event EventHandler<string> NewOutputAvailable;

        public SerialOutputViewModel(
            IServiceProvider serviceProvider,
            ushort serialPortNumber)
            : base(ModelCacheCapacity)
        {
            this.serialPortNumber = serialPortNumber;
            this.jobService = serviceProvider.GetService<IJobService>();
            this.computeEngineAdapter = serviceProvider.GetService<Service<IComputeEngineAdapter>>();
        }

        //---------------------------------------------------------------------
        // Tailing.
        //
        // There are two separate flags that determine whether tailing should
        // take place or not:
        //
        // (1) IsTailEnabled - set by the user, indicates whether he wants 
        //                     tailing or not.
        // (2) IsTailBlocked - determined by window state, indicates whether 
        //                     tailing is safe to do.
        //---------------------------------------------------------------------

        private void StartTailing()
        {
            if (this.Model == null)
            {
                return;
            }

            Debug.Assert(this.TailCancellationTokenSource == null);
            ApplicationTraceSources.Default.TraceVerbose("Start tailing");

            this.TailCancellationTokenSource = new CancellationTokenSource();
            this.Model.TailAsync(
                output => this.NewOutputAvailable?.Invoke(this, output),
                this.TailCancellationTokenSource.Token);
        }

        private void StopTailing()
        {
            if (this.TailCancellationTokenSource != null)
            {
                ApplicationTraceSources.Default.TraceVerbose("Stop tailing");
                this.TailCancellationTokenSource.Cancel();
                this.TailCancellationTokenSource = null;
            }
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public string Output => this.Model?.Output;

        public bool IsEnableTailingButtonEnabled
        {
            get => this.isEnableTailingButtonEnabled;
            set
            {
                this.isEnableTailingButtonEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsOutputBoxEnabled
        {
            get => this.isOutputBoxEnabled;
            set
            {
                this.isOutputBoxEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsTailEnabled
        {
            get => this.isTailEnabled;
            set
            {
                if (value && !this.isTailEnabled && !this.IsTailBlocked)
                {
                    // Only start tailing if this is a true status
                    // transition (disabled -> enabled) to avoid running
                    // multiple tail operations concurrently.
                    StartTailing();
                }
                else if (!value)
                {
                    // NB. Stopping multiple times is safe.
                    StopTailing();
                }

                this.isTailEnabled = value;
                RaisePropertyChange();
            }
        }

        public bool IsTailBlocked
        {
            get => this.isTailBlocked;
            set
            {
                if (!value && this.isTailBlocked && this.IsTailEnabled)
                {
                    // Only start tailing if this is a true status
                    // transition (blocked -> unblocked) to avoid running
                    // multiple tail operations concurrently.
                    StartTailing();
                }
                else if (value)
                {
                    // NB. Stopping multiple times is safe.
                    StopTailing();
                }

                this.isTailBlocked = value;
                RaisePropertyChange();
            }
        }

        public string WindowTitle
        {
            get => this.windowTitle;
            set
            {
                this.windowTitle = value;
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        //---------------------------------------------------------------------
        // ModelCachingViewModelBase.
        //---------------------------------------------------------------------

        public static CommandState GetCommandState(IProjectModelNode node)
        {
            if (node is IProjectModelInstanceNode vmNode)
            {
                return vmNode.IsRunning ? CommandState.Enabled : CommandState.Disabled;
            }
            else
            {
                return CommandState.Unavailable;
            }
        }

        protected override async Task<SerialOutputModel> LoadModelAsync(
            IProjectModelNode node,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(node))
            {
                if (node is IProjectModelInstanceNode vmNode && vmNode.IsRunning)
                {
                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    return await this.jobService.RunInBackground(
                        new JobDescription(
                            $"Reading serial port output for {vmNode.Instance.Name}",
                            JobUserFeedbackType.BackgroundFeedback),
                        async jobToken =>
                        {
                            using (var combinedTokenSource = jobToken.Combine(token))
                            {
                                return await SerialOutputModel.LoadAsync(
                                    vmNode.Instance.Name,
                                    this.computeEngineAdapter.CreateInstance(),
                                    vmNode.Instance,
                                    this.serialPortNumber,
                                    combinedTokenSource.Token)
                                .ConfigureAwait(false);
                            }
                        }).ConfigureAwait(true);  // Back to original (UI) thread.
                }
                else
                {
                    // Unknown/unsupported node.
                    return null;
                }
            }
        }

        protected override void ApplyModel(bool cached)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(this.Model, cached))
            {
                // Stop tailing the old model.
                this.IsTailBlocked = true;

                if (this.Model == null)
                {
                    // Unsupported node.
                    this.WindowTitle = DefaultWindowTitle + $" (COM{this.serialPortNumber})";
                    this.IsEnableTailingButtonEnabled =
                        this.IsOutputBoxEnabled = false;
                }
                else
                {
                    this.WindowTitle = DefaultWindowTitle +
                        $": {this.Model.DisplayName} (COM{this.serialPortNumber})";
                    this.IsEnableTailingButtonEnabled =
                        this.IsOutputBoxEnabled = true;
                }

                // Clear.
                RaisePropertyChange((SerialOutputViewModel m) => m.Output);

                // Begin tailing again (if it's enabled).
                this.IsTailBlocked = false;
            }
        }
    }
}
