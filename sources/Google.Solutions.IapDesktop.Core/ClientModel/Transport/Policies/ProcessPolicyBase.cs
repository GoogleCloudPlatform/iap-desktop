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

using Google.Solutions.Platform.Net;
using System.Linq;
using System.Net;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies
{
    /// <summary>
    /// Policy for making access decisions based on the client process.
    /// </summary>
    public abstract class ProcessPolicyBase : ITransportPolicy
    {
        /// <summary>
        /// Decide whether a local process should be allowed access.
        /// </summary>
        protected internal abstract bool IsClientProcessAllowed(uint processId);

        //---------------------------------------------------------------------
        // ITransportPolicy.
        //---------------------------------------------------------------------

        public abstract string Name { get; }

        public bool IsClientAllowed(IPEndPoint remote)
        {
            //
            // NB. For connections from localhost, there are two
            // entries in the table - one tracking the outgoing
            // connection and one tracking the incoming connection.
            //
            // To find out the client process id, we need to find
            // the entry for the outgoing connection.
            //
            var tcpTableEntry = TcpTable.GetTcpTable2()
                .Where(e => e.LocalEndpoint.Equals(remote));

            //
            // Only permit access if the originating process is
            // the current process.
            //
            return remote.Address.Equals(IPAddress.Loopback) &&
                tcpTableEntry.Any() &&
                IsClientProcessAllowed(tcpTableEntry.First().ProcessId);
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ProcessPolicyBase);
        }

        public bool Equals(ITransportPolicy? other)
        {
            return other is ProcessPolicyBase &&
                other.GetType() == GetType() &&
                other != null;
        }

        public static bool operator ==(ProcessPolicyBase obj1, ProcessPolicyBase obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(ProcessPolicyBase obj1, ProcessPolicyBase obj2)
        {
            return !(obj1 == obj2);
        }


        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string ToString()
        {
            return this.Name;
        }
    }
}
