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
        private readonly RegistrySettingsStore store;

        public ToolWindowStateRepository(RegistryKey baseKey)
        {
            this.store = new RegistrySettingsStore(baseKey);
        }

        public ToolWindowStateSettings GetSetting(
            string toolWindowName,
            DockState defaultState)
        {
            return new ToolWindowStateSettings(
                toolWindowName,
                defaultState,
                this.store);
        }

        public void SetSetting(ToolWindowStateSettings settings)
        {
            if (settings.DockState.IsDirty)
            {
                this.store.Write(settings.DockState);
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
            this.store.Dispose();
        }
    }

    public class ToolWindowStateSettings : ISettingsCollection
    {
        public ISetting<DockState> DockState { get; }

        public IEnumerable<ISetting> Settings => new[]
        {
            this.DockState
        };

        internal ToolWindowStateSettings(
            string windowName,
            DockState defaultState,
            ISettingsStore store)
        {
            Debug.Assert(!windowName.Contains(' '));

            this.DockState = store.Read<DockState>(
                windowName,
                null,
                null,
                null,
                defaultState);
        }
    }
}
