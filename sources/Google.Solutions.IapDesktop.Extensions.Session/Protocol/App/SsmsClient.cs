//
// Copyright 2023 Google LLC
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

using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.App
{
    internal sealed class SsmsClient : IAppProtocolClient
    {
        private readonly Ssms? ssms; // null if not found.

        internal SsmsClient(Ssms? ssms)
        {
            this.ssms = ssms;
        }

        public SsmsClient()
            : this(TryFindSsms())
        { }

        private static Ssms? TryFindSsms()
        {
            Ssms.TryFind(out var ssms);
            return ssms;
        }

        //---------------------------------------------------------------------
        // IWindowsAppClient.
        //---------------------------------------------------------------------

        public bool IsUsernameRequired
        {
            //
            // SSMS always needs a username, otherwise the -U flag won't work.
            //
            get => true;
        }
        public bool IsNetworkLevelAuthenticationSupported
        {
            get => true;
        }

        public bool IsAvailable
        {
            get => this.ssms != null;
        }

        public string Executable
        {
            get => this.ssms?.ExecutablePath
                ?? throw new InvalidOperationException("SSMS is not available");
        }


        public string FormatArguments(
            ITransport transport,
            AppProtocolParameters parameters)
        {
            //
            // Create command line arguments based on
            // https://learn.microsoft.com/en-us/sql/ssms/ssms-utility?view=sql-server-ver16
            //

            string authFlag;
            if (parameters.NetworkLevelAuthentication == AppNetworkLevelAuthenticationState.Enabled)
            {
                //
                // Windows authentication.
                //
                authFlag = "-E";
            }
            else if (!string.IsNullOrWhiteSpace(parameters.PreferredUsername))
            {
                if (parameters.PreferredUsername!.Contains("\"") ||
                    parameters.PreferredUsername!.Contains("'"))
                {
                    throw new ArgumentException("The username contains invalid characters");
                }

                authFlag = $"-U \"{parameters.PreferredUsername}\"";
            }
            else
            {
                //
                // SQL Server authentication.
                //
                authFlag = "-U sa";
            }

            //
            // NB. SSMS uses the notation `host,port` instead of the more common
            // notation `host:port`.
            //
            // In case there is more than one SQL Server instance running on the
            // host, the port number uniquely identifies one of them. Therefore,
            // adding an instance name (like `host\instance,port`) isn't necessary 
            // and if we do anyway, SSMS ignores it.
            //
            // We take advantage of this behavior here by using the `\instance` part
            // to pass a "human readable" name of the VM to SSMS so that it's easier
            // to recognize the server in Object Explorer.
            // 
            string instancePart;
            if (transport.Target is InstanceLocator instance)
            {
                instancePart = new InternalDnsName.ZonalName(instance).Name;
            }
            else
            {
                instancePart = transport.Target.Name;
            }

            var endpoint = transport.Endpoint;
            return $"-S {endpoint.Address}\\{instancePart},{endpoint.Port} {authFlag}";
        }
    }
}
