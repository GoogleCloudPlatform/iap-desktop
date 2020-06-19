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
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.SerialOutput
{
    internal class SerialOutputViewModel
        : ModelCachingViewModelBase<IProjectExplorerNode, SerialOutputModel>
    {
        private const int ModelCacheCapacity = 5;

        public static readonly ReadOnlyCollection<SerialPort> AvailablePorts
            = new ReadOnlyCollection<SerialPort>(new List<SerialPort>()
        {
            new SerialPort(1, "COM0 - Log"),
            new SerialPort(2, "COM1 - SAC"),
            new SerialPort(3, "COM2"),
            new SerialPort(4, "COM3")
        });


        private int selectedPortIndex = 0;
        private bool isPortComboBoxEnabled = false;

        public SerialOutputViewModel() 
            : base(ModelCacheCapacity)
        {
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------
        
        public int SelectedPortIndex
        {
            get => this.selectedPortIndex;
            set
            {
                this.selectedPortIndex = value;
                RaisePropertyChange();

                // TODO: Reload from backend.
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
                if (node is IProjectExplorerVmInstanceNode vmNode)
                {
                    // TODO
                    return null;
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
            if (this.Model == null)
            {
                // Unsupported node.
                this.IsPortComboBoxEnabled = false;
            }
            else
            {

            }
        }

        //---------------------------------------------------------------------

        public class SerialPort
        {
            public int Number { get; }

            public string Description { get; }

            public SerialPort(int number, string description)
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
