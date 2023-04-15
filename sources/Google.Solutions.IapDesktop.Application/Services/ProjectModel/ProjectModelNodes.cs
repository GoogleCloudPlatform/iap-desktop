//
// Copyright 2021 Google LLC
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
using Google.Solutions.IapDesktop.Application.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Services.ProjectModel
{
    internal class CloudNode : IProjectModelCloudNode
    {
        //---------------------------------------------------------------------
        // Readonly properties.
        //---------------------------------------------------------------------

        public string DisplayName => "Google Cloud";

        public IEnumerable<IProjectModelProjectNode> Projects { get; }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public CloudNode(
            IEnumerable<IProjectModelProjectNode> projects)
        {
            this.Projects = projects;
        }
    }

    internal class ProjectNode : IProjectModelProjectNode
    {
        //---------------------------------------------------------------------
        // Readonly properties.
        //---------------------------------------------------------------------

        public ProjectLocator Project { get; }

        public string DisplayName { get; }

        public bool IsAccesible { get; }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public ProjectNode(
            ProjectLocator locator,
            bool accessible,
            string displayName)
        {
            this.Project = locator;
            this.IsAccesible = accessible;
            this.DisplayName = displayName;
        }
    }

    internal class ZoneNode : IProjectModelZoneNode
    {
        //---------------------------------------------------------------------
        // Readonly properties.
        //---------------------------------------------------------------------

        public ZoneLocator Zone { get; }

        public string DisplayName
            => this.Zone.Name;

        public IEnumerable<IProjectModelInstanceNode> Instances { get; }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public ZoneNode(
            ZoneLocator locator,
            IEnumerable<IProjectModelInstanceNode> instances)
        {
            this.Zone = locator;
            this.Instances = instances;
        }
    }

    internal class InstanceNode : IProjectModelInstanceNode
    {
        //---------------------------------------------------------------------
        // Readonly properties.
        //---------------------------------------------------------------------

        public ulong InstanceId { get; }

        public InstanceLocator Instance { get; }

        public OperatingSystems OperatingSystem { get; }

        public bool IsRunning { get; }

        public bool CanStart { get; }
        public bool CanStop { get; }
        public bool CanSuspend { get; }
        public bool CanResume { get; }
        public bool CanReset { get; }

        public string DisplayName
            => this.Instance.Name;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public InstanceNode(
            ulong instanceId,
            InstanceLocator locator,
            OperatingSystems os,
            string status)
        {
            Debug.Assert(!os.IsFlagCombination());
            Debug.Assert(status.All(char.IsUpper));

            this.InstanceId = instanceId;
            this.Instance = locator;
            this.OperatingSystem = os;

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
