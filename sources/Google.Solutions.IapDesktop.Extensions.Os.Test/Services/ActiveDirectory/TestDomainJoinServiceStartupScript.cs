﻿//
// Copyright 2022 Google LLC
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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Locator;
using Google.Solutions.Testing.Common.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Os.Services.Inventory;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Solutions.Testing.Common;
using Google.Solutions.Testing.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Os.Services.ActiveDirectory;
using Moq;
using System;
using System.Collections.Generic;
using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Text;
using Newtonsoft.Json;
using Google.Solutions.IapDesktop.Application.ObjectModel;

namespace Google.Solutions.IapDesktop.Extensions.Os.Test.Services.ActiveDirectory
{
    [TestFixture]
    [UsesCloudResources]
    public class TestDomainJoinServiceStartupScript : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // Hello message
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceUsesStartupScript_ThenAwaitHelloMessageSucceeds(
            [DomainJoinWindowsInstance] ResourceTask<InstanceLocator> instance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var computeEngineAdapter = new ComputeEngineAdapter(await credential))
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(180));

                using (var operation = new StartupScriptOperation(
                    Guid.Empty,
                    await instance,
                    DomainJoinService.MetadataKeys.JoinDomainGuard,
                    computeEngineAdapter))
                {
                    var hello = await new DomainJoinService(null)
                        .AwaitMessageAsync<DomainJoinService.HelloMessage>(
                            operation,
                            DomainJoinService.HelloMessage.MessageTypeString,
                            cts.Token)
                        .ConfigureAwait(false);

                    Assert.IsNotNull(hello);
                    Assert.AreEqual(Guid.Empty.ToString(), hello.OperationId);
                    Assert.AreEqual(DomainJoinService.HelloMessage.MessageTypeString, hello.MessageType);
                    Assert.IsNotEmpty(hello.Exponent);
                    Assert.IsNotEmpty(hello.Modulus);
                }
            }
        }

        //---------------------------------------------------------------------
        // JoinResponse message
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInstanceUsesStartupScript_ThenJoinFailsBecausePasswordCannotBeDecrypted(
            [DomainJoinWindowsInstance] ResourceTask<InstanceLocator> instance,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<ICredential> credential)
        {
            using (var computeEngineAdapter = new ComputeEngineAdapter(await credential))
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(180));

                using (var operation = new StartupScriptOperation(
                    Guid.Empty,
                    await instance,
                    DomainJoinService.MetadataKeys.JoinDomainGuard,
                    computeEngineAdapter))
                {
                    var response = await new DomainJoinService(null)
                        .AwaitMessageAsync<DomainJoinService.JoinResponse>(
                            operation,
                            DomainJoinService.JoinResponse.MessageTypeString,
                            cts.Token)
                        .ConfigureAwait(false);

                    Assert.IsNotNull(response);
                    Assert.AreEqual(Guid.Empty.ToString(), response.OperationId);
                    Assert.AreEqual(DomainJoinService.JoinResponse.MessageTypeString, response.MessageType);
                    Assert.IsFalse(response.Succeeded);
                    Assert.IsNotEmpty(response.ErrorDetails);
                }
            }
        }
    }

    public class DomainJoinWindowsInstanceAttribute : WindowsInstanceAttribute
    {
        public DomainJoinWindowsInstanceAttribute()
        {
            //
            // Use as sysprep script as the startup script is used for
            // determining readiness.
            //
            this.InitializeScript = DomainJoinService.CreateStartupScript(Guid.Empty);
        }

        protected override IEnumerable<Metadata.ItemsData> Metadata
        {
            get
            {
                var metadata = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = DomainJoinService.MetadataKeys.JoinDomain,
                        Value = JsonConvert.SerializeObject(new DomainJoinService.JoinRequest()
                        {
                            OperationId = Guid.Empty.ToString(),
                            MessageType = DomainJoinService.JoinRequest.MessageTypeString,
                            DomainName = "example.com",
                            Username = "admin",
                            EncryptedPassword = "" // Invalid cyphertext
                        })
                    }
                };
                metadata.AddRange(base.Metadata);
                return metadata;
            }
        }
    }
}
