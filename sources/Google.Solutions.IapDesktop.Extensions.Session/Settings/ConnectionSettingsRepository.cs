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
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Settings;
using System;
using System.Collections.Generic;
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

        private ISettingsStore CreateProjectSettingsStore(ProjectLocator project)
        {
            var key = this.projectRepository.OpenRegistryKey(project.Name);
            if (key == null)
            {
                throw new KeyNotFoundException(project.Name);
            }

            return new RegistrySettingsStore(key);
        }

        private ISettingsStore CreateZoneSettingsStore(ZoneLocator zone)
        {
            var key = this.projectRepository.OpenRegistryKey(
                zone.ProjectId,
                ZonePrefix + zone.Name,
                true);

            //
            // Return zone settings, applying project settings
            // as defaults.
            //
            return new MergedSettingsStore(new[]
                {
                    CreateProjectSettingsStore(zone.Project),
                    new RegistrySettingsStore(key)
                },
                MergedSettingsStore.MergeBehavior.Overlay);
        }

        private ISettingsStore CreateInstanceSettingsStore(InstanceLocator instance)
        {
            var key = this.projectRepository.OpenRegistryKey(
                instance.ProjectId,
                VmPrefix + instance.Name,
                true);

            //
            // Return instance settings, applying zone and
            // project settings as defaults.
            //
            return new MergedSettingsStore(new[]
                {
                    CreateProjectSettingsStore(instance.Project),
                    CreateZoneSettingsStore(new ZoneLocator(instance.ProjectId, instance.Zone)),
                    new RegistrySettingsStore(key)
                },
                MergedSettingsStore.MergeBehavior.Overlay);
        }

        private static void WriteAllSettings(
            ISettingsStore store,
            ConnectionSettings settings)
        {
            foreach (var setting in settings.Settings.Where(s => s.IsDirty))
            {
                store.Write(setting);
            }
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        public ConnectionSettings GetProjectSettings(ProjectLocator project)
        {
            using (var store = CreateProjectSettingsStore(project))
            {
                return new ConnectionSettings(project, store);
            }
        }

        public void SetProjectSettings(ConnectionSettings settings)
        {
            if (!(settings.Resource is ProjectLocator project))
            {
                throw new ArgumentException(nameof(settings));
            }

            using (var store = CreateProjectSettingsStore(project))
            {
                WriteAllSettings(store, settings);
            }
        }

        //---------------------------------------------------------------------
        // Zones.
        //---------------------------------------------------------------------

        public ConnectionSettings GetZoneSettings(ZoneLocator zone)
        {
            using (var store = CreateZoneSettingsStore(zone))
            {
                return new ConnectionSettings(zone, store);
            }
        }

        public void SetZoneSettings(ConnectionSettings settings)
        {
            if (!(settings.Resource is ZoneLocator zone))
            {
                throw new ArgumentException(nameof(settings));
            }

            using (var store = CreateZoneSettingsStore(zone))
            {
                WriteAllSettings(store, settings);
            }
        }

        //---------------------------------------------------------------------
        // Virtual Machines.
        //---------------------------------------------------------------------

        public ConnectionSettings GetInstanceSettings(InstanceLocator instance)
        {
            using (var store = CreateInstanceSettingsStore(instance))
            {
                return new ConnectionSettings(instance, store);
            }
        }

        public void SetInstanceSettings(ConnectionSettings settings)
        {
            if (!(settings.Resource is InstanceLocator instance))
            {
                throw new ArgumentException(nameof(settings));
            }

            using (var store = CreateInstanceSettingsStore(instance))
            {
                WriteAllSettings(store, settings);
            }
        }

        public ConnectionSettings GetInstanceSettings(
            IapRdpUrl url,
            out bool foundInInventory)
        {
            //
            // Populate an ephermeral settings store from the
            // URL parameters.
            //
            using (var urlSettingStore = new IapRdpUrlSettingsStore(url))
            {
                try
                {
                    //
                    // We have a full set of settings for this VM, so use
                    // that as basis and apply parameters from URL on top.
                    //
                    using (var storedSettingStore = CreateInstanceSettingsStore(url.Instance))
                    using (var mergedStore = new MergedSettingsStore(
                        new[] { storedSettingStore, urlSettingStore },
                        MergedSettingsStore.MergeBehavior.Overlay))
                    {
                        foundInInventory = true;
                        return new ConnectionSettings(url.Instance, mergedStore);
                    }
                }
                catch (KeyNotFoundException)
                {
                    //
                    // Project not found in inventory, all we have is the URL.
                    //
                    foundInInventory = false;
                    return new ConnectionSettings(url.Instance, urlSettingStore);
                }
            }
        }
    }
}
