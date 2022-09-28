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
using Google.Solutions.IapDesktop.Application.Data;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Application.Services.ProjectModel
{
    public interface IProjectModelNode
    {
        string DisplayName { get; }
    }

    public interface IProjectModelCloudNode : IProjectModelNode
    {
        IEnumerable<IProjectModelProjectNode> Projects { get; }
    }

    public interface IProjectModelProjectNode : IProjectModelNode
    {
        bool IsAccesible { get; }
        ProjectLocator Project { get; }
    }

    public interface IProjectModelZoneNode : IProjectModelNode
    {
        ZoneLocator Zone { get; }

        IEnumerable<IProjectModelInstanceNode> Instances { get; }
    }

    public interface IProjectModelInstanceNode : IProjectModelNode
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
    }
}
