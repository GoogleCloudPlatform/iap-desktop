//
// Copyright 2020 Google LLC
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public class SecureConnectNativeHelperAdapter
    {
        private const string NativeHelperKey =
            @"Google\chrome\NativeMessagingHosts\com.google.secure_connect.native_helper";

        internal static string FindNativeHelperLocationFromManifest(string manifestPath)
        {
            //
            // The manifest typically looks like this:
            // {
            //  "name": "com.google.secure_connect.native_helper",
            //  "description": "Chrome Endpoint Verification Helper",
            //  "path": "NativeHelperWin.exe",
            //  "type": "stdio",
            //  "allowed_origins": [
            //    ...
            //  ]
            // }
            //

            if (manifestPath == null || !File.Exists(manifestPath))
            {
                throw new SecureConnectNotInstalledException();
            }

            using (var file = File.OpenText(manifestPath))
            using (var reader = new JsonTextReader(file))
            {
                var json = (JObject)JToken.ReadFrom(reader);
                if (!json.TryGetValue("name", out JToken name) ||
                    name.Value<string>() != "com.google.secure_connect.native_helper")
                {
                    throw new SecureConnectNotInstalledException();
                }

                if (json.TryGetValue("path", out JToken binaryPath))
                {
                    return Path.GetFullPath(Path.Combine(
                        Path.GetDirectoryName(manifestPath),
                        binaryPath.Value<string>()));
                }
                else
                {
                    throw new SecureConnectNotInstalledException();
                }
            }
        }

        internal static string FindNativeHelperLocation()
        {
            //
            // The native helper is a Chrome native messaging host
            // (see https://developer.chrome.com/apps/nativeMessaging), so
            // we first need to locate its manifest.
            //
            // Because the native helper is always installed by-machine (as
            // opposed to by-user), it's sufficient to look in HKLM.
            //
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (var key = hklm.OpenSubKey(NativeHelperKey, false))
                {
                    if (key == null)
                    {
                        throw new SecureConnectNotInstalledException();
                    }

                    return FindNativeHelperLocationFromManifest((string)key.GetValue(null));
                }
            }
        }
    }

    public class SecureConnectNotInstalledException : Exception
    {
        public SecureConnectNotInstalledException()
            : base("SecureConnect is not installed on this computer")
        { }

        public SecureConnectNotInstalledException(string message) 
            : base(message)
        {
        }

        public SecureConnectNotInstalledException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
