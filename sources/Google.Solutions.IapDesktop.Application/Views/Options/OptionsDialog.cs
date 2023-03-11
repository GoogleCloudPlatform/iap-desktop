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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views.Diagnostics;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.Mvvm.Binding;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    [SkipCodeCoverage("UI code")]
    public class OptionsDialog : PropertiesDialog
    {
        public OptionsDialog(IServiceCategoryProvider serviceProvider) //TODO: Remove class
            : base(serviceProvider)
        {
            this.Text = "Options";

            //var appSettingsRepository =
            //    serviceProvider.GetService<ApplicationSettingsRepository>();
            //var themeSettingsRepository =
            //    serviceProvider.GetService<ThemeSettingsRepository>();

            //AddSheet(new GeneralOptionsSheet(
            //    appSettingsRepository,
            //    serviceProvider.GetService<IAppProtocolRegistry>(),
            //    serviceProvider.GetService<HelpAdapter>()));
            //AddSheet(new AppearanceOptionsSheet(themeSettingsRepository));
            //AddSheet(new NetworkOptionsSheet(
            //    appSettingsRepository,
            //    serviceProvider.GetService<IHttpProxyAdapter>()));
            //AddSheet(new ScreenOptionsSheet(appSettingsRepository));

            //// Load all services implementing IOptionsDialogPane and
            //// add them automatically. This gives extensions a chance
            //// to plug in their own panes.
            //foreach (var sheet in serviceProvider
            //    .GetServicesByCategory<IPropertiesSheet>()
            //    .OrderBy(p => p.ViewModel.Title))
            //{
            //    AddSheet((UserControl)sheet, sheet);
            //}
        }

        public static void Show(
            IWin32Window parent,
            IServiceCategoryProvider serviceProvider) // TODO: Move elsewhere
        {
            using (var dialog = serviceProvider.GetDialog<PropertiesView, PropertiesViewModel>())
            {
                dialog.Theme = serviceProvider.GetService<IThemeService>().DialogTheme;

                var appSettingsRepository = serviceProvider.GetService<ApplicationSettingsRepository>();

                dialog.ViewModel.AddSheet(
                    new GeneralOptionsSheet(),
                    new GeneralOptionsViewModel(
                        appSettingsRepository,
                        serviceProvider.GetService<IAppProtocolRegistry>(),
                        serviceProvider.GetService<HelpAdapter>()));
                dialog.ViewModel.AddSheet(
                    new AppearanceOptionsSheet(),
                    new AppearanceOptionsViewModel(serviceProvider.GetService<ThemeSettingsRepository>()));
                dialog.ViewModel.AddSheet(
                    new ScreenOptionsSheet(),
                    new ScreenOptionsViewModel(appSettingsRepository));

#if DEBUG
                dialog.ViewModel.AddSheet(new DebugOptionsSheet(), new DebugOptionsSheetViewModel());
#endif

                dialog.ShowDialog(parent);
            }
        }
    }

}
