using Google.Solutions.CloudIap.IapDesktop.Windows;
using Google.Solutions.CloudIap.IapDesktop.Application.Settings;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Solutions.Compute.Auth;
using Google.Solutions.IapDesktop.Application.ObjectModel;

namespace Google.Solutions.CloudIap.IapDesktop
{
    static class Program
    {
        private const string BaseRegistryKeyPath = @"Software\Google\IapDesktop\1.0";

        public static readonly ServiceRegistry Services = new ServiceRegistry();


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

            Services.AddSingleton(new WindowSettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Window")));
            Services.AddSingleton(new AuthSettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Auth"),
                OAuthAuthorization.StoreUserId));
            Services.AddSingleton(new InventorySettingsRepository(
                hkcu.CreateSubKey($@"{BaseRegistryKeyPath}\Inventory")));


            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            var mainForm = new MainForm();

            System.Windows.Forms.Application.Run(mainForm);
        }
    }
}
