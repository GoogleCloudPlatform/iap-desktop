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
using Google.Solutions.Common.Text;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface IComputeEngineAdapter : IDisposable
    {
        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        Task<Project> GetProjectAsync(
            string projectId,
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Instances.
        //---------------------------------------------------------------------

        Task<Instance> GetInstanceAsync(
            InstanceLocator instanceLocator,
            CancellationToken cancellationToken);

        Task<IEnumerable<Instance>> ListInstancesAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<IEnumerable<Instance>> ListInstancesAsync(
            ZoneLocator zoneLocator,
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Guest attributes.
        //---------------------------------------------------------------------

        Task<GuestAttributes> GetGuestAttributesAsync(
            InstanceLocator instanceLocator,
            string queryPath,
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Nodes.
        //---------------------------------------------------------------------

        Task<IEnumerable<NodeGroup>> ListNodeGroupsAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<IEnumerable<NodeGroupNode>> ListNodesAsync(
            ZoneLocator zone,
            string nodeGroup,
            CancellationToken cancellationToken);

        Task<IEnumerable<NodeGroupNode>> ListNodesAsync(
            string projectId,
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Disks/images.
        //---------------------------------------------------------------------

        Task<IEnumerable<Disk>> ListDisksAsync(
            string projectId,
            CancellationToken cancellationToken);

        Task<Image> GetImageAsync(
            ImageLocator image,
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Serial port.
        //---------------------------------------------------------------------

        IAsyncReader<string> GetSerialPortOutput(
            InstanceLocator instanceRef,
            ushort portNumber);

        //---------------------------------------------------------------------
        // Windows user.
        //---------------------------------------------------------------------

        Task<NetworkCredential> ResetWindowsUserAsync(
            InstanceLocator instanceRef,
            string username,
            CancellationToken token);

        Task<NetworkCredential> ResetWindowsUserAsync(
            InstanceLocator instanceRef,
            string username,
            TimeSpan timeout,
            CancellationToken token);

        //---------------------------------------------------------------------
        // Permission check.
        //---------------------------------------------------------------------

        Task<bool> IsGrantedPermission(
            InstanceLocator instanceRef,
            string permission);

        Task<bool> IsGrantedPermissionToResetWindowsUser(
            InstanceLocator instanceRef);
    }
}
