//
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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.ActiveDirectory;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Test;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.GuestOs.ActiveDirectory
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
            [DomainJoinWindowsInstance] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var cts = new CancellationTokenSource())
            {
                var instance = await instanceTask;

                cts.CancelAfter(TimeSpan.FromSeconds(30));

                var computeClient = new ComputeEngineClient(
                    ComputeEngineClient.CreateEndpoint(),
                    await auth,
                    TestProject.UserAgent);
                using (var operation = new StartupScriptOperation(
                    Guid.Empty,
                    instance,
                    DomainJoinService.MetadataKeys.JoinDomainGuard,
                    computeClient))
                {
                    var hello = await new DomainJoinService(new Mock<IComputeEngineClient>().AsService())
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
            [DomainJoinWindowsInstance] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.ComputeInstanceAdminV1)] ResourceTask<IAuthorization> auth)
        {
            using (var cts = new CancellationTokenSource())
            {
                var instance = await instanceTask;

                cts.CancelAfter(TimeSpan.FromSeconds(30));

                var computeClient = new ComputeEngineClient(
                    ComputeEngineClient.CreateEndpoint(),
                    await auth,
                    TestProject.UserAgent);
                using (var operation = new StartupScriptOperation(
                    Guid.Empty,
                    instance,
                    DomainJoinService.MetadataKeys.JoinDomainGuard,
                    computeClient))
                {
                    var response = await new DomainJoinService(new Mock<IComputeEngineClient>().AsService())
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
        protected override IEnumerable<Metadata.ItemsData> Metadata
        {
            get
            {
                var metadata = base.Metadata.ToList();

                metadata.Add(new Metadata.ItemsData()
                {
                    Key = DomainJoinService.MetadataKeys.JoinDomain,
                    Value = JsonConvert.SerializeObject(new DomainJoinService.JoinRequest()
                    {
                        OperationId = Guid.Empty.ToString(),
                        MessageType = DomainJoinService.JoinRequest.MessageTypeString,
                        DomainName = "example.com",
                        Username = "admin",
                        EncryptedPassword = "" // Invalid ciphertext
                    })
                });

                metadata
                    .Find(i => i.Key == "windows-startup-script-ps1")
                    .Value += "\n\n" + DomainJoinService.CreateStartupScript(Guid.Empty);

                return metadata;
            }
        }
    }
}
