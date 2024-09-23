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

using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ProjectModel
{
    /// <summary>
    /// Base interfaces for nodes.
    /// </summary>
    public interface IProjectModelNode
    {
        string DisplayName { get; }
    }

    /// <summary>
    /// Represents a virtual "root node" for the project inventory.
    /// </summary>
    public interface IProjectModelCloudNode : IProjectModelNode
    {
        /// <summary>
        /// Google Cloud organizations.
        /// </summary>
        IEnumerable<IProjectModelOrganizationNode> Organizations { get; }
    }

    /// <summary>
    /// Represents a Google Cloud organization.
    /// </summary>
    public interface IProjectModelOrganizationNode : IProjectModelNode // TODO: Drop prefix?
    {
        /// <summary>
        /// List of projects that are currently loaded. Some
        /// projects might be inaccessible.
        /// </summary>
        IEnumerable<IProjectModelProjectNode> Projects { get; }

        /// <summary>
        /// Organization locator.
        /// </summary>
        OrganizationLocator Organization { get; }
    }

    /// <summary>
    /// Represents a project.
    /// </summary>
    public interface IProjectModelProjectNode : IProjectModelNode
    {
        /// <summary>
        /// Indicates whether additional details about this
        /// project are accessible.
        /// </summary>
        bool IsAccesible { get; }

        /// <summary>
        /// Project locator.
        /// </summary>
        ProjectLocator Project { get; }
    }

    /// <summary>
    /// Represents a zone within a project.
    /// </summary>
    public interface IProjectModelZoneNode : IProjectModelNode
    {
        /// <summary>
        /// Zone locator.
        /// </summary>
        ZoneLocator Zone { get; }

        /// <summary>
        /// List of instances in this zone.
        /// </summary>
        IEnumerable<IProjectModelInstanceNode> Instances { get; }
    }

    /// <summary>
    /// Represents an instance.
    /// </summary>
    public interface IProjectModelInstanceNode : IProjectModelNode, IProtocolTarget
    {
        /// <summary>
        /// Unique instance ID.
        /// </summary>
        ulong InstanceId { get; }

        /// <summary>
        /// Locator.
        /// </summary>
        InstanceLocator Instance { get; }

        /// <summary>
        /// Operating system (best guess).
        /// </summary>
        OperatingSystems OperatingSystem { get; }

        /// <summary>
        /// Check if instance is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Check if VM is in a status that permits starting. Does
        /// not perform any permission checks.
        /// </summary>
        bool CanStart { get; }

        /// <summary>
        /// Check if VM is in a status that permits stopping. Does
        /// not perform any permission checks.
        /// </summary>
        bool CanStop { get; }

        /// <summary>
        /// Check if VM is in a status that permits suspending. Does
        /// not perform any permission checks.
        /// </summary>
        bool CanSuspend { get; }

        /// <summary>
        /// Check if VM is in a status that permits resuming. Does
        /// not perform any permission checks.
        /// </summary>
        bool CanResume { get; }

        /// <summary>
        /// Check if VM is in a status that permits resetting. Does
        /// not perform any permission checks.
        /// </summary>
        bool CanReset { get; }

        /// <summary>
        /// Start, stop, or otherwise control the lifecycle of an instance
        /// and notify other services.
        /// </summary>
        Task ControlInstanceAsync(
            InstanceControlCommand command,
            CancellationToken cancellationToken);
    }
}
