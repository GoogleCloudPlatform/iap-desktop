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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.ProjectModel
{
    internal class CloudNode : IProjectExplorerCloudNode
    {
        public string DisplayName => "Google Cloud";
    }

    internal class ProjectNode : IProjectExplorerProjectNode
    {
        //---------------------------------------------------------------------
        // Readonly properties.
        //---------------------------------------------------------------------

        public ProjectLocator Project { get; }

        public string DisplayName { get; }

        public IEnumerable<IProjectExplorerZoneNode> Zones { get; }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public ProjectNode(
            ProjectLocator locator,
            string displayName,
            IEnumerable<IProjectExplorerZoneNode> zones)
        {
            this.Project = locator;
            this.DisplayName = displayName;
            this.Zones = zones;
        }

        //---------------------------------------------------------------------
        // Factory.
        //---------------------------------------------------------------------

        internal static ProjectNode FromProject(
            Project project,
            IEnumerable<Instance> instances)
        {
            var zoneLocators = instances
                .EnsureNotNull()
                .Select(i => ZoneLocator.FromString(i.Zone))
                .ToHashSet();

            var zones = new List<ZoneNode>();
            foreach (var zoneLocator in zoneLocators.OrderBy(z => z.Name))
            {
                var instancesInZone = instances
                    .Where(i => ZoneLocator.FromString(i.Zone) == zoneLocator)
                    .Where(i => i.Disks != null && i.Disks.Any())
                    .OrderBy(i => i.Name)
                    .Select(i => new InstanceNode(
                        i.Id.Value,
                        new InstanceLocator(
                            zoneLocator.ProjectId,
                            zoneLocator.Name,
                            i.Name),
                        i.IsWindowsInstance()
                            ? OperatingSystems.Windows
                            : OperatingSystems.Linux,
                        i.Status == "RUNNING"))
                    .ToList();

                zones.Add(new ZoneNode(
                    zoneLocator,
                    instancesInZone));
            }

            return new ProjectNode(
                new ProjectLocator(project.Name),
                project.Description,
                zones);
        }
    }

    internal class ZoneNode : IProjectExplorerZoneNode
    {
        //---------------------------------------------------------------------
        // Readonly properties.
        //---------------------------------------------------------------------

        public ZoneLocator Zone { get; }

        public string DisplayName
            => this.Zone.Name;

        public IEnumerable<IProjectExplorerInstanceNode> Instances { get; }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public ZoneNode(
            ZoneLocator locator,
            IEnumerable<IProjectExplorerInstanceNode> instances)
        {
            this.Zone = locator;
            this.Instances = instances;
        }
    }

    internal class InstanceNode : IProjectExplorerInstanceNode
    {
        //---------------------------------------------------------------------
        // Readonly properties.
        //---------------------------------------------------------------------

        public ulong InstanceId { get; }

        public InstanceLocator Instance { get; }

        public OperatingSystems OperatingSystem { get; }
        public bool IsRunning { get; }

        public bool IsWindowsInstance
            => this.OperatingSystem.HasFlag(OperatingSystems.Windows);

        public string DisplayName
            => this.Instance.Name;

        //---------------------------------------------------------------------
        // Mutable properties.
        //---------------------------------------------------------------------


        public bool IsConnected { get; set; }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public void Select()
        {
            // TODO: Remove this call
            throw new NotImplementedException();
        }

        public InstanceNode(
            ulong instanceId,
            InstanceLocator locator,
            OperatingSystems os,
            bool isRunning)
        {
            this.InstanceId = instanceId;
            this.Instance = locator;
            this.OperatingSystem = os;
            this.IsRunning = isRunning;
        }
    }
}
