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

using Google.Apis.Util;
using Google.Solutions.IapDesktop.Application.Registry;
using Microsoft.Win32;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    /// <summary>
    /// Registry-backed repository for app settings.
    /// </summary>
    public class ApplicationSettingsRepository : SettingsRepositoryBase<ApplicationSettings>
    {
        public ApplicationSettingsRepository(RegistryKey baseKey) : base(baseKey)
        {
            Utilities.ThrowIfNull(baseKey, nameof(baseKey));
        }
    }

    public class ApplicationSettings
    {
        [BoolRegistryValue("IsMainWindowMaximized")]
        public bool IsMainWindowMaximized { get; set; }

        [DwordRegistryValue("MainWindowHeight")]
        public int MainWindowHeight { get; set; }

        [DwordRegistryValue("WindowWidth")]
        public int MainWindowWidth { get; set; }

        [BoolRegistryValue("IsUpdateCheckEnabled")]
        public bool IsUpdateCheckEnabled { get; set; } = true;

        [QwordRegistryValue("LastUpdateCheck")]
        public long LastUpdateCheck { get; set; }
    }
}
