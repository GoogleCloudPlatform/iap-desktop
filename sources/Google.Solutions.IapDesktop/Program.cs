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
using Google.Solutions.Apis;
using Google.Solutions.Apis.Analytics;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Crm;
using Google.Solutions.Apis.Logging;
using Google.Solutions.Common;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Threading;
using Google.Solutions.Common.Util;
using Google.Solutions.Iap;
using Google.Solutions.Iap.Net;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Auth;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.ToolWindows.Update;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Auth;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Application.Windows.Options;
using Google.Solutions.IapDesktop.Application.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Windows.ProjectPicker;
using Google.Solutions.IapDesktop.Core;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Windows;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Diagnostics;
using Google.Solutions.Mvvm.Theme;
using Google.Solutions.Platform;
using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Platform.Net;
using Google.Solutions.Platform.Security;
using Google.Solutions.Platform.Security.Cryptography;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Ssh;
using Google.Solutions.Terminal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.IapDesktop
{
    public class Program : SingletonApplicationBase
    {
        private static bool tracingEnabled = false;

        private static readonly TraceSource[] TraceSources = new[]
        {
            ApiTraceSource.Log,
            PlatformTraceSource.Log,
            CommonTraceSource.Log,
            IapTraceSource.Log,
            SshTraceSource.Log,
            TerminalTraceSource.Log,
            ApplicationTraceSource.Log,
            CoreTraceSource.Log,
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

                    var logListener = new TextWriterTraceListener(new StreamWriter(logFilePath)
                    {
                        AutoFlush = true
                    });

                    foreach (var trace in TraceSources)
                    {
                        trace.Listeners.Add(new DefaultTraceListener());
                        trace.Listeners.Add(logListener);
                        trace.Switch.Level = SourceLevels.Verbose;
                    }
                }
                else
                {
                    foreach (var trace in TraceSources)
                    {
                        foreach (var listener in trace.Listeners.Cast<TraceListener>())
                        {
                            listener.Flush();
                        }

                        trace.Switch.Level = SourceLevels.Off;
                    }
                }
            }
        }

        private static IEnumerable<Assembly> LoadExtensionAssemblies()
        {
            var deprecatedExtensions = new[]
            {
                "google.solutions.iapdesktop.extensions.rdp.dll",
                "google.solutions.iapdesktop.extensions.activity.dll",
                "google.solutions.iapdesktop.extensions.os.dll",
                "google.solutions.iapdesktop.extensions.shell.dll"
            };

            return Directory.GetFiles(
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location),
                    "*.Extensions.*.dll")
                .Where(dllPath => !deprecatedExtensions.Contains(new FileInfo(dllPath).Name.ToLower()))
                .Select(dllPath => Assembly.LoadFrom(dllPath));
        }

        private static UserProfile LoadProfileOrExit(Install install, CommandLineOptions options)
        {
            try
            {
                return install.OpenProfile(options.Profile);
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
            using (var dialog = serviceProvider
                .GetService<WindowActivator<AuthorizeView, AuthorizeViewModel, IDialogTheme>>()
                .CreateDialog())
            {
                //
                // Initialize the view model.
                //
                dialog.ViewModel.DeviceEnrollment = DeviceEnrollment.Create(
                    new CertificateStore(),
                    serviceProvider.GetService<IRepository<IAccessSettings>>());
                dialog.ViewModel.ClientRegistrations = new List<OidcClientRegistration>()
                {
                    new OidcClientRegistration(
                        OidcIssuer.Gaia,
                        OAuthClient.Secrets.ClientId,
                        OAuthClient.Secrets.ClientSecret,
                        "/authorize/"),
                    new OidcClientRegistration(
                        OidcIssuer.Sts,
                        OAuthClient.SdkSecrets.ClientId,
                        OAuthClient.SdkSecrets.ClientSecret,
                        "/")
                };

                //
                // Allow recovery from common errors.
                //
                dialog.ViewModel.OAuthScopeNotGranted += (_, retryArgs) =>
                {
                    //
                    // User did not grant 'cloud-platform' scope.
                    //
                    using (var scopeDialog = serviceProvider
                        .GetService<WindowActivator<OAuthScopeNotGrantedView, OAuthScopeNotGrantedViewModel, IDialogTheme>>()
                        .CreateDialog())
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
                    var dialogParameters = new TaskDialogParameters(
                        "Authorization failed",
                        "IAP Desktop failed to complete the OAuth authorization. " +
                            "This might be due to network communication issues.",
                        retryArgs.Exception.Message)
                    {
                        Footnote = retryArgs.Exception.FullMessage(),
                        Icon = TaskDialogIcon.Error
                    };

                    dialogParameters.Buttons.Add(TaskDialogStandardButton.Cancel);
                    dialogParameters.Buttons.Add(new TaskDialogCommandLinkButton(
                        "Change network settings",
                        DialogResult.OK));

                    if (serviceProvider
                        .GetService<ITaskDialog>()
                        .ShowDialog(dialog.ViewModel.View, dialogParameters) == DialogResult.OK)
                    {
                        //
                        // Open settings.
                        //
                        retryArgs.Retry = OptionsDialog.Show(
                            dialog.ViewModel.View!,
                            (IServiceCategoryProvider)serviceProvider) == DialogResult.OK;
                    }
                };

                dialog.ViewModel.ShowOptions += (_, args) =>
                {
                    using (var scopeDialog = serviceProvider
                        .GetService<WindowActivator<AuthorizeOptionsView, AuthorizeOptionsViewModel, IDialogTheme>>()
                        .CreateDialog())
                    {
                        scopeDialog.ShowDialog(dialog.ViewModel.View);
                    }
                };

                if (dialog.ShowDialog(null) == DialogResult.OK)
                {
                    Debug.Assert(dialog.ViewModel.Authorization != null);

                    return dialog.ViewModel.Authorization!;
                }
                else
                {
                    //
                    // User closed the dialog without completing the sign-in.
                    //

                    //
                    // Ensure logs are flushed.
                    //
                    IsLoggingEnabled = false;

                    Environment.Exit(1);
                    throw new InvalidOperationException();
                }
            }
        }

        //---------------------------------------------------------------------
        // SingletonApplicationBase overrides.
        //---------------------------------------------------------------------

        private MainForm? initializedMainForm = null;
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
                ApplicationTraceSource.Log.TraceError(e);
            }

            //
            // Use the GDI-based TextRenderer class.
            //
            // NB. This must be set early, before the first window is created.
            //
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.EnableVisualStyles();

