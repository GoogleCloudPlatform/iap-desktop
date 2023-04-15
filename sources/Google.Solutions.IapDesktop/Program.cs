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

using Google.Apis.Util;
using Google.Solutions.Apis;
using Google.Solutions.CloudIap;
using Google.Solutions.Common;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Management;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Services.SecureConnect;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Theme;
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
using Google.Solutions.IapTunneling.Iap;
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
            ApiTraceSources.Google,
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

        private static IAuthorization AuthorizeOrExit(IServiceProvider serviceProvider)
        {
            var theme = serviceProvider.GetService<IThemeService>().DialogTheme;
            Debug.Assert(theme != null);

            using (var dialog = serviceProvider
                .GetDialog<AuthorizeView, AuthorizeViewModel>(theme))
            {
                //
                // Initialize the view model.
                //
                dialog.ViewModel.DeviceEnrollment = SecureConnectEnrollment.GetEnrollmentAsync(
                    new CertificateStoreAdapter(),
                    new ChromePolicy(),
                    serviceProvider.GetService<ApplicationSettingsRepository>()).Result;
                dialog.ViewModel.ClientSecrets = OAuthClient.Secrets;
                dialog.ViewModel.Scopes = new[] { IapTunnelingEndpoint.RequiredScope };
                dialog.ViewModel.TokenStore = serviceProvider.GetService<AuthSettingsRepository>();

                //
                // Allow recovery from common errors.
                //
                dialog.ViewModel.OAuthScopeNotGranted += (_, retryArgs) =>
                {
                    //
                    // User did not grant 'cloud-platform' scope.
                    //
                    using (var scopeDialog = serviceProvider
                        .GetDialog<OAuthScopeNotGrantedView, OAuthScopeNotGrantedViewModel>(theme))
                    {
                        retryArgs.Retry = scopeDialog.ShowDialog(dialog.ViewModel.View) == DialogResult.OK;
                    }
                };

                dialog.ViewModel.NetworkError += (_, retryArgs) =>
                {
                    //
                    // This exception might be due to a missing/incorrect proxy
                    // configuration, so give the user a chance to change proxy
                    // settings.
                    //
                    try
                    {
                        if (serviceProvider.GetService<ITaskDialog>()
                            .ShowOptionsTaskDialog(
                                dialog.ViewModel.View,
                                TaskDialogIcons.TD_ERROR_ICON,
                                "Authorization failed",
                                "IAP Desktop failed to complete the OAuth authorization. " +
                                    "This might be due to network communication issues.",
                                retryArgs.Exception.Message,
                                retryArgs.Exception.FullMessage(),
                                new[]
                                {
                            "Change network settings"
                                },
                                null,
                                out bool _) == 0)
                        {
                            //
                            // Open settings.
                            //
                            retryArgs.Retry = OptionsDialog.Show(
                                dialog.ViewModel.View,
                                (IServiceCategoryProvider)serviceProvider) == DialogResult.OK;
                        }
                    }
                    catch (OperationCanceledException)
                    { }
                };

                if (dialog.ShowDialog(null) == DialogResult.OK)
                {
                    Debug.Assert(dialog.ViewModel.Authorization != null);
                    return dialog.ViewModel.Authorization.Value;
                }
                else
                {
                    //
                    // User just closed the dialog.
                    //
                    Environment.Exit(1);
                    throw new InvalidOperationException();
                }
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
            var preAuthLayer = new ServiceRegistry();

            var install = new Install(Install.DefaultBaseKeyPath);
            using (var profile = LoadProfileOrExit(install, this.commandLineOptions))
            {
                Debug.Assert(!Install.IsExecutingTests);

                // 
                // Load pre-auth layer: Platform abstractions, API adapters.
                //
                // We can only load and access services that don't require
                // authorization. In particular, this means that we cannot access
                // any Google APIs.
                //
                preAuthLayer.AddSingleton<IInstall>(install);
                preAuthLayer.AddSingleton(profile);

                preAuthLayer.AddSingleton<IClock>(SystemClock.Default);
                preAuthLayer.AddTransient<IConfirmationDialog, ConfirmationDialog>();
                preAuthLayer.AddTransient<ITaskDialog, TaskDialog>();
                preAuthLayer.AddTransient<ICredentialDialog, CredentialDialog>();
                preAuthLayer.AddTransient<IExceptionDialog, ExceptionDialog>();
                preAuthLayer.AddTransient<IOperationProgressDialog, OperationProgressDialog>();

                preAuthLayer.AddSingleton<IExternalRestAdapter, ExternalRestAdapter>();
                preAuthLayer.AddTransient<HelpAdapter>();
                preAuthLayer.AddTransient<IGithubAdapter, GithubAdapter>();
                preAuthLayer.AddTransient<BuganizerAdapter>();
                preAuthLayer.AddTransient<ICloudConsoleAdapter, CloudConsoleAdapter>();
                preAuthLayer.AddTransient<IHttpProxyAdapter, HttpProxyAdapter>();

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
                    Install.UserAgent.Extensions = "Enterprise";
                }

                preAuthLayer.AddSingleton<IBindingContext, ViewBindingContext>();
                preAuthLayer.AddSingleton(new ThemeSettingsRepository(
                    profile.SettingsKey.CreateSubKey("Theme")));
                preAuthLayer.AddSingleton<IThemeService, ThemeService>();

                preAuthLayer.AddTransient<IAppProtocolRegistry, AppProtocolRegistry>();
                preAuthLayer.AddSingleton(appSettingsRepository);
                preAuthLayer.AddSingleton(new ToolWindowStateRepository(
                    profile.SettingsKey.CreateSubKey("ToolWindows")));
                preAuthLayer.AddSingleton(new AuthSettingsRepository(
                    profile.SettingsKey.CreateSubKey("Auth"),
                    SignInAdapter.StoreUserId));

                //
                // Configure networking settings.
                //
                // NB. Until now, no network connections have been made.
                //
                try
                {
                    var settings = preAuthLayer
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
                    preAuthLayer.GetService<IHttpProxyAdapter>().ActivateSettings(settings);
                }
                catch (Exception)
                {
                    // Settings invalid -> ignore.
                }

                preAuthLayer.AddTransient<AuthorizeView>();
                preAuthLayer.AddTransient<AuthorizeViewModel>();
                preAuthLayer.AddTransient<OAuthScopeNotGrantedView>();
                preAuthLayer.AddTransient<OAuthScopeNotGrantedViewModel>();
                preAuthLayer.AddTransient<PropertiesView>();
                preAuthLayer.AddTransient<PropertiesViewModel>();

                var authorization = AuthorizeOrExit(preAuthLayer);

                //
                // Authorization complete, now the main part of the application
                // can be initialized.
                //
                // Load main layer, containing everything else (except for
                // extensions).
                //
                var mainLayer = new ServiceRegistry(preAuthLayer);
                mainLayer.AddSingleton<IAuthorization>(authorization);

                var mainForm = new MainForm(mainLayer)
                {
                    StartupUrl = this.commandLineOptions.StartupUrl
                };

                mainLayer.AddSingleton<IJobHost>(mainForm);

                //
                // Load main services.
                //
                var eventService = new EventService(mainForm);

                //
                // Register adapters as singletons to ensure connection resuse.
                //
                mainLayer.AddSingleton<IResourceManagerAdapter, ResourceManagerAdapter>();
                mainLayer.AddSingleton<IComputeEngineAdapter, ComputeEngineAdapter>();

                mainLayer.AddTransient<IWindowsCredentialService, WindowsCredentialService>();
                mainLayer.AddSingleton<IJobService, JobService>();
                mainLayer.AddSingleton<IEventService>(eventService);
                mainLayer.AddSingleton<IGlobalSessionBroker, GlobalSessionBroker>();
                mainLayer.AddSingleton<IProjectRepository>(new ProjectRepository(
                    profile.SettingsKey.CreateSubKey("Inventory")));
                mainLayer.AddSingleton<IProjectModelService, ProjectModelService>();
                mainLayer.AddTransient<IInstanceControlService, InstanceControlService>();
                mainLayer.AddTransient<IUpdateService, UpdateService>();

                //
                // Load windows.
                //
                mainLayer.AddSingleton<IMainWindow>(mainForm);
                mainLayer.AddTransient<AboutView>();
                mainLayer.AddTransient<AboutViewModel>();
                mainLayer.AddTransient<DeviceFlyoutView>();
                mainLayer.AddTransient<DeviceFlyoutViewModel>();
                mainLayer.AddTransient<NewProfileView>();
                mainLayer.AddTransient<NewProfileViewModel>();

                mainLayer.AddTransient<IProjectPickerDialog, ProjectPickerDialog>();
                mainLayer.AddTransient<ProjectPickerView>();
                mainLayer.AddTransient<ProjectPickerViewModel>();

                mainLayer.AddSingleton<IProjectExplorer, ProjectExplorer>();
                mainLayer.AddSingleton<ProjectExplorerView>();
                mainLayer.AddTransient<ProjectExplorerViewModel>();
                mainLayer.AddSingleton<UrlCommands>();

#if DEBUG
                mainLayer.AddSingleton<DebugProjectExplorerTrackingView>();
                mainLayer.AddTransient<DebugProjectExplorerTrackingViewModel>();
                mainLayer.AddTransient<DebugThemeView>();
                mainLayer.AddTransient<DebugThemeViewModel>();
                mainLayer.AddSingleton<DebugJobServiceView>();
                mainLayer.AddTransient<DebugJobServiceViewModel>();
                mainLayer.AddTransient<DebugFullScreenView>();
                mainLayer.AddTransient<DebugFullScreenViewModel>();
                mainLayer.AddTransient<DebugDockingView>();
                mainLayer.AddTransient<DebugDockingViewModel>();
                mainLayer.AddTransient<DebugServiceRegistryView>();
                mainLayer.AddTransient<DebugServiceRegistryViewModel>();
                mainLayer.AddTransient<DebugCommonControlsView>();
                mainLayer.AddTransient<DebugCommonControlsViewModel>();
#endif
                //
                // Load extensions.
                //
                var extensionLayer = new ServiceRegistry(mainLayer);
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


                mainForm.Shown += (_, __) =>
                {
                    //
                    // Try to force the window into the foreground. This might
                    // not be allowed in all circumstances, but ensures that the
                    // window becomes visible after the user has completed a
                    // (browser-based) authorization.
                    //
                    TrySetForegroundWindow(Process.GetCurrentProcess().Id);
                };
                //
                // Show the main window.
                //
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
