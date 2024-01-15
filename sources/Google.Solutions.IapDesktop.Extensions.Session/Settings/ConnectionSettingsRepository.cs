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
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Profile.Settings.Registry;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

        public ConnectionSettingsBase GetProjectSettings(ProjectLocator project)// TODO: test
        {
            using (var key = this.projectRepository.OpenRegistryKey(project.Name))
            {
                if (key == null)
                {
                    throw new KeyNotFoundException(project.Name);
                }

                return new ConnectionSettingsBase(project, key);
            }
        }

        public void SetProjectSettings(ConnectionSettingsBase settings)
        {
            if (!(settings.Resource is ProjectLocator project))
            {
                throw new ArgumentException(nameof(settings));
            }    

            using (var key = this.projectRepository.OpenRegistryKey(project.ProjectId))
            {
                if (key == null)
                {
                    throw new KeyNotFoundException(project.ProjectId);
                }

                settings.Save(key);
            }
        }

        //---------------------------------------------------------------------
        // Zones.
        //---------------------------------------------------------------------

        public ConnectionSettingsBase GetZoneSettings(ZoneLocator zone)// TODO: test
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                zone.ProjectId,
                ZonePrefix + zone.Name,
                true))
            {
                //
                // Return zone settings, applying project settings
                // as defaults.
                //
                return GetProjectSettings(zone.Project)
                    .OverlayBy(new ConnectionSettingsBase(zone, key));
            }
        }

        public void SetZoneSettings(ConnectionSettingsBase settings)
        {
            if (!(settings.Resource is ZoneLocator zone))
            {
                throw new ArgumentException(nameof(settings));
            }

            using (var key = this.projectRepository.OpenRegistryKey(
                zone.ProjectId,
                ZonePrefix + zone.Name,
                true))
            {
                settings.Save(key);
            }
        }

        //---------------------------------------------------------------------
        // Virtual Machines.
        //---------------------------------------------------------------------

        public ConnectionSettingsBase GetVmInstanceSettings(InstanceLocator instance) // TODO: test
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                instance.ProjectId,
                VmPrefix + instance.Name,
                true))
            {
                //
                // Return zone settings, applying zone settings
                // as defaults.
                //
                return GetZoneSettings(new ZoneLocator(instance.ProjectId, instance.Zone))
                    .OverlayBy(new ConnectionSettingsBase(instance, key));
            }
        }

        public void SetVmInstanceSettings(ConnectionSettingsBase settings)
        {
            if (!(settings.Resource is InstanceLocator instance))
            {
                throw new ArgumentException(nameof(settings));
            }

            using (var key = this.projectRepository.OpenRegistryKey(
                instance.ProjectId,
                VmPrefix + instance.Name,
                true))
            {
                settings.Save(key);
            }
        }
    }
}
