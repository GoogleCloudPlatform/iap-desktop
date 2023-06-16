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

using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using System.Drawing;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.App
{
    internal sealed class SsmsClient : IWindowsAppClient
    {
        private readonly Ssms ssms; // null if not found.

        internal SsmsClient(
            Ssms ssms, 
            string name, 
            NetworkCredentialType requiredCredential)
        {
            this.ssms = ssms;
            this.Name = name;
            this.RequiredCredential = requiredCredential;
        }

        public SsmsClient(
            string name,
            NetworkCredentialType credentialType)
            : this(TryFindSsms(), name, credentialType)
        { }

        private static Ssms TryFindSsms()
        {
            Ssms.TryFind(out var ssms);
            return ssms;
        }

        //---------------------------------------------------------------------
        // IWindowsAppClient.
        //---------------------------------------------------------------------

        public string Name { get; }

        public Image Icon => this.ssms?.Icon;

        public NetworkCredentialType RequiredCredential { get; }

        public bool IsAvailable
        {
            get => this.ssms != null;
        }

        public string Executable
        {
            get => this.ssms?.ExecutablePath;
        }

        public string FormatArguments(ITransport transport)
        {
            //
            // Create command line arguments based on
            // https://learn.microsoft.com/en-us/sql/ssms/ssms-utility?view=sql-server-ver16
            //
            var authFlag = this.RequiredCredential == NetworkCredentialType.Default
                ? "-U sa"  // SQL Server authentication.
                : "-E";    // Windows authentication.

            var endpoint = transport.Endpoint;
            return $"-S {endpoint.Address},{endpoint.Port} {authFlag}";
        }
    }
}
