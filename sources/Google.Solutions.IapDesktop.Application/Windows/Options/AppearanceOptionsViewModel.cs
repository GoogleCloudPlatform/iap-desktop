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

using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Theme;
using Google.Solutions.Settings.Collection;

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    internal class AppearanceOptionsViewModel : OptionsViewModelBase<IThemeSettings>
    {
        public AppearanceOptionsViewModel(IRepository<IThemeSettings> settingsRepository)
            : base("Appearance", settingsRepository)
        {
            var settings = settingsRepository.GetSettings();

            //
            // If Windows doesn't support dark mode, none other than
            // the default scheme are guaranteed to work.
            //
            this.IsThemeEditable = SystemTheme.IsDarkModeSupported;
            this.ThemeInfoText = this.IsThemeEditable
                ? "Changes take effect after relaunch"
                : "Themes are not supported on this version\nof Windows";
            this.SelectedTheme = ObservableProperty.Build(settings.Theme.Value);

            this.ScalingMode = ObservableProperty.Build(
                settings.ScalingMode.Value);

            MarkDirtyWhenPropertyChanges(this.SelectedTheme);
            MarkDirtyWhenPropertyChanges(this.ScalingMode);

            base.OnInitializationCompleted();
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Save(IThemeSettings settings)
        {
            settings.Theme.Value = this.SelectedTheme.Value;
            settings.ScalingMode.Value = this.ScalingMode.Value;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsThemeEditable { get; private set; }

        public string ThemeInfoText { get; private set; }

        public ObservableProperty<ApplicationTheme> SelectedTheme { get; private set; }

        public ObservableProperty<ScalingMode> ScalingMode { get; private set; }
    }
}
