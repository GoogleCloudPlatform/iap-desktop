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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Google.Solutions.IapDesktop.Application.Services.SecureConnect
{
    /// <summary>
    /// Wrapper to interact with a Chrome native messaging host.
    /// See https://developer.chrome.com/apps/nativeMessaging for details.
    /// </summary>
    internal class ChromeNativeMessagingHost
    {
        private const string BaseKeyPath = @"Software\Google\chrome\NativeMessagingHosts";

        private readonly Process process;
        private readonly BinaryWriter input;
        private readonly BinaryReader output;

        //---------------------------------------------------------------------
        // System lookup.
        //---------------------------------------------------------------------

        internal static string FindNativeHelperLocationFromManifest(
            string extensionName,
            string manifestPath)
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
                return null;
            }

            using (var file = File.OpenText(manifestPath))
            using (var reader = new JsonTextReader(file))
            {
                var json = (JObject)JToken.ReadFrom(reader);
                if (!json.TryGetValue("name", out JToken name) ||
                    name.Value<string>() != extensionName)
                {
                    return null;
                }

                if (json.TryGetValue("path", out JToken binaryPath))
                {
                    return Path.GetFullPath(Path.Combine(
                        Path.GetDirectoryName(manifestPath),
                        binaryPath.Value<string>()));
                }
                else
                {
                    return null;
                }
            }
        }

        internal static string FindNativeHelperLocation(
            string extensionName,
            RegistryHive hive)
        {
            using (var hklm = RegistryKey.OpenBaseKey(hive, RegistryView.Registry32))
            {
                var keyPath = $"{BaseKeyPath}\\{extensionName}";
                using (var key = hklm.OpenSubKey(keyPath, false))
                {
                    if (key == null)
                    {
                        return null;
                    }

                    return FindNativeHelperLocationFromManifest(
                        extensionName,
                        (string)key.GetValue(null));
                }
            }
        }


        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public ChromeNativeMessagingHost(Process process)
        {
            this.process = process;
            this.input = new BinaryWriter(process.StandardInput.BaseStream);
            this.output = new BinaryReader(process.StandardOutput.BaseStream);
        }

        public static ChromeNativeMessagingHost Start(
            string extensionName,
            RegistryHive hive)
        {
            var location = FindNativeHelperLocation(extensionName, hive);
            if (location == null)
            {
                throw new ChromeNativeMessagingHostNotAvailableException(
                    $"Chrome extension '{extensionName}' not found");
            }

            var startupInfo = new ProcessStartInfo()
            {
                FileName = location,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            return new ChromeNativeMessagingHost(Process.Start(startupInfo));
        }

        public string TransactMessage(string request)
        {
            // Each message is serialized using JSON, UTF-8 encoded and is preceded with 
            // 32-bit message length in native byte order.

            var requestBytes = Encoding.UTF8.GetBytes(request);
            this.input.Write(requestBytes.Length);
            this.input.Write(requestBytes);
            this.input.Flush();

            var responseLength = this.output.ReadInt32();
            var responseBytes = this.output.ReadBytes(responseLength);

            return Encoding.UTF8.GetString(responseBytes);
        }

        public TResponse TransactMessage<TRequest, TResponse>(TRequest request)
        {
            return JsonConvert.DeserializeObject<TResponse>(
                TransactMessage(JsonConvert.SerializeObject(request)));
        }

        public void Dispose()
        {
            this.input.Dispose();
            this.output.Dispose();
            this.process.Dispose();
        }
    }

    public class ChromeNativeMessagingHostNotAvailableException : Exception
    { 
        public ChromeNativeMessagingHostNotAvailableException(string message) 
            : base(message)
        {
        }
    }
}
