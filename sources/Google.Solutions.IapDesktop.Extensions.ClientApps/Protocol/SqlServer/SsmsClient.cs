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
using System;
using System.Diagnostics;
using System.Net;

namespace Google.Solutions.IapDesktop.Extensions.ClientApps.Protocol.SqlServer
{
    internal sealed class SsmsClient : INetonlyCredentialClient //TODO: Add tests
    {
        private readonly Authentication authentication;

        private string CreateCommandLine(IPEndPoint endpoint)
        {
            //
            // Create command line arguments based on
            // https://learn.microsoft.com/en-us/sql/ssms/ssms-utility?view=sql-server-ver16
            //
            var authFlag = authentication == Authentication.Windows
                ? " -E"
                : string.Empty;

            return $"-S {endpoint.Address},{endpoint.Port}{authFlag}";
        }

        public SsmsClient(Authentication authentication)
        {
            this.authentication = authentication;
        }

        //---------------------------------------------------------------------
        // INetonlyCredentialClient.
        //---------------------------------------------------------------------

        public bool RequireNetonlyCredential
        {
            get => this.authentication == Authentication.Windows;
        }

        public bool IsAvailable
        {
            get => Ssms.TryFind(out var _);
        }

        public bool Equals(IAppProtocolClient other)
        {
            return other is SsmsClient ssms &&
                ssms.authentication == this.authentication;
        }

        public Process Launch(ITransport endpoint)
        {
            if (this.authentication == Authentication.Windows &&
                this.RequireNetonlyCredential)
            {
                throw new InvalidOperationException(
                    "SSMS must be launched with netonly credentials");
            }

            throw new NotImplementedException(); //TODO: Create process
        }

        public Process LaunchWithNetonlyCredentials(
            ITransport endpoint,
            NetworkCredential credential)
        {
            if (!this.RequireNetonlyCredential)
            {
                return Launch(endpoint);
            }

            //TODO: Create process with netonly creds
            throw new NotImplementedException();
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public enum Authentication
        {
            SqlServer,
            Windows
        }
    }
}
