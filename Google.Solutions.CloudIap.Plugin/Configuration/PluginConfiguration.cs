//
// Copyright 2019 Google LLC
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

using Google.Apis.Util.Store;
using Google.Solutions.Compute.Auth;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Google.Solutions.CloudIap.Plugin.Configuration
{
    /// <summary>
    /// Class encapsulating all plugin configuration, suitable to be used
    /// in a PropertyGrid.
    /// </summary>
    internal class PluginConfiguration
    {
        private const string TunnelingCategoryName = "Tunneling";
        private const string SoftwareCategoryName = "Software";

        //---------------------------------------------------------------------
        // Tunneling
        //---------------------------------------------------------------------

        [Category(TunnelingCategoryName)]
        [Browsable(true)]
        [Description("Path to gcloud.cmd in Cloud SDK installation folder")]
        [DisplayName("Path to gcloud")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string GcloudCommandPath { get; set; }

        [Category(TunnelingCategoryName)]
        [Browsable(true)]
        [Description("IAP tunneling implementation to use. Change requires application restart.")]
        [DisplayName("Tunneling implementation")]
        public Tunneler Tunneler { get; set; }

        [Category(TunnelingCategoryName)]
        [Browsable(true)]
        [Description("Timeout for establishing a Cloud IAP tunnel")]
        [DisplayName("IAP Connection Timeout")]
        public TimeSpan IapConnectionTimeout { get; set; }

        //---------------------------------------------------------------------
        // Software
        //---------------------------------------------------------------------

        [Category(SoftwareCategoryName)]
        [Browsable(true)]
        [Description("Automatically check for updates when closing application")]
        [DisplayName("Check for updates")]
        public bool CheckForUpdates { get; set; }

        [Category(SoftwareCategoryName)]
        [Browsable(true)]
        [Description("Version of plugin")]
        [DisplayName("Plugin version")]
        public Version Version => GetType().Assembly.GetName().Version;

        [Category(SoftwareCategoryName)]
        [Browsable(true)]
        [ReadOnly(true)]
        [Description("Last time an update check was performed")]
        [DisplayName("Last update check")]
        public DateTime LastUpdateCheck { get; set; }

        [Category(SoftwareCategoryName)]
        [Browsable(true)]
        [Description("Configure tracing. Traces can be viewed using DbgView. Note that verbose " +
                     "tracing degrades tunneling performance. Change requires restart.")]
        [DisplayName("Tracing")]
        public SourceLevels TracingLevel { get; set; }

        //---------------------------------------------------------------------
        // Hidden
        //---------------------------------------------------------------------

        [Browsable(false)]
        public string AppDataLocation => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Google\Cloud IAP Plugin");
    }

    public enum Tunneler
    {
        [Description("Default (built-in)")]
        Builtin = 0,

        [Description("gcloud (requires Cloud SDK)")]
        Gcloud = 1
    }

    /// <summary>
    /// Store for plugin configuration that uses the HKCU registry hive.
    /// </summary>
    internal class PluginConfigurationStore : IDisposable
    {
        private readonly int DefaultCheckForUpdates = 1;
        private readonly int DefaultIapConnectionTimeout = 30*1000;
        private readonly RegistryKey configKey;

        public PluginConfigurationStore(RegistryHive hive, string configKeyPath)
        {
            this.configKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default)
                                .CreateSubKey(configKeyPath, true);
        }

        public PluginConfiguration Configuration
        {
            get
            {
                return new PluginConfiguration()
                {
                    GcloudCommandPath = GetConfig(
                        "GcloudCommandPath",
                        RegistryValueKind.String,
                        FindGcloudInPath()),
                    IapConnectionTimeout = TimeSpan.FromMilliseconds(GetConfig<int>(
                        "IapConnectionTimeout",
                        RegistryValueKind.DWord,
                        DefaultIapConnectionTimeout)),
                    CheckForUpdates = GetConfig(
                        "CheckForUpdates",
                        RegistryValueKind.DWord,
                        DefaultCheckForUpdates) == 1,
                    LastUpdateCheck = DateTime.FromBinary(GetConfig<long>(
                        "LastUpdateCheck",
                        RegistryValueKind.QWord,
                        0)),
                    Tunneler = (Tunneler)GetConfig<int>(
                        "Tunneler",
                        RegistryValueKind.DWord,
                        (int)Tunneler.Builtin),
                    TracingLevel = (SourceLevels)GetConfig<int>(
                        "TracingLevel",
                        RegistryValueKind.DWord,
                        (int)SourceLevels.Off)
                };
            }
            set
            {
                SetConfig(
                    "GcloudCommandPath",
                    RegistryValueKind.String,
                    value.GcloudCommandPath,
                    FindGcloudInPath());
                SetConfig<int>(
                    "IapConnectionTimeout",
                    RegistryValueKind.DWord, 
                    (int)value.IapConnectionTimeout.TotalMilliseconds, 
                    DefaultIapConnectionTimeout);
                SetConfig<int>(
                   "CheckForUpdates",
                   RegistryValueKind.DWord,
                   value.CheckForUpdates ? 1 : 0,
                   DefaultCheckForUpdates);
                SetConfig<long>(
                   "LastUpdateCheck",
                   RegistryValueKind.QWord,
                   value.LastUpdateCheck.ToBinary(),
                   0);
                SetConfig<int>(
                    "Tunneler",
                    RegistryValueKind.DWord,
                    (int)value.Tunneler,
                    (int)Tunneler.Builtin);
                SetConfig<int>(
                    "TracingLevel",
                    RegistryValueKind.DWord,
                    (int)value.TracingLevel,
                    (int)SourceLevels.Off);
            }
        }

        private static string FindGcloudInPath()
        {
            var path = System.Environment.GetEnvironmentVariable("PATH");

            if (path != null)
            {
                path = path
                .Split(';')
                .Select(x => Path.Combine(x, "gcloud.cmd"))
                   .Where(x => File.Exists(x))
                   .FirstOrDefault();
            }

            return path;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.configKey.Dispose();
            }
        }
        
        private T GetConfig<T>(
            string name,
            RegistryValueKind kind,
            T defaultValue)
        {
            var customValue = this.configKey.GetValue(name);
            if (customValue != null && customValue is T)
            {
                return (T)customValue;
            }
            else
            {
                return defaultValue;
            }
        }

        private void SetConfig<T>(
            string name,
            RegistryValueKind kind,
            T value,
            T defaultValue)
        {
            if (value == null || value.Equals(defaultValue))
            {
                try
                {
                    this.configKey.DeleteValue(name);
                }
                catch (Exception) { }
            }
            else
            {
                this.configKey.SetValue(name, value, kind);
            }
        }
    }
}
