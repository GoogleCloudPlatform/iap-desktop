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
using Google.Solutions.IapDesktop.Application.Services.Windows.ConnectionSettings;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer
{
    public interface IProjectExplorerNode
    {
        string DisplayName { get; }
    }

    public interface IProjectExplorerCloudNode : IProjectExplorerNode
    {
    }

    public interface IProjectExplorerNodeWithSettings : IProjectExplorerCloudNode
    {
        ConnectionSettingsEditor SettingsEditor { get; }
    }

    public interface IProjectExplorerProjectNode : IProjectExplorerNode, IProjectExplorerNodeWithSettings
    {
        string ProjectId { get; }

        IEnumerable<IProjectExplorerZoneNode> Zones { get; }
    }

    public interface IProjectExplorerZoneNode : IProjectExplorerNode, IProjectExplorerNodeWithSettings
    {
        string ProjectId { get; }
        string ZoneId { get; }
        IEnumerable<IProjectExplorerVmInstanceNode> Instances { get; }
    }

    public interface IProjectExplorerVmInstanceNode : IProjectExplorerNode, IProjectExplorerNodeWithSettings
    {
        ulong InstanceId { get; }
        string ProjectId { get; }
        string ZoneId { get; }
        string InstanceName { get; }

        InstanceLocator Reference { get; }

        bool IsRunning { get; }
        bool IsConnected { get; }

        void Select();
    }
}
