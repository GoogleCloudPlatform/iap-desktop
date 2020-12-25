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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#pragma warning disable IDE1006 // Naming Styles

namespace Google.Solutions.IapDesktop.Application.Views.ProjectExplorer
{
    internal class CloudNode : TreeNode, IProjectExplorerCloudNode
    {
        private const int IconIndex = 0;

        public CloudNode()
            : base("Google Cloud", IconIndex, IconIndex)
        {
        }

        public string DisplayName => this.Text;
    }

    [ComVisible(false)]
    public abstract class InventoryNode : TreeNode, IProjectExplorerNode
    {
        protected InventoryNode(
            string name,
            int iconIndex)
            : base(name, iconIndex, iconIndex)
        {
        }

        public string DisplayName => this.Text;

        internal static string ShortIdFromUrl(string url) => url.Substring(url.LastIndexOf("/") + 1);
    }

    [ComVisible(false)]
    public class ProjectNode : InventoryNode, IProjectExplorerProjectNode
    {
        private const int IconIndex = 1;

        public string ProjectId => this.Text;
        public IEnumerable<IProjectExplorerZoneNode> Zones
            => this.Nodes.OfType<IProjectExplorerZoneNode>();

        internal ProjectNode(string projectId)
            : base(projectId, IconIndex)
        {
        }

        public void Populate(
            IEnumerable<Instance> instances,
            Func<InstanceLocator, bool> isConnected)
        {
            this.Nodes.Clear();

            var zoneIds = instances.Select(i => InventoryNode.ShortIdFromUrl(i.Zone)).ToHashSet();

            foreach (var zoneId in zoneIds)
            {
                var zoneNode = new ZoneNode(zoneId, this);

                var instancesInZone = instances
                    .Where(i => InventoryNode.ShortIdFromUrl(i.Zone) == zoneId)
                    .OrderBy(i => i.Name);

                foreach (var instance in instancesInZone)
                {
                    var instanceNode = new VmInstanceNode(instance, zoneNode);
                    instanceNode.IsConnected = isConnected(
                        new InstanceLocator(this.ProjectId, zoneId, instance.Name));

                    zoneNode.Nodes.Add(instanceNode);
                }

                this.Nodes.Add(zoneNode);
                zoneNode.Expand();
            }

            Expand();
        }
    }

    [ComVisible(false)]
    public class ZoneNode : InventoryNode, IProjectExplorerZoneNode
    {
        private const int IconIndex = 3;

        public string ProjectId => ((ProjectNode)this.Parent).ProjectId;
        public string ZoneId => this.Text;
        public IEnumerable<IProjectExplorerVmInstanceNode> Instances
            => this.Nodes.OfType<VmInstanceNode>();

        public ZoneLocator Locator => new ZoneLocator(this.ProjectId, this.ZoneId);

        internal ZoneNode(
            string zoneId,
            ProjectNode parent)
            : base(zoneId, IconIndex)
        {
        }
    }

    [ComVisible(false)]
    public class VmInstanceNode : InventoryNode, IProjectExplorerVmInstanceNode
    {
        private const int DisconnectedIconIndex = 4;
        private const int ConnectedIconIndex = 5;
        private const int StoppedIconIndex = 6;

        public InstanceLocator Reference
            => new InstanceLocator(this.ProjectId, this.ZoneId, this.InstanceName);

        public string ProjectId => ((ZoneNode)this.Parent).ProjectId;
        public string ZoneId => ((ZoneNode)this.Parent).ZoneId;

        internal VmInstanceNode(
            Instance instance,
            ZoneNode parent)
            : base(
                instance.Name,
                DisconnectedIconIndex)
        {
            this.InstanceId = instance.Id.Value;
            this.IsRunning = instance.Status == "RUNNING";
        }

        public string InstanceName => this.Text;

        public ulong InstanceId { get; }

        public bool IsRunning { get; }

        public bool IsConnected
        {
            get => this.ImageIndex == ConnectedIconIndex;
            set
            {
                if (value)
                {
                    this.ImageIndex = this.SelectedImageIndex = ConnectedIconIndex;
                }
                else if (!IsRunning)
                {
                    this.ImageIndex = this.SelectedImageIndex = StoppedIconIndex;
                }
                else
                {
                    this.ImageIndex = this.SelectedImageIndex = DisconnectedIconIndex;
                }
            }
        }

        public void Select()
        {
            this.TreeView.SelectedNode = this;
        }
    }
}
