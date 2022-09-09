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

using Google.Apis.Util;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshKeys
{
    public class AuthorizedPublicKeysModel
    {
        public IEnumerable<Item> Items { get; }

        public string DisplayName { get; }

        public IEnumerable<string> Warnings { get; }

        private AuthorizedPublicKeysModel(
            string displayName,
            IEnumerable<Item> items,
            IEnumerable<string> warnings)
        {
            this.DisplayName = displayName.ThrowIfNull(nameof(displayName));
            this.Items = items.EnsureNotNull();
            this.Warnings = warnings.EnsureNotNull();
        }

        public static async Task DeleteFromOsLoginAsync(
            IOsLoginService osLoginService,
            Item item,
            CancellationToken cancellationToken)
        {
            Utilities.ThrowIfNull(item, nameof(item));

            if (item.AuthorizationMethod == KeyAuthorizationMethods.Oslogin)
            {
                await osLoginService.DeleteAuthorizedKeyAsync(
                        item.Key,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public static async Task DeleteFromMetadataAsync(
            IComputeEngineAdapter computeEngineAdapter,
            IResourceManagerAdapter resourceManagerAdapter,
            IProjectModelNode node,
            Item item,
            CancellationToken cancellationToken)
        {
            Utilities.ThrowIfNull(item, nameof(item));

            if (item.AuthorizationMethod == KeyAuthorizationMethods.ProjectMetadata &&
                item.Key is MetadataAuthorizedPublicKey projectMetadataKey)
            {
                ProjectLocator project;
                if (node is IProjectModelProjectNode projectNode)
                {
                    project = projectNode.Project;
                }
                else if (node is IProjectModelInstanceNode instanceNode)
                {
                    project = new ProjectLocator(instanceNode.Instance.ProjectId);
                }
                else
                {
                    throw new ArgumentException(nameof(node));
                }

                var processor = await MetadataAuthorizedPublicKeyProcessor.ForProject(
                        computeEngineAdapter,
                        project,
                        cancellationToken)
                    .ConfigureAwait(false);
                await processor.RemoveAuthorizedKeyAsync(
                        projectMetadataKey,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (item.AuthorizationMethod == KeyAuthorizationMethods.InstanceMetadata &&
                node is IProjectModelInstanceNode instanceNode &&
                item.Key is MetadataAuthorizedPublicKey instanceMetadataKey)
            {
                var processor = await MetadataAuthorizedPublicKeyProcessor.ForInstance(
                        computeEngineAdapter,
                        resourceManagerAdapter,
                        instanceNode.Instance,
                        cancellationToken)
                    .ConfigureAwait(false);
                await processor.RemoveAuthorizedKeyAsync(
                        instanceMetadataKey,
                        item.AuthorizationMethod,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        //---------------------------------------------------------------------
        // Factory.
        //---------------------------------------------------------------------

        internal static bool IsNodeSupported(IProjectModelNode node)
        {
            return node is IProjectModelProjectNode ||
                (node is IProjectModelInstanceNode instanceNode &&
                    instanceNode.OperatingSystem == OperatingSystems.Linux);
        }

        internal static async Task<AuthorizedPublicKeysModel> LoadAsync(
            IComputeEngineAdapter computeEngineAdapter,
            IResourceManagerAdapter resourceManagerAdapter,
            IOsLoginService osLoginService,
            IProjectModelNode node,
            CancellationToken cancellationToken)
        {
            //
            // Kick off the tasks we need.
            //
            var osLoginKeysTask = osLoginService.ListAuthorizedKeysAsync(cancellationToken);

            Task<MetadataAuthorizedPublicKeyProcessor> metadataTask = null;
            if (!IsNodeSupported(node))
            {
                //
                // We don't support that kind of node.
                //
                return null;
            }
            else if (node is IProjectModelProjectNode projectNode)
            {
                metadataTask = MetadataAuthorizedPublicKeyProcessor.ForProject(
                        computeEngineAdapter,
                        projectNode.Project,
                        cancellationToken)
                    .ContinueWith(t => (MetadataAuthorizedPublicKeyProcessor)t.Result);
            }
            else if (node is IProjectModelInstanceNode instanceNode)
            {
                metadataTask = MetadataAuthorizedPublicKeyProcessor.ForInstance(
                        computeEngineAdapter,
                        resourceManagerAdapter,
                        instanceNode.Instance,
                        cancellationToken)
                    .ContinueWith(t => (MetadataAuthorizedPublicKeyProcessor)t.Result); ;
            }
            else
            {
                Debug.Fail("This case should not happen.");
                return null;
            }

            var processor = await metadataTask.ConfigureAwait(false);

            var items = new List<AuthorizedPublicKeysModel.Item>();
            string warning = null;

            if (processor.IsOsLoginEnabled)
            {
                //
                // OS Login enabled - only include OS Login keys them, since
                // all others are ignored anyway.
                //
                items.AddRange((await osLoginKeysTask.ConfigureAwait(false))
                    .Select(k => new AuthorizedPublicKeysModel.Item(k, KeyAuthorizationMethods.Oslogin))
                    .ToList());
                warning = "OS Login is enabled, the list only includes your personal OS Login keys.";
            }
            else
            {
                warning = "OS Login is disabled, the list only includes metadata-based keys.";
                //
                // OS Login disabled - consider metadata keys.
                //
                if (processor.AreProjectSshKeysBlocked)
                {
                    warning += " Project SSH keys are blocked.";
                }
                else
                {
                    items.AddRange(processor
                        .ListAuthorizedKeys(KeyAuthorizationMethods.ProjectMetadata)
                        .Select(k => new AuthorizedPublicKeysModel.Item(
                            k,
                            KeyAuthorizationMethods.ProjectMetadata)));
                }

                items.AddRange(processor
                    .ListAuthorizedKeys(KeyAuthorizationMethods.InstanceMetadata)
                    .Select(k => new AuthorizedPublicKeysModel.Item(
                        k,
                        KeyAuthorizationMethods.InstanceMetadata)));

            }

            return new AuthorizedPublicKeysModel(
                node.DisplayName,
                items,
                warning != null ? new[] { warning } : Enumerable.Empty<string>());
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public class Item
        {
            public IAuthorizedPublicKey Key { get; }

            public KeyAuthorizationMethods AuthorizationMethod { get; }

            internal Item(
                IAuthorizedPublicKey key,
                KeyAuthorizationMethods method)
            {
                Debug.Assert(method.IsSingleFlag());
                this.Key = key.ThrowIfNull(nameof(key));
                this.AuthorizationMethod = method;
            }
        }
    }
}
