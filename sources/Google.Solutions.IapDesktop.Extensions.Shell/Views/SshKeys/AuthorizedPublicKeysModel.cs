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

using Google.Apis.Util;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        //---------------------------------------------------------------------
        // Factory.
        //---------------------------------------------------------------------

        internal static async Task<AuthorizedPublicKeysModel> LoadAsync(
            IComputeEngineAdapter computeEngineAdapter,
            IResourceManagerAdapter resourceManagerAdapter,
            IOsLoginService osLoginService,
            IProjectModelNode node,
            KeyAuthorizationMethods authorizationMethods,
            CancellationToken cancellationToken)
        {
            //
            // Kick off the tasks we need.
            //

            Task<MetadataAuthorizedPublicKeyProcessor> metadataTask = null;
            if (node is IProjectModelProjectNode projectNode)
            {
                if (authorizationMethods.HasFlag(KeyAuthorizationMethods.ProjectMetadata))
                {
                    metadataTask = MetadataAuthorizedPublicKeyProcessor.ForProject(
                            computeEngineAdapter,
                            projectNode.Project,
                            cancellationToken)
                        .ContinueWith(t => (MetadataAuthorizedPublicKeyProcessor)t.Result);
                }
            }
            else if (node is IProjectModelInstanceNode instanceNode)
            {
                if (authorizationMethods.HasFlag(KeyAuthorizationMethods.ProjectMetadata) ||
                    authorizationMethods.HasFlag(KeyAuthorizationMethods.InstanceMetadata))
                {
                    metadataTask = MetadataAuthorizedPublicKeyProcessor.ForInstance(
                            computeEngineAdapter,
                            resourceManagerAdapter,
                            instanceNode.Instance,
                            cancellationToken)
                        .ContinueWith(t => (MetadataAuthorizedPublicKeyProcessor)t.Result); ;
                }
            }
            else
            {
                //
                // We don't support that kind of node.
                //
                return null;
            }

            //
            // Consider OS Login keys (if requested).
            //
            Task<IEnumerable<IAuthorizedPublicKey>> osLoginKeysTask =
                authorizationMethods.HasFlag(KeyAuthorizationMethods.Oslogin)
                    ? osLoginService.ListAuthorizedKeysAsync(cancellationToken)
                    : null;

            var items = new List<AuthorizedPublicKeysModel.Item>();

            if (authorizationMethods.HasFlag(KeyAuthorizationMethods.Oslogin))
            {
                Debug.Assert(osLoginKeysTask != null);

                items.AddRange((await osLoginKeysTask.ConfigureAwait(false))
                    .Select(k => new AuthorizedPublicKeysModel.Item(k, KeyAuthorizationMethods.Oslogin))
                    .ToList());
            }

            //
            // Consider metadata keys (if requested).
            //

            string warning = null;
            if (authorizationMethods.HasFlag(KeyAuthorizationMethods.ProjectMetadata) ||
                authorizationMethods.HasFlag(KeyAuthorizationMethods.InstanceMetadata))
            {
                var processor = await metadataTask.ConfigureAwait(false);
                if (processor.IsOsLoginEnabled)
                {
                    warning = "OS Login enabled - keys from metadata are ignored";
                }
                else
                {
                    if (authorizationMethods.HasFlag(KeyAuthorizationMethods.ProjectMetadata))
                    {
                        if (processor.AreProjectSshKeysBlocked)
                        {
                            warning = "Project-wide keys are blocked";
                        }
                        else
                        {
                            items.AddRange(processor
                                .ListAuthorizedKeys(KeyAuthorizationMethods.ProjectMetadata)
                                .Select(k => new AuthorizedPublicKeysModel.Item(
                                    k,
                                    KeyAuthorizationMethods.ProjectMetadata)));
                        }
                    }

                    if (authorizationMethods.HasFlag(KeyAuthorizationMethods.InstanceMetadata))
                    {
                        items.AddRange(processor
                            .ListAuthorizedKeys(KeyAuthorizationMethods.InstanceMetadata)
                            .Select(k => new AuthorizedPublicKeysModel.Item(
                                k,
                                KeyAuthorizationMethods.InstanceMetadata)));
                    }
                }
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
