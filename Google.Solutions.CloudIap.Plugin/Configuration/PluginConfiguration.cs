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
        private const string DefaultCategoryName = "General";

        [Category(DefaultCategoryName)]
        [Browsable(true)]
        [Description("Path to gcloud.cmd in Cloud SDK installation folder")]
        [DisplayName("Path to gcloud")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string GcloudCommandPath { get; set; }

        [Category(DefaultCategoryName)]
        [Browsable(true)]
        [Description("Timeout for establishing a Cloud IAP tunnel")]
        [DisplayName("IAP Connection Timeout")]
        public TimeSpan IapConnectionTimeout { get; set; }


        [Category(DefaultCategoryName)]
        [Browsable(true)]
        [Description("IAP tunneling implementation to use (change requires restart)")]
        [DisplayName("Tunneling implementation")]
        public Tunneler Tunneler { get; set; }

        [Category(DefaultCategoryName)]
        [Browsable(false)]
        [Description("Tracing level")]
        [DisplayName("Tracing level")]
        public SourceLevels TracingLevel { get; set; }
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
