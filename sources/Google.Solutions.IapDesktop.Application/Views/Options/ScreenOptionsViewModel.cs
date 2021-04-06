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
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    public class ScreenOptionsViewModel : ViewModelBase, IOptionsDialogPane
    {
        private readonly ApplicationSettingsRepository settingsRepository;

        private bool isDirty;

        public ScreenOptionsViewModel(
            ApplicationSettingsRepository settingsRepository)
        {
            this.settingsRepository = settingsRepository;

            //
            // Read current settings.
            //
            // NB. Do not hold on to the settings object because other tabs
            // might apply changes to other application settings.
            //

            var fullScreenDevices = (this.settingsRepository.GetSettings()
                .FullScreenDevices
                .StringValue ?? string.Empty)
                    .Split(ApplicationSettings.FullScreenDevicesSeparator)
                    .ToHashSet();

            this.Devices = new ObservableCollection<ScreenDevice>(
                Screen.AllScreens.Select(s => new ScreenDevice(this, s)
                {
                    IsSelected = fullScreenDevices.Contains(s.DeviceName)
                }));

            this.isDirty = false;
        }


        public ScreenOptionsViewModel(IServiceProvider serviceProvider)
            : this(
                  serviceProvider.GetService<ApplicationSettingsRepository>())
        {
        }

        //---------------------------------------------------------------------
        // IOptionsDialogPane.
        //---------------------------------------------------------------------

        public string Title => "Display";

        public UserControl CreateControl() => new ScreenOptionsControl(this);

        public bool IsDirty
        {
            get => this.isDirty;
            set
            {
                this.isDirty = value;
                RaisePropertyChange();
            }
        }

        public void ApplyChanges()
        {
            Debug.Assert(this.IsDirty);

            //
            // Validate ans save settings.
            //
            var selectedDevices = this.Devices
                .Where(d => d.IsSelected)
                .Select(d => d.DeviceName);

            var settings = this.settingsRepository.GetSettings();

            settings.FullScreenDevices.StringValue = selectedDevices.Any()
                ? string.Join(
                    ApplicationSettings.FullScreenDevicesSeparator.ToString(),
                    selectedDevices)
                : null;

            this.settingsRepository.SetSettings(settings);

            this.IsDirty = false;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableCollection<ScreenDevice> Devices;

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
                    this.model.IsDirty = true;
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
