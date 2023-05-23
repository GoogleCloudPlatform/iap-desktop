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

using Google.Solutions.Apis.Diagnostics;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Compute
{
    public interface IAddressResolver
    {
        /// <summary>
        /// Lookup the IP address of an instance.
        /// </summary>
        Task<IPAddress> GetAddressAsync(
            InstanceLocator instance,
            NetworkInterfaceType type,
            CancellationToken cancellationToken);
    }

    public enum NetworkInterfaceType
    {
        /// <summary>
        /// Internal address of nic0.
        /// </summary>
        PrimaryInternal,

        /// <summary>
        /// External (1:1 NAT) address.
        /// </summary>
        External
    }

    public class AddressResolver : IAddressResolver
    {
        private readonly IComputeEngineAdapter computeEngine;

        public AddressResolver(IComputeEngineAdapter computeEngine)
        {
            this.computeEngine = computeEngine.ExpectNotNull(nameof(computeEngine));
        }

        //---------------------------------------------------------------------
        // IAddressResolver.
        //---------------------------------------------------------------------

        public async Task<IPAddress> GetAddressAsync(
            InstanceLocator instance,
            NetworkInterfaceType type,
            CancellationToken cancellationToken)
        {
            var instanceData = await this.computeEngine
                .GetInstanceAsync(instance, cancellationToken)
                .ConfigureAwait(false);

            switch (type)
            {
                case NetworkInterfaceType.PrimaryInternal:
                    {
                        return instanceData.PrimaryInternalAddress()
                            ?? throw new AddressNotFoundException(
                                "The VM instance doesn't have a suitable internal IPv4 address",
                                ComputeHelpTopics.LocateInstanceIpAddress);
                    }

                case NetworkInterfaceType.External:
                    {
                        return instanceData.PublicAddress()
                            ?? throw new AddressNotFoundException(
                                "The VM instance doesn't have an external IPv4 address",
                                ComputeHelpTopics.LocateInstanceIpAddress);
                    }

                default:
                    throw new ArgumentException(nameof(type));
            }
        }
    }

    public class AddressNotFoundException : AdapterException, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public AddressNotFoundException(string message, IHelpTopic help)
            : base(message)
        {
            this.Help = help;
        }
    }
}
