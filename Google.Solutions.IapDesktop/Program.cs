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

            TempProgram.Services.AddSingleton(new WindowSettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Window")));
            TempProgram.Services.AddSingleton(new AuthSettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Auth"),
                OAuthAuthorization.StoreUserId));
            TempProgram.Services.AddSingleton(new InventorySettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Inventory")));


            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            var mainForm = new MainForm(TempProgram.Services);
            TempProgram.Services.AddSingleton<IMainForm>(mainForm);
            TempProgram.Services.AddSingleton<IAuthorizationService>(mainForm);
            TempProgram.Services.AddSingleton(new JobService(mainForm, TempProgram.Services));
            TempProgram.Services.AddSingleton<IEventService>(new EventService(mainForm));
            TempProgram.Services.AddTransient<ProjectInventoryService>();
            TempProgram.Services.AddTransient<ResourceManagerAdapter>();
            TempProgram.Services.AddTransient<ComputeEngineAdapter>();
            TempProgram.Services.AddTransient<CloudConsoleService>();


            System.Windows.Forms.Application.Run(mainForm);
        }
    }
}
