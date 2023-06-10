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

using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Mvvm.Theme;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    /// <summary>
    /// Applies themes to controls and dialogs.
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Theme for dialogs and other secondary windows.
        /// </summary>
        IControlTheme DialogTheme { get; }

        /// <summary>
        /// Theme for tool windows, docked or undocked.
        /// </summary>
        IControlTheme ToolWindowTheme { get; }

        /// <summary>
        /// Theme for the main window.
        /// </summary>
        IControlTheme MainWindowTheme { get; }

        /// <summary>
        /// Theme for the docking suite.
        /// </summary>
        ThemeBase DockPanelTheme { get; }
    }

    public class ThemeService : IThemeService
    {
        public ThemeService(ThemeSettingsRepository themeSettingsRepository)
        {
            var settings = themeSettingsRepository.GetSettings();

            WindowsRuleSet windowsTheme;

            switch (settings.Theme.EnumValue)
            {
                case ThemeSettings.ApplicationTheme.System:
                    //
                    // Use same mode as Windows.
                    //
                    windowsTheme = new WindowsRuleSet(WindowsRuleSet.ShouldAppsUseDarkMode);
                    break;

                case ThemeSettings.ApplicationTheme.Dark:
                    //
                    // Use dark mode if possible.
                    //
                    windowsTheme = new WindowsRuleSet(WindowsRuleSet.IsDarkModeSupported);
                    break;

                default:
                    //
                    // Use safe defaults that also work on downlevel
                    // versions of Windows.
                    //
                    windowsTheme = new WindowsRuleSet(false);
                    break;
            }


            var vsTheme = windowsTheme.IsDarkModeEnabled
                ? VSTheme.GetDarkTheme()
                : VSTheme.GetLightTheme();

            var dialogTheme = new ControlTheme()
                .AddRuleSet(windowsTheme)
                .AddRuleSet(new CommonControlRuleSet())
                .AddRuleSet(new VSThemeDialogRuleSet(vsTheme));

            var dockWindowTheme = new ControlTheme()
                .AddRuleSet(windowsTheme)
                .AddRuleSet(new CommonControlRuleSet())
                .AddRuleSet(new VSThemeDockWindowRuleSet(vsTheme));

            //
            // Apply the resulting theme to the different kinds of windows we have.
            //
            this.DockPanelTheme = vsTheme;
            this.DialogTheme = dialogTheme;
            this.MainWindowTheme = dockWindowTheme;
            this.ToolWindowTheme = dockWindowTheme;
        }

        //---------------------------------------------------------------------
        // ITheme.
        //---------------------------------------------------------------------

        public ThemeBase DockPanelTheme { get; }
        public IControlTheme DialogTheme { get; }
        public IControlTheme ToolWindowTheme { get; }
        public IControlTheme MainWindowTheme { get; }

    }
}
