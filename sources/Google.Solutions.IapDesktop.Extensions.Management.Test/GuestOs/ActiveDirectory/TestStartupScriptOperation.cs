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
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.ActiveDirectory;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.GuestOs.ActiveDirectory
{
    [TestFixture]
    public class TestStartupScriptOperation
    {
        //---------------------------------------------------------------------
        // ReplaceStartupScript.
        //---------------------------------------------------------------------

        [Test]
        public void ReplaceStartupScript_WhenGuardKeyExists()
        {
            var guardKeyName = "guard";
            var metadata = new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = guardKeyName
                    }
                }
            };

            var computeClient = new Mock<IComputeEngineClient>();
            computeClient.Setup(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()))
                .Callback((InstanceLocator loc, Action<Metadata> action, CancellationToken t) =>
                {
                    action(metadata);
                });

            using (var operation = new StartupScriptOperation(
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                guardKeyName,
                computeClient.Object))
            {
                ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                    () => operation.ReplaceStartupScriptAsync(
                    "script",
                    CancellationToken.None).Wait());
            }
        }

        [Test]
        public async Task ReplaceStartupScript_WhenMetadataEmpty_ThenSwapsScripts()
        {
            var metadata = new Metadata();

            var guardKeyName = "guard";
            var computeClient = new Mock<IComputeEngineClient>();
            computeClient.Setup(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()))
                .Callback((InstanceLocator loc, Action<Metadata> action, CancellationToken t) =>
                {
                    action(metadata);
                });

            var operation = new StartupScriptOperation(
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                guardKeyName,
                computeClient.Object);

            await operation.ReplaceStartupScriptAsync(
                    "script",
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(
                metadata.Items.Select(i => i.Key).ToList(), Is.EquivalentTo(new[] { "windows-startup-script-ps1", guardKeyName }));
        }

        [Test]
        public async Task ReplaceStartupScript_WhenGuardKeyDoesNotExist_ThenSwapsScripts()
        {
            var metadata = new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = "windows-startup-script-url"
                    },
                    new Metadata.ItemsData()
                    {
                        Key = "windows-startup-script-cmd"
                    },
                    new Metadata.ItemsData()
                    {
                        Key = "windows-startup-script-ps1"
                    }
                }
            };

            var guardKeyName = "guard";
            var computeClient = new Mock<IComputeEngineClient>();
            computeClient.Setup(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()))
                .Callback((InstanceLocator loc, Action<Metadata> action, CancellationToken t) =>
                {
                    action(metadata);
                });

            var operation = new StartupScriptOperation(
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                guardKeyName,
                computeClient.Object);

            await operation.ReplaceStartupScriptAsync(
                    "script",
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(
                metadata.Items.Select(i => i.Key).ToList(), Is.EquivalentTo(new[] { "windows-startup-script-ps1", guardKeyName }));
        }

        //---------------------------------------------------------------------
        // SetMetadata.
        //---------------------------------------------------------------------

        [Test]
        public async Task SetMetadata_WhenGuardKeyExists_ThenSetMetadataUpdatesMetadata()
        {
            var guardKeyName = "guard";
            var metadata = new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = guardKeyName
                    }
                }
            };

            var computeClient = new Mock<IComputeEngineClient>();
            computeClient.Setup(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()))
                .Callback((InstanceLocator loc, Action<Metadata> action, CancellationToken t) =>
                {
                    action(metadata);
                });

            var operation = new StartupScriptOperation(
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                guardKeyName,
                computeClient.Object);

            await operation
                .SetMetadataAsync(
                    "foo", "bar",
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.That(
                metadata.Items.Select(i => i.Key).ToList(), Is.EquivalentTo(new[] { "foo", guardKeyName }));
        }

        //---------------------------------------------------------------------
        // RestoreStartupScripts.
        //---------------------------------------------------------------------

        [Test]
        public async Task RestoreStartupScripts_WhenStartupScriptReplaced()
        {
            var metadata = new Metadata()
            {
                Items = new List<Metadata.ItemsData>()
                {
                    new Metadata.ItemsData()
                    {
                        Key = "windows-startup-script-ps1",
                        Value = "original"
                    }
                }
            };

            var guardKeyName = "guard";
            var computeClient = new Mock<IComputeEngineClient>();
            computeClient.Setup(a => a.UpdateMetadataAsync(
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<Action<Metadata>>(),
                    It.IsAny<CancellationToken>()))
                .Callback((InstanceLocator loc, Action<Metadata> action, CancellationToken t) =>
                {
                    action(metadata);
                });

            using (var operation = new StartupScriptOperation(
                new InstanceLocator("project-1", "zone-1", "instance-1"),
                guardKeyName,
                computeClient.Object))
            {
                await operation
                    .ReplaceStartupScriptAsync(
                        "script",
                        CancellationToken.None)
                    .ConfigureAwait(false);

                await operation
                    .SetMetadataAsync(
                        "foo", "bar",
                        CancellationToken.None)
                    .ConfigureAwait(false);

                await operation.RestoreStartupScriptsAsync(CancellationToken.None);

                Assert.That(metadata.Items.Count, Is.EqualTo(1));
                Assert.That(metadata.Items[0].Key, Is.EqualTo("windows-startup-script-ps1"));
                Assert.That(metadata.Items[0].Value, Is.EqualTo("original"));
            }
        }
    }
}
