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

using Google.Apis.Util;
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
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.About;
using Google.Solutions.IapDesktop.Application.Views.Authorization;
using Google.Solutions.IapDesktop.Application.Views.Diagnostics;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Views.ProjectPicker;
using Google.Solutions.IapDesktop.Windows;
using Google.Solutions.IapTunneling;
using Google.Solutions.IapTunneling.Net;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Ssh;
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
    public class Program : SingletonApplicationBase
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
                .Where(dllPath => !deprecatedExtensions.Contains(new FileInfo(dllPath).Name.ToLower()))
                .Select(dllPath => Assembly.LoadFrom(dllPath));
        }

        private static Profile LoadProfileOrExit(Install install, CommandLineOptions options)
        {
            try
            {
                return Profile.OpenProfile(install, options.Profile);
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
            var serviceLayer = new ServiceRegistry(baseLayer);
            var windowLayer = new ServiceRegistry(serviceLayer);

            var install = new Install(Install.DefaultBaseKeyPath);
            using (var profile = LoadProfileOrExit(install, this.commandLineOptions))
            {
                // 
                // Load base layer: Platform abstractions, API adapters.
                //
                baseLayer.AddSingleton(install);
                baseLayer.AddSingleton(profile);

                baseLayer.AddSingleton<IClock>(SystemClock.Default);
                baseLayer.AddTransient<IConfirmationDialog, ConfirmationDialog>();
                baseLayer.AddTransient<ITaskDialog, TaskDialog>();
                baseLayer.AddTransient<ICredentialDialog, CredentialDialog>();
                baseLayer.AddTransient<IExceptionDialog, ExceptionDialog>();
                baseLayer.AddTransient<IOperationProgressDialog, OperationProgressDialog>();

                baseLayer.AddTransient<HelpAdapter>();
                baseLayer.AddTransient<IGithubAdapter, GithubAdapter>();
                baseLayer.AddTransient<BuganizerAdapter>();
                baseLayer.AddTransient<ICloudConsoleAdapter, CloudConsoleAdapter>();
                baseLayer.AddTransient<IHttpProxyAdapter, HttpProxyAdapter>();

                //
                // Register adapters as singletons to ensure connection resuse.
                //
                baseLayer.AddSingleton<IResourceManagerAdapter, ResourceManagerAdapter>();
                baseLayer.AddSingleton<IComputeEngineAdapter, ComputeEngineAdapter>();


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

                baseLayer.AddTransient<IBindingContext, ViewBindingContext>();
                baseLayer.AddSingleton(new ThemeSettingsRepository(
                    profile.SettingsKey.CreateSubKey("Theme")));
                baseLayer.AddSingleton<IThemeService, ThemeService>();

                baseLayer.AddTransient<IAppProtocolRegistry, AppProtocolRegistry>();
                baseLayer.AddSingleton(appSettingsRepository);
                baseLayer.AddSingleton(new ToolWindowStateRepository(
                    profile.SettingsKey.CreateSubKey("ToolWindows")));
                baseLayer.AddSingleton(new AuthSettingsRepository(
                    profile.SettingsKey.CreateSubKey("Auth"),
                    SignInAdapter.StoreUserId));

                //
                // Configure networking settings.
                //
                // NB. Until now, no network connections have been made.
                //
                try
                {
                    var settings = baseLayer
                        .GetService<ApplicationSettingsRepository>()
                        .GetSettings();

                    //
                    // Set connection pool limit. This limit applies per endpoint,
                    // and GCE, RM, OS Login, etc are all separate endpoints.
                    //
                    ServicePointManager.DefaultConnectionLimit = settings.ConnectionLimit.IntValue;

                    //
                    // Activate proxy settings based on app settings.
                    //
                    baseLayer.GetService<IHttpProxyAdapter>().ActivateSettings(settings);
                }
                catch (Exception)
                {
                    // Settings invalid -> ignore.
                }

                var mainForm = new MainForm(baseLayer, windowLayer)
                {
                    StartupUrl = this.commandLineOptions.StartupUrl
                };

                baseLayer.AddSingleton<IJobHost>(mainForm);
                baseLayer.AddSingleton<IAuthorizationSource>(mainForm);

                //
                // Load service layer: "Business" logic
                //
                var eventService = new EventService(mainForm);
                serviceLayer.AddTransient<IWindowsCredentialService, WindowsCredentialService>();
                serviceLayer.AddSingleton<IJobService, JobService>();
                serviceLayer.AddSingleton<IEventService>(eventService);
                serviceLayer.AddSingleton<IGlobalSessionBroker, GlobalSessionBroker>();
                serviceLayer.AddSingleton<IProjectRepository>(new ProjectRepository(
                    profile.SettingsKey.CreateSubKey("Inventory")));
                serviceLayer.AddSingleton<IProjectModelService, ProjectModelService>();
                serviceLayer.AddTransient<IInstanceControlService, InstanceControlService>();
                serviceLayer.AddTransient<IUpdateService, UpdateService>();

                //
                // Load window layer.
                //
                windowLayer.AddSingleton<IMainWindow>(mainForm);
                windowLayer.AddTransient<OAuthScopeNotGrantedView>();
                windowLayer.AddTransient<OAuthScopeNotGrantedViewModel>();
                windowLayer.AddTransient<AboutView>();
                windowLayer.AddTransient<AboutViewModel>();
                windowLayer.AddTransient<DeviceFlyoutView>();
                windowLayer.AddTransient<DeviceFlyoutViewModel>();
                windowLayer.AddTransient<NewProfileView>();
                windowLayer.AddTransient<NewProfileViewModel>();

                windowLayer.AddTransient<IProjectPickerDialog, ProjectPickerDialog>();
                windowLayer.AddTransient<ProjectPickerView>();
                windowLayer.AddTransient<ProjectPickerViewModel>();

                windowLayer.AddSingleton<IProjectExplorer, ProjectExplorer>();
                windowLayer.AddSingleton<ProjectExplorerView>();
                windowLayer.AddTransient<ProjectExplorerViewModel>();
                windowLayer.AddTransient<OptionsDialog>();

#if DEBUG
                windowLayer.AddSingleton<DebugProjectExplorerTrackingView>();
                windowLayer.AddTransient<DebugProjectExplorerTrackingViewModel>();
                windowLayer.AddTransient<DebugThemeView>();
                windowLayer.AddTransient<DebugThemeViewModel>();
                windowLayer.AddSingleton<DebugJobServiceView>();
                windowLayer.AddTransient<DebugJobServiceViewModel>();
                windowLayer.AddTransient<DebugFullScreenView>();
                windowLayer.AddTransient<DebugFullScreenViewModel>();
                windowLayer.AddTransient<DebugDockingView>();
                windowLayer.AddTransient<DebugDockingViewModel>();
                windowLayer.AddTransient<DebugServiceRegistryView>();
                windowLayer.AddTransient<DebugServiceRegistryViewModel>();
                windowLayer.AddTransient<DebugCommonControlsView>();
                windowLayer.AddTransient<DebugCommonControlsViewModel>();
#endif
                //
                // Load extensions.
                //
                var extensionLayer = new ServiceRegistry(windowLayer);
                foreach (var extension in LoadExtensionAssemblies())
                {
                    extensionLayer.AddExtensionAssembly(extension);
                }

                //
                // Run app.
                //
                this.initializedMainForm = mainForm;
                this.initializedMainForm.Shown += (_, __) =>
                {
                    //
                    // Form is now ready to handle subsequent invocations.
                    //
                    this.mainFormInitialized.Set();
                };
                this.initializedMainForm.FormClosing += (_, __) =>
                {
                    //
                    // Stop handling subsequent invocations.
                    //
                    this.initializedMainForm = null;
                };

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

            //
            // Make sure the main form is ready.
            //
            this.mainFormInitialized.WaitOne();
            if (this.initializedMainForm != null)
            {
                //
                // This method is called on the named pipe server thread - switch to 
                // main thread before doing any GUI stuff.
                //
                this.initializedMainForm.Invoke(((Action)(() =>
                {
                    if (options.StartupUrl != null)
                    {
                        this.initializedMainForm.ConnectToUrl(options.StartupUrl);
                    }
                })));
            }

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
        public static void Main(string[] args)
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
