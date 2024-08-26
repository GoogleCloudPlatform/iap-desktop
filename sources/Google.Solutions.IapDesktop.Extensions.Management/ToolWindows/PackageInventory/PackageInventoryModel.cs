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

using Google.Solutions.Apis;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.PackageInventory
{
    public class PackageInventoryModel
    {
        public bool IsInventoryDataAvailable { get; }
        public string DisplayName { get; }
        public IEnumerable<PackageInventoryModel.Item> Packages { get; }

        private PackageInventoryModel(
            string displayName,
            bool isInventoryDataAvailable,
            IEnumerable<PackageInventoryModel.Item> packages)
        {
            this.DisplayName = displayName;
            this.IsInventoryDataAvailable = isInventoryDataAvailable;
            this.Packages = packages;
        }

        private static PackageInventoryModel FromInventory(
            string displayName,
            PackageInventoryType inventoryType,
            IEnumerable<GuestOsInfo> inventory)
        {
            return inventoryType switch
            {
                PackageInventoryType.AvailablePackages => new PackageInventoryModel(
                    displayName,
                    inventory.Any(),
                    inventory
                        .Where(i => i.AvailablePackages != null)
                        .SelectMany(i => i.AvailablePackages
                            .AllPackages
                            .Select(p => new Item(i.Instance, p)))),

                PackageInventoryType.InstalledPackages => new PackageInventoryModel(
                    displayName,
                    inventory.Any(),
                    inventory
                        .Where(i => i.InstalledPackages != null)
                        .SelectMany(i => i.InstalledPackages
                            .AllPackages
                            .Select(p => new Item(i.Instance, p)))),

                _ => throw new ArgumentException(nameof(inventoryType)),
            };
        }

        public static async Task<PackageInventoryModel?> LoadAsync(
            IGuestOsInventory packageInventory,
            PackageInventoryType inventoryType,
            IProjectModelNode node,
            CancellationToken token)
        {
            IEnumerable<GuestOsInfo> inventory;
            try
            {
                if (node is IProjectModelInstanceNode vmNode)
                {
                    var info = await packageInventory.GetInstanceInventoryAsync(
                            vmNode.Instance,
                            token)
                        .ConfigureAwait(false);
                    inventory = info != null
                        ? new GuestOsInfo[] { info }
                        : Enumerable.Empty<GuestOsInfo>();
                }
                else if (node is IProjectModelZoneNode zoneNode)
                {
                    inventory = await packageInventory.ListZoneInventoryAsync(
                            new ZoneLocator(zoneNode.Zone.ProjectId, zoneNode.Zone.Name),
                            OperatingSystems.Windows,
                            token)
                        .ConfigureAwait(false);
                }
                else if (node is IProjectModelProjectNode projectNode)
                {
                    inventory = await packageInventory.ListProjectInventoryAsync(
                            projectNode.Project.ProjectId,
                            OperatingSystems.Windows,
                            token)
                        .ConfigureAwait(false);
                }
                else
                {
                    // Unknown/unsupported node.
                    return null;
                }
            }
            catch (Exception e) when (e.Unwrap() is GoogleApiException apiEx &&
                apiEx.IsConstraintViolation())
            {
                //
                // Reading OS inventory data can fail because of a 
                // `compute.disableGuestAttributesAccess` constraint.
                //
                ApplicationTraceSource.Log.TraceWarning(
                    "Failed to load OS inventory data: {0}", e);

                inventory = Enumerable.Empty<GuestOsInfo>();
            }

            return PackageInventoryModel.FromInventory(
                node.DisplayName,
                inventoryType,
                inventory);
        }

        public class Item
        {
            public InstanceLocator Instance { get; }
            public IPackage Package { get; }

            public Item(InstanceLocator instance, IPackage package)
            {
                this.Instance = instance;
                this.Package = package;
            }
        }
    }
}
