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

    public interface IBrowser
    {
        /// <summary>
        /// Open browser and navigate to an address.
        /// </summary>
        void Navigate(Uri address);

        /// <summary>
        /// Open browser and navigate to an address.
        /// </summary>
        void Navigate(string address);
    }

    public abstract class Browser : IBrowser
    {
        public static IBrowser Default { get; } = new SystemDefaultBrowser();

        public static IBrowser Get(BrowserPreference preference)
        {
            if (preference == BrowserPreference.Chrome && ChromeBrowser.IsAvailable)
            {
                return new ChromeBrowser();
            }
            else
            {
                return Default;
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public abstract void Navigate(Uri address);

        public void Navigate(string address) => Navigate(new Uri(address));

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class SystemDefaultBrowser : Browser
        {
            public override void Navigate(Uri address)
            {
                using (Process.Start(new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    Verb = "open",
                    FileName = address.ToString()
                }))
                { };
            }
        }

        private class ChromeBrowser : Browser
        {
            private const string AppPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

            private static string ChromeExecutablePath { get; }

            static ChromeBrowser()
            {
                using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                using (var chromeAppPath = hive.OpenSubKey($@"{AppPath}\chrome.exe", false))
                {
                    ChromeExecutablePath = (string)chromeAppPath?.GetValue(null);
                }
            }

            public static bool IsAvailable => ChromeExecutablePath != null;

            public override void Navigate(Uri address)
            {
                using (Process.Start(new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    FileName = ChromeExecutablePath,
                    Arguments = $"\"{address}\""
                }))
                { };
            }
        }
    }
}
