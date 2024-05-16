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
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Compute
{
    /// <summary>
    /// Client for Compute Engine (GCE) API.
    /// </summary>
    public interface IComputeEngineClient : IClient
    {
        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        Task<Project> GetProjectAsync(
            ProjectLocator projectId,
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Instances.
        //---------------------------------------------------------------------

        Task<Instance> GetInstanceAsync(
            InstanceLocator instanceLocator,
            CancellationToken cancellationToken);

        Task<IEnumerable<Instance>> ListInstancesAsync(
            ProjectLocator projectId,
            CancellationToken cancellationToken);

        Task<IEnumerable<Instance>> ListInstancesAsync(
            ZoneLocator zoneLocator,
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Guest attributes.
        //---------------------------------------------------------------------

        Task<GuestAttributes?> GetGuestAttributesAsync(
            InstanceLocator instanceLocator,
            string queryPath,
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Serial port.
        //---------------------------------------------------------------------

        IAsyncReader<string> GetSerialPortOutput(
            InstanceLocator instanceRef,
            ushort portNumber);

        //---------------------------------------------------------------------
        // Metadata.
        //---------------------------------------------------------------------

        Task UpdateMetadataAsync(
           InstanceLocator instanceRef,
           Action<Metadata> updateMetadata,
           CancellationToken token);

        Task UpdateCommonInstanceMetadataAsync(
            string projectId,
            Action<Metadata> updateMetadata,
            CancellationToken token);

        //---------------------------------------------------------------------
        // Control instance lifecycle.
        //---------------------------------------------------------------------

        Task ControlInstanceAsync(
           InstanceLocator instance,
           InstanceControlCommand command,
           CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Permission check.
        //---------------------------------------------------------------------

        /// <summary>
        /// Test if a permission has been granted.
        /// </summary>
        Task<bool> IsAccessGrantedAsync(
            InstanceLocator instanceRef,
            string permission);
    }

    public enum InstanceControlCommand
    {
        Start,
        Stop,
        Suspend,
        Resume,
        Reset
    }
}
