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
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Settings;
using System;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection
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
    public class RdpSettingsRepository
    {
        private const string ZonePrefix = "zone-";
        private const string VmPrefix = "vm-";

        private readonly IProjectRepository projectRepository;

        public RdpSettingsRepository(IProjectRepository projectRepository)
        {
            this.projectRepository = projectRepository;
        }

        public RdpSettingsRepository(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IProjectRepository>())
        {
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        public RdpProjectSettings GetProjectSettings(string projectId)
        {
            using (var key = this.projectRepository.OpenRegistryKey(projectId))
            {
                if (key == null)
                {
                    throw new KeyNotFoundException(projectId);
                }

                return RdpProjectSettings.FromKey(projectId, key);
            }
        }

        public void SetProjectSettings(RdpProjectSettings settings)
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

        public RdpZoneSettings GetZoneSettings(string projectId, string zoneId)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                projectId,
                ZonePrefix + zoneId,
                true))
            {
                return RdpZoneSettings.FromKey(
                    projectId,
                    zoneId,
                    key);
            }
        }

        public void SetZoneSettings(RdpZoneSettings settings)
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

        public RdpInstanceSettings GetVmInstanceSettings(string projectId, string instanceName)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                projectId,
                VmPrefix + instanceName,
                true))
            {
                return RdpInstanceSettings.FromKey(
                    projectId,
                    instanceName,
                    key);
            }
        }

        public void SetVmInstanceSettings(RdpInstanceSettings settings)
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
