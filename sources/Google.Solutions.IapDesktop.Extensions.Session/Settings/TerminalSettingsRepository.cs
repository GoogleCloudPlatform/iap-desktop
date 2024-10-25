//
// Copyright 2024 Google LLC
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

using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Settings;
using Microsoft.Win32;
using System;
using System.Drawing;
using Google.Solutions.Common.Util;

namespace Google.Solutions.IapDesktop.Extensions.Session.Settings
{
    public interface ITerminalSettingsRepository : IRepository<ITerminalSettings>
    {
        event EventHandler<EventArgs<ITerminalSettings>> SettingsChanged;
    }

    /// <summary>
    /// Registry-backed repository for terminal settings.
    /// 
    /// Service is a singleton so that objects can subscribe to events.
    /// </summary>
    [Service(typeof(ITerminalSettingsRepository), ServiceLifetime.Singleton)]
    public class TerminalSettingsRepository
        : RepositoryBase<ITerminalSettings>, ITerminalSettingsRepository
    {
        public event EventHandler<EventArgs<ITerminalSettings>>? SettingsChanged;

        public TerminalSettingsRepository(RegistryKey key)
            : this(new RegistrySettingsStore(key))
        {
        }

        public TerminalSettingsRepository(ISettingsStore store) : base(store)
        {
        }

        public TerminalSettingsRepository(UserProfile profile)
            : this(new RegistrySettingsStore(profile
                .ExpectNotNull(nameof(profile))
                .SettingsKey
                .CreateSubKey("Terminal")))
        {
        }

        protected override ITerminalSettings LoadSettings(ISettingsStore store)
            => new TerminalSettings(store);

        public override void SetSettings(ITerminalSettings settings)
        {
            base.SetSettings(settings);
            this.SettingsChanged?.Invoke(this, new EventArgs<ITerminalSettings>(settings));
        }
    }
}