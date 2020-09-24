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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Settings;
using System;

// TODO: Merge namespaces
namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection
{
    public interface IConnectionSettingsService
    {
        bool IsConnectionSettingsAvailable(IProjectExplorerNode node);
        ConnectionSettingsBase GetConnectionSettings(IProjectExplorerNode node);
        
        void SaveConnectionSettings(ConnectionSettingsBase settings);
    }

    [Service(typeof(IConnectionSettingsService))]
    public class ConnectionSettingsService : IConnectionSettingsService
    {
        private readonly ConnectionSettingsRepository repository;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public ConnectionSettingsService(
            ConnectionSettingsRepository settingsRepository)
        {
            this.repository = settingsRepository;
        }

        public ConnectionSettingsService(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<ConnectionSettingsRepository>())
        {
        }

        //---------------------------------------------------------------------
        // IConnectionSettingsService.
        //---------------------------------------------------------------------

        public bool IsConnectionSettingsAvailable(IProjectExplorerNode node)
        {
            return node is IProjectExplorerProjectNode ||
                   node is IProjectExplorerZoneNode ||
                   node is IProjectExplorerVmInstanceNode;
        }

        public ConnectionSettingsBase GetConnectionSettings(IProjectExplorerNode node)
        {
            if (node is IProjectExplorerProjectNode projectNode)
            {
                return this.repository.GetProjectSettings(projectNode.ProjectId);
            }
            else if (node is IProjectExplorerZoneNode zoneNode)
            {
                var projectSettings = this.repository.GetProjectSettings(
                    zoneNode.ProjectId);
                var zoneSettings = this.repository.GetZoneSettings(
                    zoneNode.ProjectId, 
                    zoneNode.ZoneId);

                // Apply overlay to get effective settings.
                return projectSettings
                    .OverlayBy(zoneSettings);
            }
            else if (node is IProjectExplorerVmInstanceNode vmNode)
            {
                var projectSettings = this.repository.GetProjectSettings(
                    vmNode.ProjectId);
                var zoneSettings = this.repository.GetZoneSettings(
                    vmNode.ProjectId,
                    vmNode.ZoneId);
                var instanceSettings = this.repository.GetVmInstanceSettings(
                    vmNode.ProjectId,
                    vmNode.InstanceName);

                // Apply overlay to get effective settings.
                return projectSettings
                    .OverlayBy(zoneSettings)
                    .OverlayBy(instanceSettings);
            }
            else
            {
                throw new ArgumentException("Unsupported node type: " + node.GetType().Name);
            }
        }

        public void SaveConnectionSettings(ConnectionSettingsBase settings)
        {
            if (settings is ProjectConnectionSettings projectSettings)
            {
                this.repository.SetProjectSettings(projectSettings);
            }
            else if (settings is ZoneConnectionSettings zoneSettings)
            {
                this.repository.SetZoneSettings(zoneSettings);
            }
            else if (settings is VmInstanceConnectionSettings instanceSettings)
            {
                this.repository.SetVmInstanceSettings(instanceSettings);
            }
            else
            {
                throw new ArgumentException("Unsupported settings type: " + settings.GetType().Name);
            }
        }
    }
}