#if DEBUG
            ApplicationTraceSource.Log.Switch.Level = SourceLevels.Verbose;
            TerminalTraceSource.Log.Switch.Level = SourceLevels.Verbose;
#endif

            //
            // Install patches requires for IAP.
            //
            try
            {
                SystemPatch.UnrestrictUserAgentHeader.Install();
            }
            catch (InvalidOperationException e)
            {
                ApplicationTraceSource.Log.TraceWarning(
                    "Installing UnrestrictUserAgentHeader patch failed: {0}", e);
            }

            try
            {
                WebSocket.RegisterPrefixes();
                SystemPatch.SetUsernameAsHostHeaderForWssRequests.Install();
            }
            catch (Exception e)
            {
                ApplicationTraceSource.Log.TraceWarning(
                    "Installing SetUsernameAsHostHeaderForWssRequests patch failed: {0}", e);
            }

            //
            // Set up layers. Services in a layer can lookup services in a lower layer,
            // but not in a higher layer.
            //
            var preAuthLayer = new ServiceRegistry();

            var install = new Install(Install.DefaultBaseKeyPath);
            using (var profile = LoadProfileOrExit(install, this.commandLineOptions))
            using (var processFactory = new Win32ChildProcessFactory(true))
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
                preAuthLayer.AddSingleton<UserAgent>(Install.UserAgent);
                preAuthLayer.AddSingleton(profile);
                preAuthLayer.AddSingleton<IUserProfile>(profile);

                preAuthLayer.AddSingleton<IClock>(SystemClock.Default);
                preAuthLayer.AddTransient<IConfirmationDialog, ConfirmationDialog>();
                preAuthLayer.AddTransient<ITaskDialog, TaskDialog>();
                preAuthLayer.AddTransient<ICredentialDialog, CredentialDialog>();
                preAuthLayer.AddTransient<IInputDialog, InputDialog>();
                preAuthLayer.AddTransient<IExceptionDialog, ExceptionDialog>();
                preAuthLayer.AddTransient<IOperationProgressDialog, OperationProgressDialog>();
                preAuthLayer.AddTransient<INotifyDialog, NotifyDialog>();

                preAuthLayer.AddSingleton<IExternalRestClient, ExternalRestClient>();
                preAuthLayer.AddTransient<HelpClient>();
                preAuthLayer.AddTransient<BugReportClient>();
                preAuthLayer.AddTransient<IHttpProxyAdapter, HttpProxyAdapter>();

                preAuthLayer.AddSingleton<ProtocolRegistry>();

                preAuthLayer.AddSingleton<IWin32ProcessFactory>(processFactory);
                preAuthLayer.AddSingleton<IWin32ProcessSet>(processFactory);
                preAuthLayer.AddSingleton<IKeyStore>(
                    new KeyStore(CngProvider.MicrosoftSoftwareKeyStorageProvider));

                //
                // Initialize UI, DPI mode.
                //

                var themeSettingsRepository = new ThemeSettingsRepository(
                    profile.SettingsKey.CreateSubKey("Theme"));

                var dpiMode = (themeSettingsRepository.GetSettings().ScalingMode.Value) switch
                {
                    ScalingMode.None => DpiAwarenessMode.DpiUnaware,
                    ScalingMode.LegacyGdi => DpiAwarenessMode.SystemAware,
                    _ => DpiAwarenessMode.SystemAware,
                };

                if (DpiAwareness.IsSupported && dpiMode != DpiAwarenessMode.DpiUnaware)
                {
                    //
                    // Set DPI mode. 
                    //
                    // NB. Setting the DPI mode programmatically (instead of using the
                    // app manifest or app.config) does causes WinForms to *not* deliver
                    // DPI change (WM_DPICHANGE) events, but that's ok.
                    //
                    try
                    {
                        DpiAwareness.ProcessMode = dpiMode;
                    }
                    catch (Exception e)
                    {
                        ApplicationTraceSource.Log.TraceWarning(
                            "Setting DPI mode to {0} failed: {1}", dpiMode, e.Message);
                    }
                }

                //
                // Load settings.
                //
                var appSettingsRepository = new ApplicationSettingsRepository(profile);
                preAuthLayer.AddSingleton<IRepository<IApplicationSettings>>(appSettingsRepository);

                if (appSettingsRepository.IsPolicyPresent)
                {
                    //
                    // If there are policies in place, mark the UA as
                    // Enterprise-managed.
                    //
                    Install.UserAgent.Extensions = "Enterprise";
                }

                var authSettingsRepository = new AuthSettingsRepository(
                    profile.SettingsKey.CreateSubKey("Auth"));

                var accessSettingsRepository = new AccessSettingsRepository(
                    profile.SettingsKey.CreateSubKey("Application"),
                    profile.MachinePolicyKey?.OpenSubKey("Application"),
                    profile.UserPolicyKey?.OpenSubKey("Application"));

                preAuthLayer.AddSingleton<IRepository<IAuthSettings>>(authSettingsRepository);
                preAuthLayer.AddSingleton<IOidcOfflineCredentialStore>(authSettingsRepository);
                preAuthLayer.AddSingleton<IRepository<IAccessSettings>>(accessSettingsRepository);
                preAuthLayer.AddSingleton<IRepository<IThemeSettings>>(themeSettingsRepository);
                preAuthLayer.AddSingleton(new ToolWindowStateRepository(
                    profile.SettingsKey.CreateSubKey("ToolWindows")));
                preAuthLayer.AddSingleton<IBindingContext, ViewBindingContext>();
                preAuthLayer.AddTransient<IBrowserProtocolRegistry, BrowserProtocolRegistry>();

                //
                // Load themes.
                //
                var themes = Themes.Load(themeSettingsRepository);
                preAuthLayer.AddSingleton<ISystemDialogTheme>(themes.SystemDialog);
                preAuthLayer.AddSingleton<IDialogTheme>(themes.Dialog);
                preAuthLayer.AddSingleton<IToolWindowTheme>(themes.ToolWindow);
                preAuthLayer.AddSingleton<IMainWindowTheme>(themes.MainWindow);

                //
                // Configure networking settings.
                //
                // NB. Until now, no network connections have been made.
                //
                var appSettings = appSettingsRepository.GetSettings();

                //
                // Override default set of TLS versions.
                //
                ServicePointManager.SecurityProtocol = appSettings.TlsVersions.Value;

                try
                {
                    //
                    // Activate proxy settings based on app settings.
                    //
                    preAuthLayer.GetService<IHttpProxyAdapter>().ActivateSettings(appSettings);

                    PscAndMtlsAwareHttpClientFactory.NtlmProxyAuthenticationRetries =
                        (ushort)appSettings.ProxyAuthenticationRetries.Value;
                }
                catch (Exception)
                {
                    //
                    // Settings invalid -> ignore.
                    //
                }

                //
                // Register and configure API client endpoints.
                //
                var serviceRoute = ServiceRoute.Public;
                {
                    var accessSettings = accessSettingsRepository.GetSettings();
                    if (accessSettings.PrivateServiceConnectEndpoint.Value is var pscEndpoint &&
                        !string.IsNullOrEmpty(pscEndpoint))
                    {
                        //
                        // Enable PSC.
                        //
                        serviceRoute = new ServiceRoute(pscEndpoint);
                    }

                    //
                    // Set connection pool limit. This limit applies per endpoint.
                    //
                    ServicePointManager.DefaultConnectionLimit
                        = accessSettings.ConnectionLimit.Value;
                }

                preAuthLayer.AddSingleton(serviceRoute);
                preAuthLayer.AddSingleton(GaiaOidcClient.CreateEndpoint(serviceRoute));
                preAuthLayer.AddSingleton(WorkforcePoolClient.CreateEndpoint(serviceRoute));
                preAuthLayer.AddSingleton(ResourceManagerClient.CreateEndpoint(serviceRoute));
                preAuthLayer.AddSingleton(ComputeEngineClient.CreateEndpoint(serviceRoute));
                preAuthLayer.AddSingleton(OsLoginClient.CreateEndpoint(serviceRoute));
                preAuthLayer.AddSingleton(LoggingClient.CreateEndpoint(serviceRoute));
                preAuthLayer.AddSingleton(IapClient.CreateEndpoint(serviceRoute));

                //
                // Enable telemetry if the user allows it. Do this before
                // authorization takes place.
                //
                TelemetryLog.Current = new AnalyticsLog(
                    new MeasurementClient(
                        MeasurementClient.CreateEndpoint(),
                        Install.UserAgent,
                        AnalyticsStream.ApiKey,
                        AnalyticsStream.MeasurementId),
                    install,
                    new Dictionary<string, object>()
                    {
                        { DefaultParameters.UserAgent, Install.UserAgent.ToApplicationName() },
                        { DefaultParameters.UserAgentArchitecture, ProcessEnvironment.ProcessArchitecture.ToString() },
                        { DefaultParameters.UserAgentPlatformVersion, Environment.OSVersion.Version.ToString() },
                        { "osdrk", SystemTheme.ShouldAppsUseDarkMode ? "1" : "0" },
                        { "oscdp", DeviceCapabilities.Current.Dpi.ToString() },
                        { "ossdp", DeviceCapabilities.System.Dpi.ToString() },
                        { "apent", appSettingsRepository.IsPolicyPresent ? "1" : "0" },
                        { "apsrt", serviceRoute == ServiceRoute.Public ? "Public" : "PSC" },
                    })
                {
                    Enabled = appSettings.IsTelemetryEnabled.Value
                };

                preAuthLayer.AddSingleton(TelemetryLog.Current);
                preAuthLayer.AddTransient<AuthorizeView>();
                preAuthLayer.AddTransient<AuthorizeViewModel>();
                preAuthLayer.AddTransient<AuthorizeOptionsView>();
                preAuthLayer.AddTransient<AuthorizeOptionsViewModel>();
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
                mainLayer.AddTransient<IToolWindowHost, ToolWindowHost>();

                var mainForm = new MainForm(mainLayer)
                {
                    StartupUrl = this.commandLineOptions.StartupUrl,
                    ShowWhatsNew = this.commandLineOptions.IsPostInstall && install.PreviousVersion != null
                };

                mainLayer.AddSingleton<IJobHost>(mainForm);

                //
                // Load main services.
                //
                var eventService = new EventQueue(mainForm);

                //
                // Register API clients as singletons to ensure connection reuse.
                //
                mainLayer.AddSingleton<IResourceManagerClient, ResourceManagerClient>();
                mainLayer.AddSingleton<IComputeEngineClient, ComputeEngineClient>();
                mainLayer.AddSingleton<ILoggingClient, LoggingClient>();
                mainLayer.AddSingleton<IOsLoginClient, OsLoginClient>();
                mainLayer.AddSingleton<IIapClient, IapClient>();
                mainLayer.AddSingleton<IReleaseFeed>(new GithubClient(
                    new ExternalRestClient(),
                    OAuthClient.RepositoryName));

                mainLayer.AddTransient<IAddressResolver, AddressResolver>();
                mainLayer.AddTransient<IWindowsCredentialGenerator, WindowsCredentialGenerator>();
                mainLayer.AddSingleton<IJobService, JobService>();
                mainLayer.AddSingleton<IEventQueue>(eventService);
                mainLayer.AddSingleton<ISessionBroker, SessionBroker>();
                mainLayer.AddSingleton<IBrowser>(Browser.Default);

                var projectRepository = new ProjectRepository(profile.SettingsKey.CreateSubKey("Inventory"));
                mainLayer.AddSingleton<IProjectRepository>(projectRepository);
                mainLayer.AddSingleton<IProjectSettingsRepository>(projectRepository);
                mainLayer.AddSingleton<IProjectWorkspace, ProjectWorkspace>();
                mainLayer.AddTransient<ICloudConsoleClient, CloudConsoleClient>();
                mainLayer.AddTransient<IUpdatePolicy, UpdatePolicy>();
                mainLayer.AddSingleton<IIapTransportFactory, IapTransportFactory>();
                mainLayer.AddSingleton<IDirectTransportFactory, DirectTransportFactory>();

                //
                // Load windows.
                //
                mainLayer.AddSingleton<IMainWindow>(mainForm);
                mainLayer.AddSingleton<IWin32Window>(mainForm);
                mainLayer.AddTransient<AccessInfoFlyoutView>();
                mainLayer.AddTransient<AccessInfoViewModel>();
                mainLayer.AddTransient<NewProfileView>();
                mainLayer.AddTransient<NewProfileViewModel>();

                mainLayer.AddTransient<IProjectPickerDialog, ProjectPickerDialog>();
                mainLayer.AddTransient<ProjectPickerView>();
                mainLayer.AddTransient<ProjectPickerViewModel>();

                mainLayer.AddSingleton<IProjectExplorer, ProjectExplorer>();
                mainLayer.AddSingleton<ProjectExplorerView>();
                mainLayer.AddTransient<ProjectExplorerViewModel>();
                mainLayer.AddTransient<ReleaseNotesView>();
                mainLayer.AddTransient<ReleaseNotesViewModel>();
                mainLayer.AddSingleton<UrlCommands>();

                //
                // Load extensions.
                //
                foreach (var extension in LoadExtensionAssemblies())
                {
                    mainLayer.AddExtensionAssembly(extension);
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

                using (new DebugMessageThrottle(TimeSpan.FromMilliseconds(100)))
                using (var recorder = new MessageTraceRecorder(8))
                {
                    //
                    // Replace the standard WinForms exception dialog.
                    //
                    System.Windows.Forms.Application.ThreadException += (_, exArgs)
                        => ShowFatalErrorAndExit(exArgs.Exception, recorder.Capture());

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
                    try
                    {
                        System.Windows.Forms.Application.Run(mainForm);
                    }
                    catch (Exception e)
                    {
                        ShowFatalErrorAndExit(e, recorder.Capture());
                    }

                    if (processFactory.ChildProcesses > 0)
                    {
                        //
                        // Instead of killing child processes outright, give
                        // them a chance to close gracefully (and possibly
                        // save any work).
                        //
                        try
                        {
                            WaitDialog.Wait(
                                null,
                                "Waiting for applications to close...",
                                async cancellationToken =>
                                {
                                    //
                                    // Give child processes a fixed time to close,
                                    // but the user might cancel early.
                                    //
                                    using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                                    using (var combinedCts = timeoutCts.Token.Combine(cancellationToken))
                                    {
                                        await processFactory
                                            .CloseAsync(combinedCts.Token)
                                            .ConfigureAwait(true);
                                    }
                                });
                        }
                        catch (Exception e)
                        {
#if DEBUG
                            if (!e.IsCancellation())
                            {
                                ShowFatalErrorAndExit(e, recorder.Capture());
                            }
#else
                            _ = e;
#endif
                        }
                    }

                    //
                    // Ensure logs are flushed.
                    //
                    IsLoggingEnabled = false;

                    return 0;
                }
            }
        }

        protected override int HandleSubsequentInvocation(string[] args)
        {
            var options = CommandLineOptions.ParseOrExit(args);

            //
            // Make sure the main form is ready. If that's not the case
            // within a few seconds, then the process might be "stuck"
            // in the sign-in process.
            //
            if (!this.mainFormInitialized.WaitOne(TimeSpan.FromSeconds(3)))
            {
                throw new TimeoutException("The main form is not ready");
            }

            //
            // This method is called on the named pipe server thread - switch to 
            // main thread before doing any GUI stuff.
            //
            this.initializedMainForm?.Invoke(((Action)(() =>
            {
                if (options.StartupUrl != null)
                {
                    this.initializedMainForm.ConnectToUrl(options.StartupUrl);
                }
            })));

            return 1;
        }

        protected override void HandleSubsequentInvocationException(Exception e)
            => ShowFatalErrorAndExit(e, null);

        private static void ShowFatalErrorAndExit(Exception e, MessageTrace? messageTrace)
        {
            //
            // Ensure logs are flushed.
            //
            IsLoggingEnabled = false;

            TelemetryLog.Current.Write(
                "app_crash",
                new Dictionary<string, object>
                {
                    //
                    // Only include a base set of information that's
                    // safe to not contain any PII.
                    //
                    { "error", e.GetType().Name },
                    { "cause",  e.InnerException?.GetType().Name ?? string.Empty },
                    { "location", e.ToString(ExceptionFormatOptions.Compact) }
                });

            //
            // NB. This could be called on any thread, at any time, so avoid
            // touching the main form.
            //
            ErrorDialog.Show(new BugReport(typeof(Program), e)
            {
                WindowMessageTrace = messageTrace
            });
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
                ShowFatalErrorAndExit(e, null);
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