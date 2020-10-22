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

namespace Google.Solutions.IapDesktop.Application.Services.Persistence
{
    public interface IAppProtocolRegistry
    {
        bool IsRegistered(string scheme, string applicationLocation);
        
        void Register(
            string scheme,
            string friendlyName,
            string applicationLocation);

        void Unregister(string scheme);
    }

    public class AppProtocolRegistry : IAppProtocolRegistry
    {
        private static string KeyPathFromScheme(string scheme)
            => $@"SOFTWARE\Classes\{scheme}";

        private static string CommandStringFromAppLocation(string applicationLocation)
            => $"\"{applicationLocation}\" /url \"%1\"";

        public void Register(
            string scheme,
            string friendlyName,
            string applicationLocation)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(KeyPathFromScheme(scheme)))
            {
                key.SetValue("", "URL:" + friendlyName);
                key.SetValue("URL Protocol", "");

                using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                {
                    defaultIcon.SetValue("", applicationLocation + ",1");
                }

                using (var commandKey = key.CreateSubKey(@"shell\open\command"))
                {
                    commandKey.SetValue("", CommandStringFromAppLocation(applicationLocation));
                }
            }
        }

        public bool IsRegistered(string scheme, string applicationLocation)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(KeyPathFromScheme(scheme) + @"\shell\open\command"))
            {
                if (key != null)
                {
                    var value = key.GetValue("", null);
                    return value is string && ((string)value) == CommandStringFromAppLocation(applicationLocation);
                }
                else
                {
                    return false;
                }
            }
        }

        public void Unregister(string scheme)
        {
            Registry.CurrentUser.DeleteSubKeyTree(KeyPathFromScheme(scheme), false);
        }
    }
}
