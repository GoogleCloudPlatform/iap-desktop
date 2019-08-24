//
// Copyright 2019 Google LLC
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

using Google.Solutions.Compute;
using System;
using System.Linq;

namespace Google.Solutions.CloudIap.IapClient
{
    public class CommandLine
    {
        public VmInstanceReference InstanceReference { get; }
        public ushort Port { get; }

        public CommandLine(
            VmInstanceReference instanceReference,
            ushort port)
        {
            this.InstanceReference = instanceReference;
            this.Port = port;
        }

        public static CommandLine Parse(string[] args)
        {
            string projectId = null;
            string zone = null;
            string instanceName = null;
            string port = null;

            foreach (var arg in args)
            {
                if (projectId == null && arg.StartsWith("--project="))
                {
                    projectId = arg.Substring(10);
                }
                else if (zone == null && arg.StartsWith("--zone="))
                {
                    zone = arg.Substring(7);
                }
                else if (instanceName == null && port == null &&
                         !arg.StartsWith("-") && arg.Contains(':'))
                {
                    var parts = arg.Split(':');
                    instanceName = parts[0];
                    port = parts[1];
                }
                else
                {
                    throw new ArgumentException($"Unrecognized argument: {arg}");
                }
            }

            if (!string.IsNullOrWhiteSpace(projectId) &&
                !string.IsNullOrWhiteSpace(zone) &&
                !string.IsNullOrWhiteSpace(instanceName) &&
                !string.IsNullOrWhiteSpace(port) &&
                ushort.TryParse(port, out ushort portNumber))
            {
                return new CommandLine(
                    new VmInstanceReference(projectId, zone, instanceName),
                    portNumber);
            }
            else
            {
                throw new ArgumentException($"Invalid or insufficient arguments supplied");
            }
        }
    }
}
