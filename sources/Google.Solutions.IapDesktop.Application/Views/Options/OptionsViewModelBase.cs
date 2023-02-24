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
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.Mvvm.Binding;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    internal abstract class OptionsViewModelBase<TSettings> // TODO: Add tests 
        : ViewModelBase, IPropertiesSheetViewModel
        where TSettings : IRegistrySettingsCollection
    {
        private bool isDirty;
        private readonly SettingsRepositoryBase<TSettings> settingsRepository;

        public OptionsViewModelBase(
            string title,
            SettingsRepositoryBase<TSettings> settingsRepository
            )
        {
            this.Title = title.ThrowIfNullOrEmpty(nameof(title));
            this.settingsRepository = settingsRepository.ThrowIfNull(nameof(settingsRepository));

            this.settingsRepository = settingsRepository;

            Load(settingsRepository.GetSettings());
        }

        //---------------------------------------------------------------------
        // IPropertiesSheetViewModel.
        //---------------------------------------------------------------------

        public string Title { get; }

        public bool IsDirty
        {
            get => this.isDirty;
            set
            {
                this.isDirty = value;
                RaisePropertyChange();
            }
        }

        public DialogResult ApplyChanges()
        {
            Debug.Assert(this.IsDirty);

            //
            // Save settings.
            //
            var settings = this.settingsRepository.GetSettings();
            Save(settings);
            this.settingsRepository.SetSettings(settings);

            this.IsDirty = false;

            Debug.Assert(!this.IsDirty);

            return DialogResult.OK;
        }

        //---------------------------------------------------------------------
        // Abstracts.
        //---------------------------------------------------------------------

        protected abstract void Load(TSettings settings);

        protected abstract void Save(TSettings settings);
    }
}
