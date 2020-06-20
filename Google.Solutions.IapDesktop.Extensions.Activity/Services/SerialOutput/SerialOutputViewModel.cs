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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.SerialOutput
{
    internal class SerialOutputViewModel
        : ModelCachingViewModelBase<IProjectExplorerNode, SerialOutputModel>
    {
        private const int ModelCacheCapacity = 10;

        public static readonly ReadOnlyCollection<SerialPort> AvailablePorts
            = new ReadOnlyCollection<SerialPort>(new List<SerialPort>()
        {
            new SerialPort(1, "COM1 - Log"),
            new SerialPort(2, "COM2 - SAC"),
            new SerialPort(3, "COM3"),
            new SerialPort(4, "COM4")
        });

        private IServiceProvider serviceProvider;

        private int selectedPortIndex = 0;
        private bool isPortComboBoxEnabled = false;
        private bool isOutputBoxEnabled = false;

        private CancellationTokenSource tailCancellationTokenSource = null;
        private bool isTailEnabled = true;
        private bool isTailBlocked = true;

        public event EventHandler<string> NewOutputAvailable;

        public SerialOutputViewModel(IServiceProvider serviceProvider) 
            : base(ModelCacheCapacity)
        {
            this.serviceProvider = serviceProvider;
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

            Debug.Assert(this.tailCancellationTokenSource == null);
            TraceSources.IapDesktop.TraceVerbose("Start tailing");
            
            this.tailCancellationTokenSource = new CancellationTokenSource();
            this.Model.TailAsync(
                output => this.NewOutputAvailable?.Invoke(this, output),
                this.tailCancellationTokenSource.Token);
        }

        private void StopTailing()
        {
            if (this.tailCancellationTokenSource != null)
            {
                TraceSources.IapDesktop.TraceVerbose("Stop tailing");
                this.tailCancellationTokenSource.Cancel();
                this.tailCancellationTokenSource = null;
            }
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public string Output => this.Model?.Output;

        public int SelectedPortIndex
        {
            get => this.selectedPortIndex;
            set
            {
                this.selectedPortIndex = value;
                RaisePropertyChange();

                // TODO: interferes with cache
                // SwitchToModelAsync(this.ModelKey).ConfigureAwait(true);
            }
        }

        public bool IsPortComboBoxEnabled
        {
            get => this.isPortComboBoxEnabled;
            set
            {
                this.isPortComboBoxEnabled = value;
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

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        //---------------------------------------------------------------------
        // ModelCachingViewModelBase.
        //---------------------------------------------------------------------

        public static CommandState GetCommandState(IProjectExplorerNode node)
        {
            if (node is IProjectExplorerVmInstanceNode vmNode)
            {
                return vmNode.IsRunning ? CommandState.Enabled : CommandState.Disabled;
            }
            else
            {
                return CommandState.Unavailable;
            }
        }

        protected async override Task<SerialOutputModel> LoadModelAsync(
            IProjectExplorerNode node, 
            CancellationToken token)
        {
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(node))
            {
                if (node is IProjectExplorerVmInstanceNode vmNode && vmNode.IsRunning)
                {
                    var instanceLocator = new InstanceLocator(
                        vmNode.ProjectId,
                        vmNode.ZoneId,
                        vmNode.InstanceName);

                    // Load data using a job so that the task is retried in case
                    // of authentication issues.
                    var jobService = this.serviceProvider.GetService<IJobService>();
                    return await jobService.RunInBackground(
                        new JobDescription(
                            $"Reading serial port output for {vmNode.InstanceName}",
                            JobUserFeedbackType.BackgroundFeedback),
                        async jobToken =>
                        {
                            return await SerialOutputModel.LoadAsync(
                                this.serviceProvider.GetService<IComputeEngineAdapter>(),
                                instanceLocator,
                                AvailablePorts[this.SelectedPortIndex].Number,
                                token)  // TODO: combine tokens
                            .ConfigureAwait(false);
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
            using (TraceSources.IapDesktop.TraceMethod().WithParameters(this.Model, cached))
            {
                // Stop tailing the old model.
                this.IsTailBlocked = true;

                if (this.Model == null)
                {
                    // Unsupported node.
                    this.IsPortComboBoxEnabled = false;
                    this.IsOutputBoxEnabled = false;
                }
                else
                {
                    this.IsPortComboBoxEnabled = true;
                    this.IsOutputBoxEnabled = true;
                }

                // Clear.
                RaisePropertyChange((SerialOutputViewModel m) => m.Output);

                // Begin tailing again (if it's enabled).
                this.IsTailBlocked = false;
            }
        }

        //---------------------------------------------------------------------

        public class SerialPort
        {
            public ushort Number { get; }

            public string Description { get; }

            public SerialPort(ushort number, string description)
            {
                this.Number = number;
                this.Description = description;
            }

            public override string ToString()
            {
                return this.Description;
            }
        }
    }
}
