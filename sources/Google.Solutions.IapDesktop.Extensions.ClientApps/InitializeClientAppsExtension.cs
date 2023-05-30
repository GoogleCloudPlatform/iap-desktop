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
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.ClientApps.Protocol.SqlServer;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.ClientApps
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton, DelayCreation = false)]
    public class InitializeClientAppsExtension
    {
        public InitializeClientAppsExtension(
            ProtocolRegistry protocolRegistry)
        {
            protocolRegistry.RegisterProtocol(
                new AppProtocol(
                    "SQL Server Management Studio",
                    Enumerable.Empty<ITrait>(),
                    new AllowAllPolicy(), // TODO: Use same job/process policy
                    Ssms.DefaultServerPort,
                    null,
                    new SsmsClient(Protocol.NetworkCredentialType.Rdp)));

            protocolRegistry.RegisterProtocol(
                new AppProtocol(
                    "SQL Server Management Studio as user...",
                    Enumerable.Empty<ITrait>(),
                    new AllowAllPolicy(), // TODO: Use same job/process policy
                    Ssms.DefaultServerPort,
                    null,
                    new SsmsClient(Protocol.NetworkCredentialType.Prompt)));

            protocolRegistry.RegisterProtocol(
                new AppProtocol(
                    "SQL Server Management Studio as SQL user...",
                    Enumerable.Empty<ITrait>(),
                    new AllowAllPolicy(), // TODO: Use same job/process policy
                    Ssms.DefaultServerPort,
                    null,
                    new SsmsClient(Protocol.NetworkCredentialType.Default)));
        }
    }
}
