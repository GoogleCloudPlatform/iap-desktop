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
using Google.Solutions.Mvvm.Theme;
using Google.Solutions.Settings.Collection;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    public class Themes
    {
        public ISystemDialogTheme SystemDialogTheme { get; }
        public IDialogTheme DialogTheme { get; }
        public IToolWindowTheme ToolWindowTheme { get; }
        public IMainWindowTheme MainWindowTheme { get; }

        private Themes(
            ISystemDialogTheme systemDialogTheme,
            IDialogTheme dialogTheme,
            IToolWindowTheme toolWindowTheme,
            IMainWindowTheme mainWindowTheme)
        {
            this.SystemDialogTheme = systemDialogTheme;
            this.DialogTheme = dialogTheme;
            this.ToolWindowTheme = toolWindowTheme;
            this.MainWindowTheme = mainWindowTheme;
        }

        /// <summary>
        /// Load themes from the respository.
        /// </summary>
        public static Themes Load(IRepository<IThemeSettings> themeSettingsRepository) // TODO: make async
        {
            var settings = themeSettingsRepository.GetSettings();
            var windowsTheme = settings.Theme.Value switch
            {
                //
                // Use same mode as Windows.
                //
                ApplicationTheme.System => new WindowsRuleSet(SystemTheme.ShouldAppsUseDarkMode),

                //
                // Use dark mode if possible.
                //
                ApplicationTheme.Dark => new WindowsRuleSet(SystemTheme.IsDarkModeSupported),

                //
                // Use safe defaults that also work on downlevel
                // versions of Windows.
                //
                _ => new WindowsRuleSet(false),
            };

            var vsTheme = windowsTheme.IsDarkModeEnabled
                ? VSTheme.GetDarkTheme()
                : VSTheme.GetLightTheme();

            var systemDialogTheme = new ControlTheme()
                .AddRuleSet(windowsTheme)
                .AddRuleSet(new WindowsSystemDialogRuleset())
                .AddRuleSet(new VSThemeDialogRuleSet(vsTheme))
                .AddRuleSet(new GdiScalingRuleset());

            var dialogTheme = new ControlTheme()
                .AddRuleSet(windowsTheme)
                .AddRuleSet(new CommonControlRuleSet())
                .AddRuleSet(new VSThemeDialogRuleSet(vsTheme))
                .AddRuleSet(new GdiScalingRuleset());

            var dockWindowTheme = new ControlTheme()
                .AddRuleSet(windowsTheme)
                .AddRuleSet(new CommonControlRuleSet())
                .AddRuleSet(new VSThemeDockWindowRuleSet(vsTheme))
                .AddRuleSet(new GdiScalingRuleset());


            if (vsTheme.Extender.FloatWindowFactory is VSThemeExtensions.FloatWindowFactory factory)
            {
                factory.Theme = dialogTheme;
            }

            return new Themes(
                new SystemDialogWindowTheme(systemDialogTheme),
                new DialogWindowTheme(dialogTheme),
                new ToolWindowWindowTheme(dockWindowTheme),
                new MainWindowWindowTheme(dockWindowTheme, vsTheme));
        }

        //---------------------------------------------------------------------
        // Typed theme wrappers.
        //---------------------------------------------------------------------

        private abstract class WindowThemeBase : IControlTheme // TODO: rename wrapper classes
        {
            private readonly IControlTheme theme;

            protected WindowThemeBase(IControlTheme theme)
            {
                this.theme = theme.ExpectNotNull(nameof(theme));
            }

            public void ApplyTo(Control control)
            {
                this.theme.ApplyTo(control);
            }
        }

        private class SystemDialogWindowTheme : WindowThemeBase, ISystemDialogTheme
        {
            internal SystemDialogWindowTheme(IControlTheme theme) : base(theme)
            { }
        }

        private class DialogWindowTheme : WindowThemeBase, IDialogTheme
        {
            internal DialogWindowTheme(IControlTheme theme) : base(theme)
            { }
        }

        private class MainWindowWindowTheme : WindowThemeBase, IMainWindowTheme
        {
            internal MainWindowWindowTheme(
                IControlTheme theme,
                ThemeBase dockPanelTheme) : base(theme)
            {
                this.DockPanelTheme = dockPanelTheme;
            }

            public ThemeBase DockPanelTheme { get;}
        }

        private class ToolWindowWindowTheme : WindowThemeBase, IToolWindowTheme
        {
            internal ToolWindowWindowTheme(IControlTheme theme) : base(theme)
            { }
        }
    }
}
