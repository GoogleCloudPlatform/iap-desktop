using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public enum BrowserPreference
    {
        /// <summary>
        /// Use system default browser.
        /// </summary>
        Default,

        /// <summary>
        /// Use Chrome if available.
        /// </summary>
        Chrome
    }

    public interface IBrowserAdapter
    {
        /// <summary>
        /// Open browser and navigate to an address.
        /// </summary>
        void Navigate(Uri address, BrowserPreference preference);
    }

    public class BrowserAdapter : IBrowserAdapter
    {
        private const string AppPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

        public static string ChromeExecutablePath
        {
            get 
            {
                using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                using (var chromeAppPath = hive.OpenSubKey($@"{AppPath}\chrome.exe", false))
                {
                    return (string)chromeAppPath?.GetValue(null);
                }
            }
        }

        public static bool IsChromeInstalled => ChromeExecutablePath != null;

        //---------------------------------------------------------------------
        // IBrowserAdapter.
        //---------------------------------------------------------------------

        public void Navigate(Uri address, BrowserPreference preference)
        {
            if (preference == BrowserPreference.Chrome && IsChromeInstalled)
            {
                //
                // Launch Chrome.
                //
            }
            else
            {
                //
                // Use system-default browser.
                //
                using (Process.Start(new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    Verb = "open",
                    FileName = address.ToString()
                }))
                { };
            }
        }
    }
}
