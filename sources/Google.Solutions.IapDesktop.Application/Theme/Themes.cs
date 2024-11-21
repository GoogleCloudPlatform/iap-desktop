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
    /// <summary>
    /// Theme for system dialogs and other secondary windows.
    /// </summary>
    public interface ISystemDialogTheme : IControlTheme { }

    /// <summary>
    /// Theme for dialogs and other secondary windows.
    /// </summary>
    public interface IDialogTheme : IControlTheme { }

    /// <summary>
    /// Theme for tool windows, docked or undocked.
    /// </summary>
    public interface IToolWindowTheme : IControlTheme { }

    /// <summary>
    /// Theme for the main window.
    /// </summary>
    public interface IMainWindowTheme : IControlTheme
    {
        /// <summary>
        /// Theme for the docking suite.
        /// </summary>
        ThemeBase DockPanelTheme { get; }
    }

    public class Themes
    {
        /// <summary>
        /// Theme for system dialogs.
        /// </summary>
        public ISystemDialogTheme SystemDialog { get; }

        /// <summary>
        /// Theme for dialogs.
        /// </summary>
        public IDialogTheme Dialog { get; }

        /// <summary>
        /// Theme for tool windows.
        /// </summary>
        public IToolWindowTheme ToolWindow { get; }

        /// <summary>
        /// Theme for the main window.
        /// </summary>
        public IMainWindowTheme MainWindow { get; }

        private Themes(
            ISystemDialogTheme systemDialogTheme,
            IDialogTheme dialogTheme,
            IToolWindowTheme toolWindowTheme,
            IMainWindowTheme mainWindowTheme)
        {
            this.SystemDialog = systemDialogTheme;
            this.Dialog = dialogTheme;
            this.ToolWindow = toolWindowTheme;
            this.MainWindow = mainWindowTheme;
        }

        /// <summary>
        /// Load themes from the respository.
        /// </summary>
        public static Themes Load(IRepository<IThemeSettings> themeSettingsRepository)
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
                .AddRuleSet(new DpiAwarenessRuleset());

            var dialogTheme = new ControlTheme()
                .AddRuleSet(windowsTheme)
                .AddRuleSet(new CommonControlRuleSet())
                .AddRuleSet(new VSThemeDialogRuleSet(vsTheme))
                .AddRuleSet(new DpiAwarenessRuleset());

            var dockWindowTheme = new ControlTheme()
                .AddRuleSet(windowsTheme)
                .AddRuleSet(new CommonControlRuleSet())
                .AddRuleSet(new VSThemeDockWindowRuleSet(vsTheme))
                .AddRuleSet(new DpiAwarenessRuleset());


            if (vsTheme.Extender.FloatWindowFactory is VSThemeExtensions.FloatWindowFactory factory)
            {
                factory.Theme = dialogTheme;
            }

            return new Themes(
                new SystemDialogTheme(systemDialogTheme),
                new DialogWindowTheme(dialogTheme),
                new ToolWindowTheme(dockWindowTheme),
                new MainWindowTheme(dockWindowTheme, vsTheme));
        }

        //---------------------------------------------------------------------
        // Typed theme wrappers.
        //---------------------------------------------------------------------

        private abstract class WindowThemeBase : IControlTheme
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

        private class SystemDialogTheme : WindowThemeBase, ISystemDialogTheme
        {
            internal SystemDialogTheme(IControlTheme theme) : base(theme)
            { }
        }

        private class DialogWindowTheme : WindowThemeBase, IDialogTheme
        {
            internal DialogWindowTheme(IControlTheme theme) : base(theme)
            { }
        }

        private class MainWindowTheme : WindowThemeBase, IMainWindowTheme
        {
            internal MainWindowTheme(
                IControlTheme theme,
                ThemeBase dockPanelTheme) : base(theme)
            {
                this.DockPanelTheme = dockPanelTheme;
            }

            public ThemeBase DockPanelTheme { get; }
        }

        private class ToolWindowTheme : WindowThemeBase, IToolWindowTheme
        {
            internal ToolWindowTheme(IControlTheme theme) : base(theme)
            { }
        }
    }
}
