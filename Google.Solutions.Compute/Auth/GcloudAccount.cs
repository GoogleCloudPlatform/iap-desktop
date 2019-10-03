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

using Google.Apis.Auth.OAuth2;
using System;
using System.IO;
using System.Text;

namespace Google.Solutions.Compute.Auth
{
    /// <summary>
    /// Allows reading account information and credentials of the active
    /// GCloud account of the current user's profile.
    /// </summary>
    public class GcloudAccount
    {
        private readonly string account;

        public GcloudAccount(string account)
        {
            this.account = account;
        }

        internal static string FolderPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "gcloud");
            }
        }

        public static GcloudAccount ActiveAccount
        {
            get
            {
                var configPath = Path.Combine(FolderPath, "configurations", "config_default");
                if (!File.Exists(configPath))
                {
                    return null;
                }

                var value = new StringBuilder(128);
                if (UnsafeNativeMethods.GetPrivateProfileString(
                    "core", 
                    "account", 
                    null, 
                    value, 
                    value.Capacity,
                    configPath) < 0)
                {
                    throw new ApplicationException(
                        "No active account configured in gcloud configuration");
                }

                return new GcloudAccount(value.ToString());
            }
        }

        public string Name
        {
            get { return this.account; }
        }

        internal string CredentialFile
        {
            get
            {
                return Path.Combine(FolderPath, "legacy_credentials", this.account, "adc.json");
            }
        }

        internal GoogleCredential Credential
        {
            get
            {
                if (!File.Exists(this.CredentialFile))
                {
                    throw new ApplicationException(
                        $"No credentials found for active account '{this.account}'. "+
                        "Run 'gcloud auth login' to obtain valid credentials.");
                }
                return GoogleCredential.FromFile(this.CredentialFile);
            }
        }
    }
}
