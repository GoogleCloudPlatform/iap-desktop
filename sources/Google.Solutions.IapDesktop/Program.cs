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

using Google.Solutions.Common;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Management;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Services.Windows;
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
using System.Windows.Forms;

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
            var deprecatedExtensions = new[]
            {
                "google.solutions.iapdesktop.extensions.rdp.dll",
                "google.solutions.iapdesktop.extensions.activity.dll",
                "google.solutions.iapdesktop.extensions.os.dll"
            };
            return Directory.GetFiles(
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location),
                    "*.Extensions.*.dll")
                .Where(name => !deprecatedExtensions.Contains(name.ToLower()))
                .Select(dllPath => Assembly.LoadFrom(dllPath));
        }

        private static Profile LoadProfileOrExit(CommandLineOptions options)
        {
            try
            {
                return Profile.OpenProfile(options.Profile);
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    e.Message,
                    "Profile", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);

                Environment.Exit(1);
                throw new InvalidOperationException();
            }
        }

        //---------------------------------------------------------------------
        // SingletonApplicationBase overrides.
        //---------------------------------------------------------------------

        private MainForm initializedMainForm = null;
        private readonly ManualResetEvent mainFormInitialized = new ManualResetEvent(false);

        private readonly CommandLineOptions commandLineOptions;

        internal Program(
            string name,
            CommandLineOptions commandLineOptions) 
            : base(name)
        {
            this.commandLineOptions = commandLineOptions;
        }

        protected override int HandleFirstInvocation(string[] args)
        {
            IsLoggingEnabled = this.commandLineOptions.IsLoggingEnabled;

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
            var baseLayer = new ServiceRegistry();
            var adapterLayer = new ServiceRegistry(baseLayer);
            var integrationLayer = new ServiceRegistry(adapterLayer);
            var windowAndWorkflowLayer = new ServiceRegistry(integrationLayer);

            // 
            // Persistence layer.
            //
            using (var profile = LoadProfileOrExit(this.commandLineOptions))
            {
                baseLayer.AddSingleton(profile);

                baseLayer.AddTransient<IExceptionDialog, ExceptionDialog>();
                baseLayer.AddTransient<IConfirmationDialog, ConfirmationDialog>();
                baseLayer.AddTransient<ITaskDialog, TaskDialog>();
                baseLayer.AddTransient<ICredentialDialog, CredentialDialog>();
                baseLayer.AddSingleton<IThemeService, ThemeService>();

                var appSettingsRepository = new ApplicationSettingsRepository(
                    profile.SettingsKey.CreateSubKey("Application"),
                    profile.MachinePolicyKey?.OpenSubKey("Application"),
                    profile.UserPolicyKey?.OpenSubKey("Application"));
                if (appSettingsRepository.IsPolicyPresent)
                {
                    //
                    // If there are policies in place, mark the UA as
                    // Enterprise-managed.
                    //
                    Globals.UserAgent.Extensions = "Enterprise";
                }

                baseLayer.AddTransient<IAppProtocolRegistry, AppProtocolRegistry>();
                baseLayer.AddSingleton(appSettingsRepository);
                baseLayer.AddSingleton(new ToolWindowStateRepository(
                    profile.SettingsKey.CreateSubKey("ToolWindows")));
                baseLayer.AddSingleton(new AuthSettingsRepository(
                    profile.SettingsKey.CreateSubKey("Auth"),
                    SignInAdapter.StoreUserId));

                var mainForm = new MainForm(baseLayer, windowAndWorkflowLayer)
                {
                    StartupUrl = this.commandLineOptions.StartupUrl
                };

                //
                // Adapter layer.
                //
                adapterLayer.AddSingleton<IAuthorizationSource>(mainForm);
                adapterLayer.AddSingleton<IJobHost>(mainForm);
                adapterLayer.AddTransient<IResourceManagerAdapter, ResourceManagerAdapter>();
                adapterLayer.AddTransient<IComputeEngineAdapter, ComputeEngineAdapter>();
                adapterLayer.AddTransient<IWindowsCredentialService, WindowsCredentialService>();
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
                    profile.SettingsKey.CreateSubKey("Inventory")));

                //
                // Window & workflow layer.
                //
                windowAndWorkflowLayer.AddSingleton<IMainForm>(mainForm);
                windowAndWorkflowLayer.AddTransient<ICloudConsoleService, CloudConsoleService>();
                windowAndWorkflowLayer.AddTransient<HelpService>();
                windowAndWorkflowLayer.AddTransient<IProjectPickerWindow, ProjectPickerWindow>();
                windowAndWorkflowLayer.AddTransient<AboutWindow>();
                windowAndWorkflowLayer.AddTransient<IOperationProgressDialog, OperationProgressDialog>();
                windowAndWorkflowLayer.AddTransient<IUpdateService, UpdateService>();
                windowAndWorkflowLayer.AddSingleton<IProjectModelService, ProjectModelService>();
                windowAndWorkflowLayer.AddSingleton<IProjectExplorer, ProjectExplorerWindow>();
                windowAndWorkflowLayer.AddTransient<OptionsDialog>();
                windowAndWorkflowLayer.AddTransient<IInstanceControlService, InstanceControlService>();

#if DEBUG
                windowAndWorkflowLayer.AddSingleton<DebugJobServiceWindow>();
                windowAndWorkflowLayer.AddSingleton<DebugDockingWindow>();
                windowAndWorkflowLayer.AddSingleton<DebugProjectExplorerTrackingWindow>();
                windowAndWorkflowLayer.AddSingleton<DebugFullScreenPane>();
                windowAndWorkflowLayer.AddSingleton<DebugFocusWindow>();
                windowAndWorkflowLayer.AddTransient<DebugThemeWindow>();
#endif
                //
                // Extension layer.
                //
                var extensionLayer = new ServiceRegistry(windowAndWorkflowLayer);
                foreach (var extension in LoadExtensionAssemblies())
                {
                    extensionLayer.AddExtensionAssembly(extension);
                }

                //
                // Run app.
                //
                this.initializedMainForm = mainForm;
                this.mainFormInitialized.Set();

                //
                // Replace the standard WinForms exception dialog.
                //
                System.Windows.Forms.Application.ThreadException += (_, exArgs)
                    => ShowFatalError(exArgs.Exception);

                System.Windows.Forms.Application.Run(mainForm);

                //
                // Ensure logs are flushed.
                //
                IsLoggingEnabled = false;

                return 0;
            }
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
            Environment.Exit(e.HResult);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //
            // Parse command line to catch errors before even passing an invalid
            // command line to another instance of the app.
            //
            var options = CommandLineOptions.ParseOrExit(args);

            try
            {
                var appName = "IapDesktop";
                if (options.Profile != null)
                {
                    //
                    // Incorporate the profile name (if provided) into the
                    // name of the singleton app so that instances can 
                    // coexist if thex use different profiles.
                    //
                    appName += $"_{options.Profile}";
                }

                new Program(appName, options).Run(args);
            }
            catch (Exception e)
            {
                ShowFatalError(e);
            }
        }

        internal static void LaunchNewInstance(CommandLineOptions options)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo()
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    Arguments = options.ToString(),
                    WindowStyle = ProcessWindowStyle.Normal
                };
                process.Start();
            }
        }
    }
}
