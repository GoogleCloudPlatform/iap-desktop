using Google.Solutions.IapDesktop.Windows;
using Google.Solutions.IapDesktop.Application.Settings;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Solutions.Compute.Auth;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.Windows.RemoteDesktop;
using Google.Solutions.IapDesktop.Application.SettingsEditor;
using Google.Solutions.IapDesktop.Application.ProjectExplorer;

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
            serviceRegistry.AddSingleton(new JobService(mainForm, serviceRegistry));
            serviceRegistry.AddSingleton<IEventService>(new EventService(mainForm));
            serviceRegistry.AddTransient<ProjectInventoryService>();
            serviceRegistry.AddTransient<ResourceManagerAdapter>();
            serviceRegistry.AddTransient<ComputeEngineAdapter>();
            serviceRegistry.AddTransient<CloudConsoleService>();
            serviceRegistry.AddTransient<ProjectPickerDialog>();

            serviceRegistry.AddSingleton<RemoteDesktopService>();
            serviceRegistry.AddSingleton<ISettingsEditor, SettingsEditorWindow>();
            serviceRegistry.AddSingleton<IProjectExplorer, ProjectExplorerWindow>();

#if DEBUG
            serviceRegistry.AddSingleton<DebugWindow>();
#endif

            System.Windows.Forms.Application.Run(mainForm);
        }
    }
}
