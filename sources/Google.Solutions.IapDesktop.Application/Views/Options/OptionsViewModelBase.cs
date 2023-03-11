//
// Copyright 2023 Google LLC
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
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.Mvvm.Binding;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    public abstract class OptionsViewModelBase<TSettings>
        : PropertiesSheetViewModelBase
        where TSettings : IRegistrySettingsCollection
    {
        private readonly SettingsRepositoryBase<TSettings> settingsRepository;

        public OptionsViewModelBase(
            string title,
            SettingsRepositoryBase<TSettings> settingsRepository)
            : base(title)
        {
            this.settingsRepository = settingsRepository.ThrowIfNull(nameof(settingsRepository));

            this.IsDirty = ObservableProperty.Build(false);
        }

        /// <summary>
        /// Deriving class must call this method at the end of their constructor.
        /// </summary>
        protected void OnInitializationCompleted()
        {
            Load(this.settingsRepository.GetSettings());

            this.IsDirty.Value = false;
        }

        /// <summary>
        /// Mark view model as dirty if the property changes.
        protected void MarkDirtyWhenPropertyChanges<T>(ObservableProperty<T> property)
        {
            //
            // Mark view model as dirty until changes are applied.
            //
            property.PropertyChanged += (_, __) => this.IsDirty.Value = true;
        }

        //---------------------------------------------------------------------
        // PropertiesSheetViewModelBase.
        //---------------------------------------------------------------------

        public override ObservableProperty<bool> IsDirty { get; }

        protected override void ApplyChanges()
        {
            Debug.Assert(this.IsDirty.Value);

            //
            // Save settings.
            //
            var settings = this.settingsRepository.GetSettings();
            Save(settings);
            this.settingsRepository.SetSettings(settings);

            this.IsDirty.Value = false;

            Debug.Assert(!this.IsDirty.Value);
        }

        //---------------------------------------------------------------------
        // Abstracts.
        //---------------------------------------------------------------------

        protected abstract void Load(TSettings settings);

        protected abstract void Save(TSettings settings);
    }
}
