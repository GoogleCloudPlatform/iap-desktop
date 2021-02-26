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
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.ConnectionSettings
{
    public interface IConnectionSettingsService
    {
        bool IsConnectionSettingsAvailable(IProjectExplorerNode node);
        IPersistentSettingsCollection<ConnectionSettingsBase> GetConnectionSettings(
            IProjectExplorerNode node);
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
                   (node is IProjectExplorerVmInstanceNode vmNode && vmNode.IsWindowsInstance);
        }

        public IPersistentSettingsCollection<ConnectionSettingsBase> GetConnectionSettings(
            IProjectExplorerNode node)
        {
            if (node is IProjectExplorerProjectNode projectNode)
            {
                return this.repository.GetProjectSettings(projectNode.ProjectId)
                    .ToPersistentSettingsCollection(s => this.repository.SetProjectSettings(s));
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
                    .OverlayBy(zoneSettings)
                    .ToPersistentSettingsCollection(s => this.repository.SetZoneSettings(s));
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
                    .OverlayBy(instanceSettings)
                    // TODO: Apply view
                    .ToPersistentSettingsCollection(s => this.repository.SetVmInstanceSettings(s));
            }
            else
            {
                throw new ArgumentException("Unsupported node type: " + node.GetType().Name);
            }
        }
    }
}
