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

using Google.Solutions.Common;
using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.About;
using Google.Solutions.IapDesktop.Application.Views.Diagnostics;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Views.ProjectPicker;
using Google.Solutions.IapDesktop.Windows;
using Google.Solutions.IapTunneling;
using Google.Solutions.IapTunneling.Net;
using Google.Solutions.Ssh;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.IapDesktop
{
    class Program : SingletonApplicationBase
    {
        private static readonly Version Windows11 = new Version(10, 0, 22000, 0);
        private static readonly Version WindowsServer2022 = new Version(10, 0, 20348, 0);

        private static bool tracingEnabled = false;

        private static readonly TraceSource[] Traces = new[]
        {
            CommonTraceSources.Google,
            CommonTraceSources.Default,
            IapTraceSources.Default,
            SshTraceSources.Default,
            ApplicationTraceSources.Default
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

        private IEnumerable<Assembly> LoadExtensionAssemblies()
        {
            return Directory.GetFiles(
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location),
                    "*.Extensions.*.dll")
                .Where(name => !name.EndsWith( // Ignore leftover, outdated extension.
                    "google.solutions.iapdesktop.extensions.rdp.dll",
                    StringComparison.OrdinalIgnoreCase))
                .Select(dllPath => Assembly.LoadFrom(dllPath));
        }

        //---------------------------------------------------------------------
        // SingletonApplicationBase overrides.
        //---------------------------------------------------------------------

        private MainForm initializedMainForm = null;
        private readonly ManualResetEvent mainFormInitialized = new ManualResetEvent(false);

        internal Program() : base("IapDesktop")
        {
        }

        protected override int HandleFirstInvocation(string[] args)
        {
            var options = CommandLineOptions.ParseOrExit(args);

            IsLoggingEnabled = options.IsLoggingEnabled;

            //
            // Set up process mitigations. This must be done early, otherwise it's
            // ineffective.
            //
            try
            {
                ProcessMitigations.Apply();
            }
            catch (Exception e)
            {
                ApplicationTraceSources.Default.TraceError(e);
            }

#if DEBUG
            ApplicationTraceSources.Default.Switch.Level = SourceLevels.Verbose;
            //SshTraceSources.Default.Switch.Level = SourceLevels.Verbose;
#endif

            if (Environment.OSVersion.Version >= Windows11 ||
                Environment.OSVersion.Version >= WindowsServer2022)
            {
                //
                // Windows 2022 and Windows 11 fully support TLS 1.3:
                // https://docs.microsoft.com/en-us/windows/win32/secauthn/protocols-in-tls-ssl--schannel-ssp-
                //
                System.Net.ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 |
                    SecurityProtocolType.Tls11 |
                    (SecurityProtocolType)0x3000; // TLS 1.3
            }
            else
            {
                //
                // Windows 10 and below don't properly support TLS 1.3 yet.
                //
                System.Net.ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 |
                    SecurityProtocolType.Tls11;
            }

            //
            // Lift limit on concurrent HTTP connections to same endpoint,
            // relevant for GCS downloads.
            //
            ServicePointManager.DefaultConnectionLimit = 16;

            // Allow custom User-Agent headers.
            try
            {
                RestrictedHeaderConfigPatch.SetHeaderRestriction("User-Agent", false);
            }
            catch (InvalidOperationException)
            {
                ApplicationTraceSources.Default.TraceWarning(
                    "Failed to un-restrict User-Agent headers");
            }

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
            var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);

            // 
            // Persistence layer.
            //
            var appSettingsRepository = new ApplicationSettingsRepository(
                hkcu.CreateSubKey($@"{Globals.SettingsKeyPath}\Application"),
                hklm.OpenSubKey($@"{Globals.PoliciesKeyPath}\Application"),
                hkcu.OpenSubKey($@"{Globals.PoliciesKeyPath}\Application"));
            if (appSettingsRepository.IsPolicyPresent)
            {
                //
                // If there are policies in place, mark the UA as
                // Enterprise-managed.
                //
                Globals.UserAgent.Extensions = "Enterprise";
            }

            persistenceLayer.AddTransient<IAppProtocolRegistry, AppProtocolRegistry>();
            persistenceLayer.AddSingleton(appSettingsRepository);
            persistenceLayer.AddSingleton(new ToolWindowStateRepository(
                hkcu.CreateSubKey($@"{Globals.SettingsKeyPath}\ToolWindows")));
            persistenceLayer.AddSingleton(new AuthSettingsRepository(
                hkcu.CreateSubKey($@"{Globals.SettingsKeyPath}\Auth"),
                GoogleAuthAdapter.StoreUserId));

            var mainForm = new MainForm(persistenceLayer, windowAndWorkflowLayer)
            {
                StartupUrl = options.StartupUrl
            };

            //
            // Adapter layer.
            //
            adapterLayer.AddSingleton<IAuthorizationAdapter>(mainForm);
            adapterLayer.AddSingleton<IJobHost>(mainForm);
            adapterLayer.AddTransient<IResourceManagerAdapter, ResourceManagerAdapter>();
            adapterLayer.AddTransient<IComputeEngineAdapter, ComputeEngineAdapter>();
            adapterLayer.AddTransient<IWindowsCredentialAdapter, WindowsCredentialAdapter>();
            adapterLayer.AddTransient<GithubAdapter>();
            adapterLayer.AddTransient<BuganizerAdapter>();
            adapterLayer.AddTransient<IHttpProxyAdapter, HttpProxyAdapter>();

            try
            {
                // Activate proxy settings based on app settings.
                adapterLayer.GetService<IHttpProxyAdapter>().ActivateSettings(
                    adapterLayer.GetService<ApplicationSettingsRepository>().GetSettings());
            }
            catch (Exception)
            {
                // Settings invalid -> ignore.
            }

            //
            // Integration layer.
            //
            var eventService = new EventService(mainForm);
            integrationLayer.AddSingleton<IJobService, JobService>();
            integrationLayer.AddSingleton<IEventService>(eventService);
            integrationLayer.AddSingleton<IGlobalSessionBroker, GlobalSessionBroker>();
            integrationLayer.AddSingleton<IProjectRepository>(new ProjectRepository(
                hkcu.CreateSubKey($@"{Globals.SettingsKeyPath}\Inventory")));

            //
            // Window & workflow layer.
            //
            windowAndWorkflowLayer.AddSingleton<IMainForm>(mainForm);
            windowAndWorkflowLayer.AddTransient<ICloudConsoleService, CloudConsoleService>();
            windowAndWorkflowLayer.AddTransient<HelpService>();
            windowAndWorkflowLayer.AddTransient<IProjectPickerWindow, ProjectPickerWindow>();
            windowAndWorkflowLayer.AddTransient<AboutWindow>();
            windowAndWorkflowLayer.AddTransient<IExceptionDialog, ExceptionDialog>();
            windowAndWorkflowLayer.AddTransient<IConfirmationDialog, ConfirmationDialog>();
            windowAndWorkflowLayer.AddTransient<ITaskDialog, TaskDialog>();
            windowAndWorkflowLayer.AddTransient<IUpdateService, UpdateService>();
            windowAndWorkflowLayer.AddSingleton<IProjectModelService, ProjectModelService>();
            windowAndWorkflowLayer.AddSingleton<IProjectExplorer, ProjectExplorerWindow>();
            windowAndWorkflowLayer.AddTransient<OptionsDialog>();

#if DEBUG
            windowAndWorkflowLayer.AddSingleton<DebugJobServiceWindow>();
            windowAndWorkflowLayer.AddSingleton<DebugDockingWindow>();
            windowAndWorkflowLayer.AddSingleton<DebugProjectExplorerTrackingWindow>();
            windowAndWorkflowLayer.AddSingleton<DebugFullScreenPane>();
            windowAndWorkflowLayer.AddSingleton<DebugFocusWindow>();
#endif
            //
            // Extension layer.
            //
            var extensionLayer = new ServiceRegistry(windowAndWorkflowLayer);
            foreach (var extension in LoadExtensionAssemblies())
            {
                extensionLayer.AddExtensionAssembly(extension);
            }

            // Run app.
            this.initializedMainForm = mainForm;
            this.mainFormInitialized.Set();

            System.Windows.Forms.Application.Run(mainForm);

            // Ensure logs are flushed.
            IsLoggingEnabled = false;

            return 0;
        }

        protected override int HandleSubsequentInvocation(string[] args)
        {
            var options = CommandLineOptions.ParseOrExit(args);

            // Make sure the main form is ready.
            this.mainFormInitialized.WaitOne();
            Debug.Assert(this.initializedMainForm != null);

            // This method is called on the named pipe server thread - switch to 
            // main thread before doing any GUI stuff.
            this.initializedMainForm.Invoke(((Action)(() =>
            {
                if (options.StartupUrl != null)
                {
                    this.initializedMainForm.ConnectToUrl(options.StartupUrl);
                }
            })));

            return 1;
        }

        protected override void HandleSubsequentInvocationException(Exception e)
            => ShowFatalError(e);

        private static void ShowFatalError(Exception e)
        {
            // NB. This could be called on any thread, at any time, so avoid
            // touching the main form.
            ErrorDialog.Show(e);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Parse command line to catch errors before even passing an invalid
            // command line to another instance of the app.
            CommandLineOptions.ParseOrExit(args);

            try
            {
                new Program().Run(args);
            }
            catch (Exception e)
            {
                ShowFatalError(e);
            }
        }
    }
}
