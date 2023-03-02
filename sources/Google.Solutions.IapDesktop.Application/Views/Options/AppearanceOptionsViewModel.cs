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

using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Theme;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    internal class AppearanceOptionsViewModel : OptionsViewModelBase<ThemeSettings>
    {
        public AppearanceOptionsViewModel(ThemeSettingsRepository settingsRepository)
            : base("Appearance", settingsRepository)
        {
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Load(ThemeSettings settings)
        {
            //
            // If Windows doesn't support dark mode, none other than
            // the default scheme are guaranteed to work.
            //
            this.IsThemeEditable = WindowsTheme.IsDarkModeSupported;
            this.ThemeInfoText = this.IsThemeEditable
                ? "Changes take effect after relaunch"
                : "Themes are not supported on this version\nof Windows";
            this.SelectedTheme = ObservableProperty.Build(settings.Theme.EnumValue);

            MarkDirtyWhenPropertyChanges(this.SelectedTheme);
        }

        protected override void Save(ThemeSettings settings)
        {
            settings.Theme.EnumValue = this.SelectedTheme.Value;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsThemeEditable { get; private set; }

        public string ThemeInfoText { get; private set; }

        public ObservableProperty<ThemeSettings.ApplicationTheme> SelectedTheme { get; private set; }
    }
}
