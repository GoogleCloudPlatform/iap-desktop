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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Extensions.Os.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Os.Views.PackageInventory
{
    public class PackageInventoryModel
    {
        public string DisplayName { get; }
        public IEnumerable<PackageInventoryModel.Item> Packages { get; }

        private PackageInventoryModel(
            string displayName,
            IEnumerable<PackageInventoryModel.Item> packages)
        {
            this.DisplayName = displayName;
            this.Packages = packages;
        }

        public static PackageInventoryModel FromInventory(
            string displayName,
            PackageInventoryType inventoryType,
            IEnumerable<GuestOsInfo> inventory)
        {
            switch (inventoryType)
            {
                case PackageInventoryType.AvailablePackages:
                    return new PackageInventoryModel(
                        displayName,
                        inventory
                            .Where(i => i.AvailablePackages != null)
                            .SelectMany(i => i.AvailablePackages
                                .AllPackages
                                .Select(p => new Item(i.Instance, p))));


                case PackageInventoryType.InstalledPackages:
                    return new PackageInventoryModel(
                        displayName,
                        inventory
                            .Where(i => i.InstalledPackages != null)
                            .SelectMany(i => i.InstalledPackages
                                .AllPackages
                                .Select(p => new Item(i.Instance, p))));

                default:
                    throw new ArgumentException(nameof(inventoryType));

            }
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
