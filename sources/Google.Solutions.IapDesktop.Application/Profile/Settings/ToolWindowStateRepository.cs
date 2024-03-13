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

using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Settings.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Profile.Settings
{
    /// <summary>
    /// Repository for storing tool window states. Because there is only a single
    /// setting per tool window (the window state), all settings are stored underneath
    /// a single registry key.
    /// </summary>
    public class ToolWindowStateRepository : IDisposable
    {
        private readonly RegistryKey baseKey;

        public ToolWindowStateRepository(RegistryKey baseKey)
        {
            this.baseKey = baseKey;
        }

        public ToolWindowStateSettings GetSetting(
            string toolWindowName,
            DockState defaultState)
        {
            return ToolWindowStateSettings.FromKey(
                toolWindowName,
                defaultState,
                this.baseKey);
        }

        public void SetSetting(ToolWindowStateSettings settings)
        {
            if (settings.DockState.IsDirty)
            {
                settings.DockState.Save(this.baseKey);
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.baseKey.Dispose();
            }
        }
    }

    public class ToolWindowStateSettings : ISettingsCollection
    {
        public RegistryEnumSetting<DockState> DockState { get; private set; }

        public IEnumerable<ISetting> Settings => new[]
        {
            this.DockState
        };

        private ToolWindowStateSettings()
        {
        }

        public static ToolWindowStateSettings FromKey(
            string windowName,
            DockState defaultState,
            RegistryKey registryKey)
        {
            Debug.Assert(!windowName.Contains(' '));

            return new ToolWindowStateSettings()
            {
                DockState = RegistryEnumSetting<DockState>.FromKey(
                    windowName,
                    null,
                    null,
                    null,
                    defaultState,
                    registryKey)
            };
        }
    }
}
