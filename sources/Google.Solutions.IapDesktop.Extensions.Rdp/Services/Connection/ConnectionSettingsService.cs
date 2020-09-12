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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection
{
    public interface IConnectionSettingsService
    {
        bool IsConnectionSettingsEditorAvailable(IProjectExplorerNode node);
        ConnectionSettingsEditor GetConnectionSettingsEditor(IProjectExplorerNode node);
    }

    [Service(typeof(IConnectionSettingsService))]
    public class ConnectionSettingsService : IConnectionSettingsService
    {
        private readonly ConnectionSettingsRepository settingsRepository;

        private ConnectionSettingsEditor GetProjectConnectionSettingsEditor(
            string projectId)
        {
            return new ConnectionSettingsEditor(
                this.settingsRepository.GetProjectSettings(projectId),
                settings => settingsRepository.SetProjectSettings((ProjectConnectionSettings)settings),
                null);
        }

        private ConnectionSettingsEditor GetZoneConnectionSettingsEditor(
            string projectId, 
            string zoneId)
        {
            return new ConnectionSettingsEditor(
                this.settingsRepository.GetZoneSettings(
                    projectId,
                    zoneId),
                settings => settingsRepository.SetZoneSettings(
                    projectId,
                    (ZoneConnectionSettings)settings),
                GetProjectConnectionSettingsEditor(projectId));
        }

        private ConnectionSettingsEditor GetVmInstanceConnectionSettingsEditor(
            string projectId,
            string zoneId,
            string instanceName)
        {
            return new ConnectionSettingsEditor(
                this.settingsRepository.GetVmInstanceSettings(
                    projectId,
                    instanceName),
                settings => this.settingsRepository.SetVmInstanceSettings(
                    projectId,
                    (VmInstanceConnectionSettings)settings),
                GetZoneConnectionSettingsEditor(
                    projectId, zoneId));
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

        public ConnectionSettingsEditor GetConnectionSettingsEditor(IProjectExplorerNode node)
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
