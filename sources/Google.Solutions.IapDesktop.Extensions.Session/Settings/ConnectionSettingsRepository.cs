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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.Settings;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

        public ConnectionSettings GetProjectSettings(ProjectLocator project)
        {
            using (var key = this.projectRepository.OpenRegistryKey(project.Name))
            {
                if (key == null)
                {
                    throw new KeyNotFoundException(project.Name);
                }

                return new ConnectionSettings(project, key);
            }
        }

        public void SetProjectSettings(ConnectionSettings settings)
        {
            if (!(settings.Resource is ProjectLocator project))
            {
                throw new ArgumentException(nameof(settings));
            }

            using (var key = new RegistrySettingsStore(
                this.projectRepository.OpenRegistryKey(project.ProjectId)))
            {
                if (key == null)
                {
                    throw new KeyNotFoundException(project.ProjectId);
                }

                foreach (var setting in settings.Settings.Where(s => s.IsDirty))
                {
                    key.Write(setting);
                }
            }
        }

        //---------------------------------------------------------------------
        // Zones.
        //---------------------------------------------------------------------

        public ConnectionSettings GetZoneSettings(ZoneLocator zone)
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
                    .OverlayBy(new ConnectionSettings(zone, key));
            }
        }

        public void SetZoneSettings(ConnectionSettings settings)
        {
            if (!(settings.Resource is ZoneLocator zone))
            {
                throw new ArgumentException(nameof(settings));
            }

            using (var key = new RegistrySettingsStore(
                this.projectRepository.OpenRegistryKey(
                    zone.ProjectId,
                    ZonePrefix + zone.Name,
                    true)))
            {
                foreach (var setting in settings.Settings.Where(s => s.IsDirty))
                {
                    key.Write(setting);
                }
            }
        }

        //---------------------------------------------------------------------
        // Virtual Machines.
        //---------------------------------------------------------------------

        public ConnectionSettings GetInstanceSettings(InstanceLocator instance)
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
                    .OverlayBy(new ConnectionSettings(instance, key));
            }
        }

        public void SetInstanceSettings(ConnectionSettings settings)
        {
            if (!(settings.Resource is InstanceLocator instance))
            {
                throw new ArgumentException(nameof(settings));
            }

            using (var key = new RegistrySettingsStore(
                this.projectRepository.OpenRegistryKey(
                    instance.ProjectId,
                    VmPrefix + instance.Name,
                    true)))
            {
                foreach (var setting in settings.Settings.Where(s => s.IsDirty))
                {
                    key.Write(setting);
                }
            }
        }
    }
}
