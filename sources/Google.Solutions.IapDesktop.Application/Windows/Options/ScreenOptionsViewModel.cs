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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Settings.Collection;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

#nullable disable

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    internal class ScreenOptionsViewModel : OptionsViewModelBase<IApplicationSettings>
    {
        public ScreenOptionsViewModel(IRepository<IApplicationSettings> settingsRepository)
            : base("Display", settingsRepository)
        {
            base.OnInitializationCompleted();
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Load(IApplicationSettings settings)
        {
            var fullScreenDevices =
                (settings.FullScreenDevices.Value ?? string.Empty)
                    .Split(ApplicationSettingsRepository.FullScreenDevicesSeparator)
                    .ToHashSet();

            this.Devices = new ObservableCollection<ScreenDevice>(
                Screen.AllScreens.Select(s => new ScreenDevice(this, s)
                {
                    IsSelected = fullScreenDevices.Contains(s.DeviceName)
                }));
        }

        protected override void Save(IApplicationSettings settings)
        {
            var selectedDevices = this.Devices
                .Where(d => d.IsSelected)
                .Select(d => d.DeviceName);

            settings.FullScreenDevices.Value = selectedDevices.Any()
                ? string.Join(
                    ApplicationSettingsRepository.FullScreenDevicesSeparator.ToString(),
                    selectedDevices)
                : null;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableCollection<ScreenDevice> Devices { get; private set; }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public class ScreenDevice : IScreenPickerModelItem
        {
            private readonly ScreenOptionsViewModel model;
            private readonly Screen screen;

            private bool isSelected;

            public string DeviceName => this.screen.DeviceName;

            public Rectangle ScreenBounds => this.screen.Bounds;

            public bool IsSelected
            {
                get => this.isSelected;
                set
                {
                    this.isSelected = value;
                    this.model.IsDirty.Value = true;
                }
            }

            public ScreenDevice(ScreenOptionsViewModel model, Screen screen)
            {
                this.model = model;
                this.screen = screen;
            }
        }
    }
}
