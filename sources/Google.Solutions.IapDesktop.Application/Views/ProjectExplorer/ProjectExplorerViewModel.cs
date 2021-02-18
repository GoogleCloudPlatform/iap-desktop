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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using System;

namespace Google.Solutions.IapDesktop.Application.Views.ProjectExplorer
{
    internal class ProjectExplorerViewModel : ViewModelBase, IDisposable
    {
        private readonly ApplicationSettingsRepository settingsRepository;
        private OperatingSystems includedOperatingSystems;

        public ProjectExplorerViewModel(
            ApplicationSettingsRepository settingsRepository)
        {
            this.settingsRepository = settingsRepository;

            //
            // Read current settings.
            //
            // NB. Do not hold on to the settings object because it might change.
            //

            this.includedOperatingSystems = settingsRepository
                .GetSettings()
                .IncludeOperatingSystems
                .EnumValue;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsLinuxIncluded
        {
            get => this.includedOperatingSystems.HasFlag(OperatingSystems.Linux);
            set
            {
                if (value)
                {
                    this.includedOperatingSystems |= OperatingSystems.Linux;
                }
                else
                {
                    this.includedOperatingSystems &= ~OperatingSystems.Linux;
                }

                RaisePropertyChange();
                SaveSettings();
            }
        }
        public bool IsWindowsIncluded
        {
            get => this.includedOperatingSystems.HasFlag(OperatingSystems.Windows);
            set
            {
                if (value)
                {
                    this.includedOperatingSystems |= OperatingSystems.Windows;
                }
                else
                {
                    this.includedOperatingSystems &= ~OperatingSystems.Windows;
                }

                RaisePropertyChange();
                SaveSettings();
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void SaveSettings()
        {
            var settings = this.settingsRepository.GetSettings();
            settings.IncludeOperatingSystems.EnumValue = this.includedOperatingSystems;
            this.settingsRepository.SetSettings(settings);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.settingsRepository.Dispose();
        }
    }
}
