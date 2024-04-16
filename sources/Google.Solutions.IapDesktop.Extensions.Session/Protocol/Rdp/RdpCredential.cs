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

using System.Security;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp
{
    public class RdpCredential
    {
        internal static RdpCredential Empty = new RdpCredential(null, null, null);

        public string? User { get; }
        public SecureString? Password { get; }
        public string? Domain { get; }

        internal RdpCredential(
            string? user,
            string? domain,
            SecureString? password)
        {
            this.User = user;
            this.Password = password;
            this.Domain = domain;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(this.Domain) && !string.IsNullOrEmpty(this.User))
            {
                return $"{this.Domain}\\{this.User}";
            }
            else if (!string.IsNullOrEmpty(this.User))
            {
                return this.User!;
            }
            else
            {
                return "(empty)";
            }
        }
    }
}
