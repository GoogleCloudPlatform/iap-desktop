﻿//
// Copyright 2022 Google LLC
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

using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace Google.Solutions.Platform.Net
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
        Chrome,

        /// <summary>
        /// Use Chrome in Guest mode if available.
        /// </summary>
        ChromeGuest
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
            else if (preference == BrowserPreference.ChromeGuest && ChromeBrowser.IsAvailable)
            {
                return new ChromeBrowser("--guest");
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
    }

    public class ChromeBrowser : Browser
    {
        private const string AppPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
        private static string ChromeExecutablePath { get; }

        private readonly string arguments;

        static ChromeBrowser()
        {
            using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
            using (var chromeAppPath = hive.OpenSubKey($@"{AppPath}\chrome.exe", false))
            {
                ChromeExecutablePath = (string)chromeAppPath?.GetValue(null);
            }
        }

        public ChromeBrowser(string arguments = null)
        {
            this.arguments = arguments;
        }

        public static bool IsAvailable => ChromeExecutablePath != null;

        public override void Navigate(Uri address)
        {
            using (Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = false,
                FileName = ChromeExecutablePath,
                Arguments = $"{this.arguments ?? string.Empty} \"{address}\""
            }))
            { };
        }
    }
}
