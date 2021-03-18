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
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings
{
    /// <summary>
    /// Registry-backed repository for terminal settings.
    /// 
    /// Service is a singleton so that objects can subscribe to events.
    /// </summary>
    [Service(ServiceLifetime.Singleton, ServiceVisibility.Global)]
    public class TerminalSettingsRepository : SettingsRepositoryBase<TerminalSettings>
    {
        public event EventHandler<EventArgs<TerminalSettings>> SettingsChanged;

        public TerminalSettingsRepository(RegistryKey baseKey) : base(baseKey)
        {
            Utilities.ThrowIfNull(baseKey, nameof(baseKey));
        }

        public TerminalSettingsRepository()
            : this(RegistryKey
                .OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
                .CreateSubKey($@"{Globals.BaseRegistryKeyPath}\Terminal"))
        {
        }

        protected override TerminalSettings LoadSettings(RegistryKey key)
            => TerminalSettings.FromKey(key);

        public override void SetSettings(TerminalSettings settings)
        {
            base.SetSettings(settings);
            this.SettingsChanged?.Invoke(this, new EventArgs<TerminalSettings>(settings));
        }
    }

    public class TerminalSettings : IRegistrySettingsCollection
    {
        public RegistryBoolSetting IsCopyPasteUsingCtrlCAndCtrlVEnabled { get; private set; }
        public RegistryBoolSetting IsSelectAllUsingCtrlAEnabled { get; private set; }
        public RegistryBoolSetting IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled { get; private set; }
        public RegistryBoolSetting IsSelectUsingShiftArrrowEnabled { get; private set; }
        public RegistryBoolSetting IsQuoteConvertionOnPasteEnabled { get; private set; }
        public RegistryBoolSetting IsNavigationUsingControlArrrowEnabled { get; private set; }
        public RegistryBoolSetting IsScrollingUsingCtrlUpDownEnabled { get; private set; }
        public RegistryBoolSetting IsScrollingUsingCtrlHomeEndEnabled { get; private set; }

        public IEnumerable<ISetting> Settings => new ISetting[]
        {
            IsCopyPasteUsingCtrlCAndCtrlVEnabled,
            IsSelectAllUsingCtrlAEnabled,
            IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled,
            IsSelectUsingShiftArrrowEnabled,
            IsQuoteConvertionOnPasteEnabled,
            IsNavigationUsingControlArrrowEnabled,
            IsScrollingUsingCtrlUpDownEnabled,
            IsScrollingUsingCtrlHomeEndEnabled
        };

        private TerminalSettings()
        {
        }

        public static TerminalSettings FromKey(RegistryKey registryKey)
        {
            return new TerminalSettings()
            {
                IsCopyPasteUsingCtrlCAndCtrlVEnabled = RegistryBoolSetting.FromKey(
                    "IsCopyPasteUsingCtrlCAndCtrlVEnabled",
                    "IsCopyPasteUsingCtrlCAndCtrlVEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsSelectAllUsingCtrlAEnabled = RegistryBoolSetting.FromKey(
                    "IsSelectAllUsingCtrlAEnabled",
                    "IsSelectAllUsingCtrlAEnabled",
                    null,
                    null,
                    false,
                    registryKey),
                IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled = RegistryBoolSetting.FromKey(
                    "IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled",
                    "IsCopyPasteUsingShiftInsertAndCtrlInsertEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsSelectUsingShiftArrrowEnabled = RegistryBoolSetting.FromKey(
                    "IsSelectUsingShiftArrrowEnabled",
                    "IsSelectUsingShiftArrrowEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsQuoteConvertionOnPasteEnabled = RegistryBoolSetting.FromKey(
                    "IsQuoteConvertionOnPasteEnabled",
                    "IsQuoteConvertionOnPasteEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsNavigationUsingControlArrrowEnabled = RegistryBoolSetting.FromKey(
                    "IsNavigationUsingControlArrrowEnabled",
                    "IsNavigationUsingControlArrrowEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsScrollingUsingCtrlUpDownEnabled = RegistryBoolSetting.FromKey(
                    "IsScrollingUsingCtrlUpDownEnabled",
                    "IsScrollingUsingCtrlUpDownEnabled",
                    null,
                    null,
                    true,
                    registryKey),
                IsScrollingUsingCtrlHomeEndEnabled = RegistryBoolSetting.FromKey(
                    "IsScrollingUsingCtrlHomeEndEnabled",
                    "IsScrollingUsingCtrlHomeEndEnabled",
                    null,
                    null,
                    true,
                    registryKey),
            };
        }
    }
}
