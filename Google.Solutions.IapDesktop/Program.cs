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
using Google.Solutions.IapDesktop.Application;
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
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Google.Solutions.IapDesktop
{
    static class Program
    {
        private const string BaseRegistryKeyPath = @"Software\Google\IapDesktop\1.0";

        private static bool tracingEnabled = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            IsLoggingEnabled = false;
            
            var serviceRegistry = new ServiceRegistry();

            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
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

            // Ensure logs are flushed.
            IsLoggingEnabled = false;
        }

        private static TraceSource[] Traces = new[]
        {
            Google.Solutions.Compute.TraceSources.Compute,
            Google.Solutions.IapDesktop.Application.TraceSources.IapDesktop
        };

        public static string LogFile =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Google",
                "Cloud IAP Desktop",
                "Logs",
                $"{DateTime.Now:yyyy-MM-dd_HHmm}.log");

        public static bool IsLoggingEnabled
        {
            get
            {
                return tracingEnabled;
            }
            set
            {
                tracingEnabled = value;


                if (tracingEnabled)
                {
                    var logFilePath = LogFile;
                    Directory.CreateDirectory(new FileInfo(logFilePath).DirectoryName);
                    var logListener = new TextWriterTraceListener(logFilePath);

                    foreach (var trace in Traces)
                    {
                        trace.Listeners.Add(new DefaultTraceListener());
                        trace.Listeners.Add(logListener);
                        trace.Switch.Level = System.Diagnostics.SourceLevels.Verbose;
                    }
                }
                else
                {
                    foreach (var trace in Traces)
                    {
                        foreach (var listener in trace.Listeners.Cast<TraceListener>())
                        {
                            listener.Flush();
                        }

                        trace.Switch.Level = System.Diagnostics.SourceLevels.Off;
                    }
                }
            }
        }
    }
}
