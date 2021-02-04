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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Os.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Os.Services.Inventory
{
    public interface IInventoryService : IDisposable
    {
        /// <summary>
        /// Get OS inventory data for instance.
        /// </summary>
        /// <returns>Inventory of null if data is not available</returns>
        Task<GuestOsInfo> GetInstanceInventoryAsync(
            InstanceLocator locator,
            CancellationToken token);

        /// <summary>
        /// Get OS inventory for instance all instances in zone
        /// </summary>
        /// <returns>Inventory for instances for which data is available</returns>
        Task<IEnumerable<GuestOsInfo>> ListZoneInventoryAsync(
            ZoneLocator locator,
            OperatingSystems operatingSystems,
            CancellationToken token);

        /// <summary>
        /// Get OS inventory for instance all instances in zone
        /// </summary>
        /// <returns>Inventory for instances for which data is available</returns>
        Task<IEnumerable<GuestOsInfo>> ListProjectInventoryAsync(
            string projectId,
            OperatingSystems operatingSystems,
            CancellationToken token);
    }

    [Flags]
    public enum OperatingSystems
    {
        Windows = 1,
        Linux = 2,
        All = Windows | Linux
    }

    [Service(typeof(IInventoryService))]
    public sealed class InventoryService : IInventoryService
    {
        private readonly IComputeEngineAdapter computeEngineAdapter;

        public InventoryService(IComputeEngineAdapter computeEngineAdapter)
        {
            this.computeEngineAdapter = computeEngineAdapter;
        }

        public InventoryService(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IComputeEngineAdapter>())
        {
        }

        private async Task<IEnumerable<GuestOsInfo>> ListInventoryAsync(
            IEnumerable<InstanceLocator> instanceLocators,
            CancellationToken token)
        {
            // There is no way to query guest attributes for multiple instances at one,
            // so we have to do it in a (parallel) loop.

            return await instanceLocators
                .SelectParallelAsync(
                    instanceLocator => GetInstanceInventoryAsync(
                        instanceLocator,
                        token))
                .ConfigureAwait(false);
        }

        private static bool IsRunningOperatingSystem(
            Instance instance,
            OperatingSystems operatingSystems)
        {
            switch (operatingSystems)
            {
                case OperatingSystems.Windows:
                    return instance.IsWindowsInstance();

                case OperatingSystems.Linux:
                    return !instance.IsWindowsInstance();

                default:
                    return true;
            }
        }

        //---------------------------------------------------------------------
        // IInventoryService.
        //---------------------------------------------------------------------

        public async Task<GuestOsInfo> GetInstanceInventoryAsync(
            InstanceLocator instanceLocator,
            CancellationToken token)
        {
            var guestAttributes = await this.computeEngineAdapter
                .GetGuestAttributesAsync(
                    instanceLocator,
                    GuestOsInfo.GuestAttributePath,
                    token)
                .ConfigureAwait(false);
            var guestAttributesList = guestAttributes?.QueryValue?.Items;

            return guestAttributesList != null
                ? GuestOsInfo.FromGuestAttributes(instanceLocator, guestAttributesList)
                : null;
        }

        public async Task<IEnumerable<GuestOsInfo>> ListProjectInventoryAsync(
            string projectId,
            OperatingSystems operatingSystems,
            CancellationToken token)
        {
            var instances = await this.computeEngineAdapter
                .ListInstancesAsync(projectId, token)
                .ConfigureAwait(false);

            return await ListInventoryAsync(
                    instances
                        .Where(i => IsRunningOperatingSystem(i, operatingSystems))
                        .Select(i => i.GetInstanceLocator()),
                    token)
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<GuestOsInfo>> ListZoneInventoryAsync(
            ZoneLocator locator,
            OperatingSystems operatingSystems,
            CancellationToken token)
        {
            var instances = await this.computeEngineAdapter
                .ListInstancesAsync(locator, token)
                .ConfigureAwait(false);

            return await ListInventoryAsync(
                    instances
                        .Where(i => IsRunningOperatingSystem(i, operatingSystems))
                        .Select(i => i.GetInstanceLocator()),
                    token)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.computeEngineAdapter.Dispose();
        }
    }
}
