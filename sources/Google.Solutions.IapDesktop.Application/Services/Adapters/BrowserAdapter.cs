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

        private static string ChromeExecutablePath { get; }

        static BrowserAdapter()
        {
            using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
            using (var chromeAppPath = hive.OpenSubKey($@"{AppPath}\chrome.exe", false))
            {
                ChromeExecutablePath = (string)chromeAppPath?.GetValue(null);
            }
        }

        public static bool IsChromeInstalled => ChromeExecutablePath != null;

        //---------------------------------------------------------------------

        /// <summary>
        /// Open system default browser and navigate to address.
        /// </summary>
        /// <param name="url"></param>
        public static void Navigate(string url)
        {
            using (Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                Verb = "open",
                FileName = url
            }))
            { };
        }

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
                using (Process.Start(new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    FileName = ChromeExecutablePath,
                    Arguments = $"\"{address}\""
                }))
                { };
            }
            else
            {
                //
                // Use system-default browser.
                //
                Navigate(address.ToString());
            }
        }
    }
}
