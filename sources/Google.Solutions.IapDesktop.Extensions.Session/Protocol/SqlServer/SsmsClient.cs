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

using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.SqlServer
{
    internal sealed class SsmsClient : IWindowsProtocolClient
    {
        private readonly Ssms ssms; // null if not found.

        public SsmsClient(NetworkCredentialType credentialType)
        {
            this.RequiredCredential = credentialType;
            Ssms.TryFind(out this.ssms);
        }

        //---------------------------------------------------------------------
        // INetonlyCredentialClient.
        //---------------------------------------------------------------------

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
                ? string.Empty  // SQL Server authentication.
                : " -E";        // Windows authentication.

            var endpoint = transport.Endpoint;
            return $"-S {endpoint.Address},{endpoint.Port}{authFlag}";
        }

        public bool Equals(IAppProtocolClient other)
        {
            return other is SsmsClient ssms &&
                ssms.RequiredCredential == this.RequiredCredential;
        }
    }
}
