﻿//
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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.RemoteDesktop;
using Google.Solutions.IapDesktop.Application.Services.Windows.SerialLog;
using Google.Solutions.IapDesktop.Application.Services.Windows.SettingsEditor;
using Google.Solutions.IapDesktop.Application.Services.Windows.TunnelsViewer;
using Google.Solutions.IapDesktop.Application.Services.Workflows;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Windows;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop
{
    static class Program
    {
        private const string BaseRegistryKeyPath = @"Software\Google\IapDesktop\1.0";

        private static bool tracingEnabled = false;

        private static readonly TraceSource[] Traces = new[]
        {
            Google.Solutions.Compute.TraceSources.Compute,
            Google.Solutions.IapDesktop.Application.TraceSources.IapDesktop
        };

        public static string LogFile =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Google",
                "IAP Desktop",
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

        private static IapRdpUrl ParseCommandLine(string[] args)
        {
            if (args.Length == 0)
            {
                // No arguments passed.
                return null;
            }
            else if (args.Length > 1 && args[0] == "/url")
            {
                // Certain legacy browsers do not properly quote URLs when passing them
                // as command line arguments. If the URL contains a space, it might be
                // delivered as two separate arguments.

                var url = string.Join(" ", args.Skip(1)).Trim();

                try
                {
                    return IapRdpUrl.FromString(url);
                }
                catch (UriFormatException e)
                {
                    MessageBox.Show(
                        "Invalid command line options.\n\n" + e.Message,
                        "IAP Desktop",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else 
            {
                MessageBox.Show(
                    "Invalid command line options.", 
                    "IAP Desktop", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }

            Environment.Exit(1);
            return null;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var startupUrl = ParseCommandLine(args);

            IsLoggingEnabled = false;

            // Use TLS 1.2 if possible.
            System.Net.ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 |
                SecurityProtocolType.Tls11;

            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            //
            // Set up layers. Services in a layer can lookup services in a lower layer,
            // but not in a higher layer.
            //
            var persistenceLayer = new ServiceRegistry();
            var adapterLayer = new ServiceRegistry(persistenceLayer);
            var integrationLayer = new ServiceRegistry(adapterLayer);
            var windowAndWorkflowLayer = new ServiceRegistry(integrationLayer);

            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

            // 
            // Persistence layer.
            //
            persistenceLayer.AddTransient<AppProtocolRegistry>();
            persistenceLayer.AddSingleton(new ApplicationSettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Application")));
            persistenceLayer.AddSingleton(new AuthSettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Auth"),
                GoogleAuthAdapter.StoreUserId));
            persistenceLayer.AddSingleton(new ConnectionSettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Inventory")));

            var mainForm = new MainForm(persistenceLayer, windowAndWorkflowLayer);
            mainForm.StartupUrl = startupUrl;

            //
            // Adapter layer.
            //
            adapterLayer.AddSingleton<IAuthorizationAdapter>(mainForm);
            adapterLayer.AddSingleton<IJobHost>(mainForm);
            adapterLayer.AddTransient<IResourceManagerAdapter, ResourceManagerAdapter>();
            adapterLayer.AddTransient<IComputeEngineAdapter, ComputeEngineAdapter>();
            adapterLayer.AddTransient<GithubAdapter>();

            //
            // Integration layer.
            //
            integrationLayer.AddSingleton<IJobService, JobService>();
            integrationLayer.AddSingleton<IEventService>(new EventService(mainForm));
            integrationLayer.AddTransient<ProjectInventoryService>();
            integrationLayer.AddTransient<ITunnelService, TunnelService>();
            integrationLayer.AddSingleton<ITunnelBrokerService, TunnelBrokerService>();


            //
            // Window & workflow layer.
            //
            windowAndWorkflowLayer.AddSingleton<IMainForm>(mainForm);
            windowAndWorkflowLayer.AddTransient<CloudConsoleService>();
            windowAndWorkflowLayer.AddTransient<IProjectPickerDialog, ProjectPickerDialog>();
            windowAndWorkflowLayer.AddTransient<AboutWindow>();
            windowAndWorkflowLayer.AddTransient<IExceptionDialog, ExceptionDialog>();
            windowAndWorkflowLayer.AddTransient<ITaskDialog, TaskDialog>();
            windowAndWorkflowLayer.AddTransient<IUpdateService, UpdateService>();
            windowAndWorkflowLayer.AddSingleton<IRemoteDesktopService, RemoteDesktopService>();
            windowAndWorkflowLayer.AddSingleton<SerialLogService>();
            windowAndWorkflowLayer.AddSingleton<ISettingsEditor, SettingsEditorWindow>();
            windowAndWorkflowLayer.AddSingleton<IProjectExplorer, ProjectExplorerWindow>();
            windowAndWorkflowLayer.AddSingleton<ITunnelsViewer, TunnelsWindow>();
            windowAndWorkflowLayer.AddTransient<ICredentialsService, CredentialsService>();
            windowAndWorkflowLayer.AddTransient<RemoteDesktopConnectionService>();

#if DEBUG
            windowAndWorkflowLayer.AddSingleton<DebugWindow>();
#endif

            // Run app.
            System.Windows.Forms.Application.Run(mainForm);

            // Ensure logs are flushed.
            IsLoggingEnabled = false;
        }
    }
}
