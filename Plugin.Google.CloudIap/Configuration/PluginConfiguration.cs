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

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Plugin.Google.CloudIap.Configuration
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
        [Description("Path to gcloud command line executable")]
        [DisplayName("Path to gcloud")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string GcloudCommandPathAsString
        {
            get { return this.GcloudCommandPath.FullName; }
            set { this.GcloudCommandPath = new FileInfo(value); }
        }

        [Browsable(false)]
        public FileInfo GcloudCommandPath { get; set; }

        [Category(DefaultCategoryName)]
        [Browsable(true)]
        [Description("Timeout for establishing a Cloud IAP tunnel")]
        [DisplayName("IAP Connection Timeout")]
        public TimeSpan IapConnectionTimeout { get; set; }
    }

    /// <summary>
    /// Store for plugin configuration that uses the HKCU registry hive.
    /// </summary>
    internal class PluginConfigurationStore : IDisposable
    {
        private const string RegistryPath = "Software\\Google\\RdcMan.Plugin\\1.0";
        private readonly TimeSpan DefaultIapConnectionTimeout = TimeSpan.FromSeconds(10);
        private readonly RegistryKey configKey;

        private PluginConfigurationStore(RegistryKey configKey)
        {
            this.configKey = configKey;
        }

        public static PluginConfigurationStore ForCurrentWindowsUser
        {
            get
            {
                return new PluginConfigurationStore(
                    RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).CreateSubKey(
                        RegistryPath,
                        true));
            }
        }

        public PluginConfiguration Configuration
        {
            get
            {
                return new PluginConfiguration()
                {
                    GcloudCommandPath = this.GcloudCommandPath,
                    IapConnectionTimeout = this.IapConnectionTimeout
                };
            }
            set
            {
                this.GcloudCommandPath = value.GcloudCommandPath;
                this.IapConnectionTimeout = value.IapConnectionTimeout;
            }
        }

        private static string FindGcloudInPath()
        {
            var path = System.Environment.GetEnvironmentVariable("PATH")
                .Split(';')
                .Select(x => Path.Combine(x, "gcloud.cmd"))
                   .Where(x => File.Exists(x))
                   .FirstOrDefault();

            return path;
        }

        public void Dispose()
        {
            this.configKey.Dispose();
        }

        public FileInfo GcloudCommandPath {
            get
            {
                var customValue = this.configKey.GetValue("GcloudCommandPath");
                if (customValue != null && customValue is string)
                {
                    return new FileInfo((string)customValue);
                }
                else
                {
                    return new FileInfo(FindGcloudInPath());
                }
            }
            set
            {
                if (value.FullName == FindGcloudInPath())
                {
                    // Do not save default values.
                    try
                    {
                        this.configKey.DeleteValue("GcloudCommandPath");
                    }
                    catch (Exception) { }
                }
                else
                {
                    this.configKey.SetValue(
                        "GcloudCommandPath",
                        value.FullName,
                        RegistryValueKind.ExpandString);
                }
            }
        }

        public TimeSpan IapConnectionTimeout {
            get
            {
                var customValue = this.configKey.GetValue("IapConnectionTimeout");
                if (customValue != null && customValue is int)
                {
                    return TimeSpan.FromMilliseconds((int)customValue);
                }
                else
                {
                    return DefaultIapConnectionTimeout;
                }
            }
            set
            {
                // Do not save default values.
                if (value == DefaultIapConnectionTimeout)
                {
                    try
                    {
                        this.configKey.DeleteValue("IapConnectionTimeout");
                    }
                    catch (Exception) { }
                }
                else
                {
                    this.configKey.SetValue(
                        "IapConnectionTimeout",
                        value.TotalMilliseconds,
                        RegistryValueKind.DWord);
                }
            }
        }
    }
}
