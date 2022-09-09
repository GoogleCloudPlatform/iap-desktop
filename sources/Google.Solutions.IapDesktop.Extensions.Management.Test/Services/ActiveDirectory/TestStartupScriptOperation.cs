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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Management.Services.ActiveDirectory;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.Services.ActiveDirectory
{
    [TestFixture]
    public class TestStartupScriptOperation
    {
        //---------------------------------------------------------------------
        // ReplaceStartupScript.
        //---------------------------------------------------------------------

        [Test]
        public void WhenGuardKeyExists_ThenReplaceStartupScriptThrowsException()
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

            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            computeEngineAdapter.Setup(a => a.UpdateMetadataAsync(
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
                computeEngineAdapter.Object))
            {
                ExceptionAssert.ThrowsAggregateException<InvalidOperationException>(
                    () => operation.ReplaceStartupScriptAsync(
                    "script",
                    CancellationToken.None).Wait());
            }
        }

        [Test]
        public async Task WhenMetadataEmpty_ThenReplaceStartupScriptSwapsScripts()
        {
            var metadata = new Metadata();

            var guardKeyName = "guard";
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            computeEngineAdapter.Setup(a => a.UpdateMetadataAsync(
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
                computeEngineAdapter.Object);

            await operation.ReplaceStartupScriptAsync(
                    "script",
                    CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.AreEquivalent(
                new[] { "windows-startup-script-ps1", guardKeyName },
                metadata.Items.Select(i => i.Key).ToList());
        }

        [Test]
        public async Task WhenGuardKeyDoesNotExist_ThenReplaceStartupScriptSwapsScripts()
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
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            computeEngineAdapter.Setup(a => a.UpdateMetadataAsync(
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
                computeEngineAdapter.Object);

            await operation.ReplaceStartupScriptAsync(
                    "script",
                    CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.AreEquivalent(
                new[] { "windows-startup-script-ps1", guardKeyName },
                metadata.Items.Select(i => i.Key).ToList());
        }

        //---------------------------------------------------------------------
        // SetMetadata.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenGuardKeyExists_ThenSetMetadataUpdatesMetadata()
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

            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            computeEngineAdapter.Setup(a => a.UpdateMetadataAsync(
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
                computeEngineAdapter.Object);

            await operation
                .SetMetadataAsync(
                    "foo", "bar",
                    CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.AreEquivalent(
                new[] { "foo", guardKeyName },
                metadata.Items.Select(i => i.Key).ToList());
        }
        //---------------------------------------------------------------------
        // RestoreStartupScripts.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenStartupScriptReplaced_ThenRestoreStartupScriptsRestoresMetadata()
        {
            Metadata metadata = new Metadata()
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
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            computeEngineAdapter.Setup(a => a.UpdateMetadataAsync(
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
                computeEngineAdapter.Object))
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

                Assert.AreEqual(1, metadata.Items.Count);
                Assert.AreEqual("windows-startup-script-ps1", metadata.Items[0].Key);
                Assert.AreEqual("original", metadata.Items[0].Value);
            }
        }
    }
}
