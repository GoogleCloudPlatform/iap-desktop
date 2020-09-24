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
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Settings;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection
{
    public interface IConnectionSettingsService
    {
        bool IsConnectionSettingsEditorAvailable(IProjectExplorerNode node);
        ISettingsEditor GetConnectionSettingsEditor(IProjectExplorerNode node);
    }

    [Service(typeof(IConnectionSettingsService))]
    public class ConnectionSettingsService : IConnectionSettingsService
    {
        private readonly ConnectionSettingsRepository settingsRepository;

        private ISettingsEditor GetProjectConnectionSettingsEditor(
            string projectId)
        {
            return new SettingsEditor(
                this.settingsRepository.GetProjectSettings(projectId),
                settings => settingsRepository.SetProjectSettings((ProjectConnectionSettings)settings));
        }

        private SettingsEditor GetZoneConnectionSettingsEditor(
            string projectId, 
            string zoneId)
        {
            var projectSettings = this.settingsRepository.GetProjectSettings(projectId);
            var zoneSettings = this.settingsRepository.GetZoneSettings(projectId, zoneId);

            return new SettingsEditor(
                projectSettings
                    .OverlayBy(zoneSettings),
                settings => settingsRepository.SetZoneSettings((ZoneConnectionSettings)settings));
        }

        private SettingsEditor GetVmInstanceConnectionSettingsEditor(
            string projectId,
            string zoneId,
            string instanceName)
        {
            var projectSettings = this.settingsRepository.GetProjectSettings(projectId);
            var zoneSettings = this.settingsRepository.GetZoneSettings(projectId, zoneId);
            var instanceSettings = this.settingsRepository.GetVmInstanceSettings(projectId, instanceName);

            return new SettingsEditor(
                projectSettings
                    .OverlayBy(zoneSettings)
                    .OverlayBy(instanceSettings),
                settings => this.settingsRepository.SetVmInstanceSettings(
                    (VmInstanceConnectionSettings)settings));
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public ConnectionSettingsService(
            ConnectionSettingsRepository settingsRepository)
        {
            this.settingsRepository = settingsRepository;
        }

        public ConnectionSettingsService(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<ConnectionSettingsRepository>())
        {
        }

        //---------------------------------------------------------------------
        // IConnectionSettingsService.
        //---------------------------------------------------------------------

        public bool IsConnectionSettingsEditorAvailable(IProjectExplorerNode node)
        {
            return node is IProjectExplorerProjectNode ||
                   node is IProjectExplorerZoneNode ||
                   node is IProjectExplorerVmInstanceNode;
        }

        public ISettingsEditor GetConnectionSettingsEditor(IProjectExplorerNode node)
        {
            if (node is IProjectExplorerProjectNode projectNode)
            {
                return GetProjectConnectionSettingsEditor(projectNode.ProjectId);
            }
            else if (node is IProjectExplorerZoneNode zoneNode)
            {
                return GetZoneConnectionSettingsEditor(zoneNode.ProjectId, zoneNode.ZoneId);
            }
            else if (node is IProjectExplorerVmInstanceNode vmNode)
            {
                return GetVmInstanceConnectionSettingsEditor(
                    vmNode.ProjectId,
                    vmNode.ZoneId,
                    vmNode.InstanceName);
            }
            else
            {
                throw new ArgumentException("Unsupported node type: " + node.GetType().Name);
            }
        }
    }
}
