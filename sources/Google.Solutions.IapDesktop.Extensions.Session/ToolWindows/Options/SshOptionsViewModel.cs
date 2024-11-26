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

using Google.Solutions.IapDesktop.Application.Windows.Options;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Ssh.Cryptography;
using System;

#nullable disable

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Options
{
    [Service(ServiceLifetime.Transient)]
    public class SshOptionsViewModel : OptionsViewModelBase<ISshSettings>
    {
        public SshOptionsViewModel(IRepository<ISshSettings> settingsRepository)
            : base("SSH", settingsRepository)
        {
            var settings = settingsRepository.GetSettings();

            this.PublicKeyType = ObservableProperty.Build(settings.PublicKeyType.Value);
            this.IsPublicKeyTypeEditable = !settings.PublicKeyType.IsReadOnly;

            this.UsePersistentKey = ObservableProperty.Build(settings.UsePersistentKey.Value);
            this.IsUsePersistentKeyEditable = ObservableProperty.Build(
                this.UsePersistentKey,
                usePersistent => !settings.UsePersistentKey.IsReadOnly);

            this.PublicKeyValidityInDays = ObservableProperty.Build(
                (decimal)TimeSpan.FromSeconds(settings.PublicKeyValidity.Value).TotalDays);
            this.IsPublicKeyValidityInDaysEditable = ObservableProperty.Build(
                this.UsePersistentKey,
                usePersistent => usePersistent && !settings.PublicKeyValidity.IsReadOnly);

            this.IsPropagateLocaleEnabled = ObservableProperty.Build(
                settings.PropagateLocale.Value);

            MarkDirtyWhenPropertyChanges(this.PublicKeyType);
            MarkDirtyWhenPropertyChanges(this.UsePersistentKey);
            MarkDirtyWhenPropertyChanges(this.PublicKeyValidityInDays);
            MarkDirtyWhenPropertyChanges(this.IsPropagateLocaleEnabled);

            base.OnInitializationCompleted();
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Save(ISshSettings settings)
        {
            settings.PublicKeyType.Value = this.PublicKeyType.Value;
            settings.UsePersistentKey.Value = this.UsePersistentKey.Value;
            settings.PublicKeyValidity.Value =
                (int)TimeSpan.FromDays((int)this.PublicKeyValidityInDays.Value).TotalSeconds;

            settings.PropagateLocale.Value = this.IsPropagateLocaleEnabled.Value;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableFunc<bool> IsPublicKeyValidityInDaysEditable { get; private set; }

        public bool IsPublicKeyTypeEditable { get; private set; }

        public ObservableProperty<bool> IsPropagateLocaleEnabled { get; private set; }

        public ObservableProperty<SshKeyType> PublicKeyType { get; private set; }

        public ObservableProperty<decimal> PublicKeyValidityInDays { get; private set; }

        public ObservableProperty<bool> UsePersistentKey { get; private set; }
        public ObservableFunc<bool> IsUsePersistentKeyEditable { get; private set; }
    }
}
