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
            serviceRegistry.AddTransient<CloudConsoleService>();
            serviceRegistry.AddTransient<ProjectPickerDialog>();
            serviceRegistry.AddTransient<IExceptionDialog, ExceptionDialog>();
            serviceRegistry.AddTransient<ITunnelService, TunnelService>();
            serviceRegistry.AddSingleton<TunnelBrokerService>();

            serviceRegistry.AddSingleton<RemoteDesktopService>();
            serviceRegistry.AddSingleton<SerialLogService>();
            serviceRegistry.AddSingleton<ISettingsEditor, SettingsEditorWindow>();
            serviceRegistry.AddSingleton<IProjectExplorer, ProjectExplorerWindow>();

#if DEBUG
            serviceRegistry.AddSingleton<DebugWindow>();
#endif

            System.Windows.Forms.Application.Run(mainForm);
        }
    }
}
