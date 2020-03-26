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

using Google.Solutions.Compute.Auth;
using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.SettingsEditor;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.RemoteDesktop;
using Google.Solutions.IapDesktop.Application.Windows.SerialLog;
using Google.Solutions.IapDesktop.Application.Windows.TunnelsViewer;
using Google.Solutions.IapDesktop.Windows;
using Microsoft.Win32;
using System;

namespace Google.Solutions.IapDesktop
{
    static class Program
    {
        private const string BaseRegistryKeyPath = @"Software\Google\IapDesktop\1.0";


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

            var serviceRegistry = new ServiceRegistry();

            serviceRegistry.AddSingleton(new WindowSettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Window")));
            serviceRegistry.AddSingleton(new AuthSettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Auth"),
                OAuthAuthorization.StoreUserId));
            serviceRegistry.AddSingleton(new InventorySettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Inventory")));


            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            var mainForm = new MainForm(serviceRegistry);
            serviceRegistry.AddSingleton<IMainForm>(mainForm);
            serviceRegistry.AddSingleton<IAuthorizationService>(mainForm);
            serviceRegistry.AddSingleton<IJobHost>(mainForm);
            serviceRegistry.AddSingleton<IJobService, JobService>();
            serviceRegistry.AddSingleton<IEventService>(new EventService(mainForm));

            serviceRegistry.AddTransient<ProjectInventoryService>();
            serviceRegistry.AddTransient<IResourceManagerAdapter, ResourceManagerAdapter>();
            serviceRegistry.AddTransient<IComputeEngineAdapter, ComputeEngineAdapter>();
            serviceRegistry.AddTransient<GithubAdapter>();
            serviceRegistry.AddTransient<CloudConsoleService>();
            serviceRegistry.AddTransient<ProjectPickerDialog>();
            serviceRegistry.AddTransient<IExceptionDialog, ExceptionDialog>();
            serviceRegistry.AddTransient<ITunnelService, TunnelService>();
            serviceRegistry.AddSingleton<TunnelBrokerService>();

            serviceRegistry.AddSingleton<RemoteDesktopService>();
            serviceRegistry.AddSingleton<SerialLogService>();
            serviceRegistry.AddSingleton<ISettingsEditor, SettingsEditorWindow>();
            serviceRegistry.AddSingleton<IProjectExplorer, ProjectExplorerWindow>();
            serviceRegistry.AddSingleton<ITunnelsViewer, TunnelsWindow>();

#if DEBUG
            serviceRegistry.AddSingleton<DebugWindow>();
#endif

            System.Windows.Forms.Application.Run(mainForm);
        }
    }
}
