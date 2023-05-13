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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Auth;
using Google.Solutions.IapDesktop.Core.Auth;
using Google.Solutions.Ssh.Auth;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter
{
    public interface IKeyStoreAdapter
    {
        ISshKeyPair OpenSshKeyPair(
            SshKeyType keyType,
            IAuthorization authorization,
            bool createNewIfNotExists,
            IWin32Window window);
    }

    [Service(typeof(IKeyStoreAdapter))]
    public class KeyStoreAdapter : IKeyStoreAdapter
    {
        private static readonly CngProvider Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider;

        internal static string CreateKeyName(
            IAuthorization authorization,
            SshKeyType keyType,
            CngProvider provider)
        {
            if (keyType == SshKeyType.Rsa3072 &&
                provider == CngProvider.MicrosoftSoftwareKeyStorageProvider)
            {
                //
                // Use backwards-compatible name.
                //
                return $"IAPDESKTOP_{authorization.Email}";
            }
            else
            {
                //
                // Embed the key type and provider in the name. This ensures
                // that varying these parameters will yield a different name.
                //
                using (var sha = new SHA256Managed())
                {
                    //
                    // Instead of using the full provider name (which can be
                    // very long), hash the name and use the prefix.
                    //
                    var providerToken = BitConverter.ToString(
                        sha.ComputeHash(Encoding.UTF8.GetBytes(provider.Provider)),
                        0,
                        4).Replace("-", string.Empty);

                    return $"IAPDESKTOP_{authorization.Email}_{keyType:x}_{providerToken}";
                }
            }
        }

        internal void DeleteSshKeyPair(
            SshKeyType keyType,
            IAuthorization authorization)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(keyType))
            {
                SshKeyPair.DeletePersistentKeyPair(
                    CreateKeyName(authorization, keyType, Provider));
            }
        }

        //---------------------------------------------------------------------
        // IKeyStoreAdapter
        //---------------------------------------------------------------------

        public ISshKeyPair OpenSshKeyPair(
            SshKeyType keyType,
            IAuthorization authorization,
            bool createNewIfNotExists,
            IWin32Window window)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(keyType))
            {
                return SshKeyPair.OpenPersistentKeyPair(
                    CreateKeyName(authorization, keyType, Provider),
                    keyType,
                    Provider,
                    CngKeyUsages.Signing,
                    createNewIfNotExists,
                    window != null ? window.Handle : IntPtr.Zero);
            }
        }
    }
}
