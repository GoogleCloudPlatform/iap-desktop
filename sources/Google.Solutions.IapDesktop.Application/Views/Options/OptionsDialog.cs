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
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views.Diagnostics;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Platform.Net;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Options
{
    public static class OptionsDialog
    {
        public static DialogResult Show(
            IWin32Window parent,
            IServiceCategoryProvider serviceProvider)
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
                    new NetworkOptionsSheet(),
                    new NetworkOptionsViewModel(
                        appSettingsRepository,
                        serviceProvider.GetService<IHttpProxyAdapter>()));
                dialog.ViewModel.AddSheet(
                    new ScreenOptionsSheet(),
                    new ScreenOptionsViewModel(appSettingsRepository));

                //
                // Load all services implementing IPropertiesSheet and
                // add them automatically. This gives extensions a chance
                // to plug in their own sheets.
                //
                foreach (var sheet in serviceProvider
                    .GetServicesByCategory<IPropertiesSheetView>()
                    .Select(sheet => new {
                        View = sheet,
                        ViewModel = (PropertiesSheetViewModelBase)serviceProvider.GetService(sheet.ViewModel)
                    })
                    .OrderBy(p => p.ViewModel.Title))
                {
                    dialog.ViewModel.AddSheet(sheet.View, sheet.ViewModel);
                }

#if DEBUG
                dialog.ViewModel.AddSheet(new DebugOptionsSheet(), new DebugOptionsSheetViewModel());
#endif

                dialog.ViewModel.WindowTitle.Value = "Options";
                return dialog.ShowDialog(parent);
            }
        }
    }

}
