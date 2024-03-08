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

using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Platform.Net;
using Google.Solutions.Settings;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Options
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

                var appSettingsRepository = serviceProvider.GetService<IRepository<IApplicationSettings>>();

                dialog.ViewModel.AddSheet(
                    new GeneralOptionsSheet(),
                    new GeneralOptionsViewModel(
                        appSettingsRepository,
                        serviceProvider.GetService<IBrowserProtocolRegistry>(),
                        serviceProvider.GetService<ITelemetryCollector>(),
                        serviceProvider.GetService<HelpClient>()));
                dialog.ViewModel.AddSheet(
                    new NetworkOptionsSheet(),
                    new NetworkOptionsViewModel(
                        appSettingsRepository,
                        serviceProvider.GetService<IHttpProxyAdapter>()));
                dialog.ViewModel.AddSheet(
                    new AccessOptionsSheet(),
                    new AccessOptionsViewModel(
                        serviceProvider.GetService<IRepository<IAccessSettings>>(),
                        serviceProvider.GetService<HelpClient>()));
                dialog.ViewModel.AddSheet(
                    new AppearanceOptionsSheet(),
                    new AppearanceOptionsViewModel(serviceProvider.GetService<IRepository<IThemeSettings>>()));
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

                dialog.ViewModel.WindowTitle.Value = "Options";
                return dialog.ShowDialog(parent);
            }
        }
    }

}
