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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.ProjectModel.Nodes
{
    internal class InstanceNode : IProjectModelInstanceNode
    {
        //---------------------------------------------------------------------
        // IProjectModelInstanceNode.
        //---------------------------------------------------------------------

        public ulong InstanceId { get; }

        public InstanceLocator Instance { get; }

        public bool IsRunning { get; }
        public bool CanStart { get; }
        public bool CanStop { get; }
        public bool CanSuspend { get; }
        public bool CanResume { get; }
        public bool CanReset { get; }
        public string DisplayName => this.Instance.Name;

        public OperatingSystems OperatingSystem
        {
            get
            {
                return this.Traits.Contains(WindowsTrait.Instance)
                    ? OperatingSystems.Windows
                    : OperatingSystems.Linux;
            }
        }

        //---------------------------------------------------------------------
        // IProtocolTarget.
        //---------------------------------------------------------------------

        public string TargetName => this.Instance.Name;
        public IEnumerable<IProtocolTargetTrait> Traits { get; }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public InstanceNode(
            ulong instanceId,
            InstanceLocator locator,
            IReadOnlyCollection<IProtocolTargetTrait> traits,
            string status)
        {
            locator.ExpectNotNull(nameof(locator));
            traits.ExpectNotNull(nameof(traits));
            status.ExpectNotNull(nameof(status));

            Debug.Assert(status.All(char.IsUpper));

            this.InstanceId = instanceId;
            this.Instance = locator;
            this.Traits = traits;

            this.IsRunning = status == "RUNNING";

            //
            // See https://cloud.google.com/compute/docs/instances/instance-life-cycle.
            //
            this.CanStart = status == "TERMINATED";
            this.CanResume = status == "SUSPENDED";
            this.CanSuspend =
                this.CanReset =
                this.CanStop = status == "RUNNING" || status == "REPAIRING";
        }
    }
}
