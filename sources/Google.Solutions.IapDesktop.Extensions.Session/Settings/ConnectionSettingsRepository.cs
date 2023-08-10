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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Profile.Settings.Registry;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Extensions.Session.Settings
{
    /// <summary>
    /// Registry-backed repository for connection settings.
    /// 
    /// To simplify key managent, a flat structure is used:
    /// 
    /// [base-key]
    ///    + [project-id]           => values...
    ///      + region-[region-id]   => values...
    ///      + zone-[zone-id]       => values...
    ///      + vm-[instance-name]   => values...
    ///      
    /// </summary>
    [Service]
    public class ConnectionSettingsRepository
    {
        private const string ZonePrefix = "zone-";
        private const string VmPrefix = "vm-";

        private readonly IProjectSettingsRepository projectRepository;

        public ConnectionSettingsRepository(IProjectSettingsRepository projectRepository)
        {
            this.projectRepository = projectRepository.ExpectNotNull(nameof(projectRepository));
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        public ProjectConnectionSettings GetProjectSettings(string projectId)
        {
            using (var key = this.projectRepository.OpenRegistryKey(projectId))
            {
                if (key == null)
                {
                    throw new KeyNotFoundException(projectId);
                }

                return ProjectConnectionSettings.FromKey(projectId, key);
            }
        }

        public void SetProjectSettings(ProjectConnectionSettings settings)
        {
            using (var key = this.projectRepository.OpenRegistryKey(settings.ProjectId))
            {
                if (key == null)
                {
                    throw new KeyNotFoundException(settings.ProjectId);
                }

                settings.Save(key);
            }
        }

        //---------------------------------------------------------------------
        // Zones.
        //---------------------------------------------------------------------

        public ZoneConnectionSettings GetZoneSettings(string projectId, string zoneId)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                projectId,
                ZonePrefix + zoneId,
                true))
            {
                return ZoneConnectionSettings.FromKey(
                    projectId,
                    zoneId,
                    key);
            }
        }

        public void SetZoneSettings(ZoneConnectionSettings settings)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                settings.ProjectId,
                ZonePrefix + settings.ZoneId,
                true))
            {
                settings.Save(key);
            }
        }

        //---------------------------------------------------------------------
        // Virtual Machines.
        //---------------------------------------------------------------------

        public InstanceConnectionSettings GetVmInstanceSettings(string projectId, string instanceName)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                projectId,
                VmPrefix + instanceName,
                true))
            {
                return InstanceConnectionSettings.FromKey(
                    projectId,
                    instanceName,
                    key);
            }
        }

        public void SetVmInstanceSettings(InstanceConnectionSettings settings)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                settings.ProjectId,
                VmPrefix + settings.InstanceName,
                true))
            {
                settings.Save(key);
            }
        }
    }
}
