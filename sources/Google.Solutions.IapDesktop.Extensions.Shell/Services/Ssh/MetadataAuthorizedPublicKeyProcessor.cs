//
// Copyright 2020 Google LLC
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
using Google.Apis.Util;
using Google.Solutions.Common.ApiExtensions;
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.Ssh.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh
{
    public class MetadataAuthorizedPublicKeyProcessor
    {
        private const string EnableOsLoginFlag = "enable-oslogin";
        private const string BlockProjectSshKeysFlag = "block-project-ssh-keys";

        private readonly IComputeEngineAdapter computeEngineAdapter;
        private readonly IResourceManagerAdapter resourceManagerAdapter;

        private readonly InstanceLocator instance;
        private readonly Instance instanceDetails;
        private readonly Project projectDetails;

        private MetadataAuthorizedPublicKeyProcessor(
            IComputeEngineAdapter computeEngineAdapter,
            IResourceManagerAdapter resourceManagerAdapter,
            InstanceLocator instance,
            Instance instanceDetails,
            Project projectDetails)
        {
            this.computeEngineAdapter = computeEngineAdapter;
            this.resourceManagerAdapter = resourceManagerAdapter;
            this.instance = instance;
            this.instanceDetails = instanceDetails;
            this.projectDetails = projectDetails;
        }

        private bool IsFlagEnabled(string flag)
        {
            var projectValue = this.projectDetails.CommonInstanceMetadata?.GetValue(flag);
            var instanceValue = this.instanceDetails.Metadata?.GetValue(flag);

            if (!string.IsNullOrEmpty(instanceValue))
            {
                // The instance takes precedence.
                return instanceValue.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            else if (!string.IsNullOrEmpty(projectValue))
            {
                // The project value only applies if the instance value was null.
                return projectValue.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return false;
            }
        }

        private static void MergeKeyIntoMetadata(
            Metadata metadata,
            MetadataAuthorizedPublicKey newKey)
        {
            //
            // Merge new key into existing keyset, and take 
            // the opportunity to purge expired keys.
            //
            var newKeySet = MetadataAuthorizedPublicKeySet.FromMetadata(metadata)
                .RemoveExpiredKeys()
                .Add(newKey);
            metadata.Add(MetadataAuthorizedPublicKeySet.MetadataKey, newKeySet.ToString());
        }

        private async Task PushPublicKeyToMetadataAsync(
            bool useInstanceKeySet,
            ManagedMetadataAuthorizedPublicKey metadataKey,
            CancellationToken token)
        {
            try
            {
                if (useInstanceKeySet)
                {
                    await this.computeEngineAdapter.UpdateMetadataAsync(
                        this.instance,
                        metadata => MergeKeyIntoMetadata(metadata, metadataKey),
                        token)
                    .ConfigureAwait(false);
                }
                else
                {
                    await this.computeEngineAdapter.UpdateCommonInstanceMetadataAsync(
                       this.instance.ProjectId,
                       metadata => MergeKeyIntoMetadata(metadata, metadataKey),
                       token)
                   .ConfigureAwait(false);
                }
            }
            catch (GoogleApiException e) when (e.Error == null || e.Error.Code == 403)
            {
                ApplicationTraceSources.Default.TraceVerbose(
                    "Setting request payload metadata failed with 403: {0} ({1})",
                    e.Message,
                    e.Error?.Errors.EnsureNotNull().Select(er => er.Reason).FirstOrDefault());

                // Setting metadata failed due to lack of permissions. Note that
                // the Error object is not always populated, hence the OR filter.

                throw new SshKeyPushFailedException(
                    "You do not have sufficient permissions to publish an SSH key. " +
                    "You need the 'Service Account User' and " +
                    "'Compute Instance Admin' roles (or equivalent custom roles) " +
                    "to perform this action.",
                    HelpTopics.ManagingMetadataAuthorizedKeys);
            }
            catch (GoogleApiException e) when (e.IsBadRequest())
            {
                ApplicationTraceSources.Default.TraceVerbose(
                    "Setting request payload metadata failed with 400: {0} ({1})",
                    e.Message,
                    e.Error?.Errors.EnsureNotNull().Select(er => er.Reason).FirstOrDefault());

                // This slightly weirdly encoded error happens if the user has the necessary
                // permissions on the VM, but lacks ActAs permission on the associated 
                // service account.

                throw new SshKeyPushFailedException(
                    "You do not have sufficient permissions to publish an SSH key. " +
                    "Because this VM instance uses a service account, you also need the " +
                    "'Service Account User' role.",
                    HelpTopics.ManagingMetadataAuthorizedKeys);
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public static async Task<MetadataAuthorizedPublicKeyProcessor> ForInstance(
            IComputeEngineAdapter computeEngineAdapter,
            IResourceManagerAdapter resourceManagerAdapter,
            InstanceLocator instance,
            CancellationToken token)
        {
            Utilities.ThrowIfNull(instance, nameof(instance));

            //
            // Query metadata for instance and project in parallel.
            //
            var instanceDetailsTask = computeEngineAdapter.GetInstanceAsync(
                    instance,
                    token)
                .ConfigureAwait(false);
            var projectDetailsTask = computeEngineAdapter.GetProjectAsync(
                    instance.ProjectId,
                    token)
                .ConfigureAwait(false);

            return new MetadataAuthorizedPublicKeyProcessor(
                computeEngineAdapter,
                resourceManagerAdapter,
                instance,
                await instanceDetailsTask,
                await projectDetailsTask);
        }

        public bool IsOsLoginEnabled 
            => IsFlagEnabled(EnableOsLoginFlag);

        public bool AreProjectSshKeysBlocked
            => IsFlagEnabled(BlockProjectSshKeysFlag);

        private bool IsLegacySshKeyPresent 
            => !string.IsNullOrEmpty(this.instanceDetails
                .Metadata
                .GetValue(MetadataAuthorizedPublicKeySet.LegacyMetadataKey));

        public async Task<AuthorizedKeyPair> AuthorizeKeyPairAsync(
            ISshKeyPair key,
            TimeSpan validity,
            string preferredPosixUsername,
            KeyAuthorizationMethods allowedMethods,
            IAuthorization authorization,
            CancellationToken cancellationToken)
        {
            var instanceMetadata = this.instanceDetails.Metadata;
            var projectMetadata = this.projectDetails.CommonInstanceMetadata;

            //
            // Check if there is a legacy SSH key. If there is one,
            // other keys are ignored.
            //
            // NB. legacy SSH keys were instance-only, so checking
            // the instance metadata is sufficient.
            //
            if (this.IsLegacySshKeyPresent)
            {
                throw new UnsupportedLegacySshKeyEncounteredException(
                    $"Connecting to the VM instance {instance.Name} is not supported " +
                    "because the instance uses legacy SSH keys in its metadata (sshKeys)",
                    HelpTopics.ManagingMetadataAuthorizedKeys);
            }

            //
            // There is no legacy key, so we're good to push a new key.
            // 
            // Now figure out which username to use and where to push it.
            //
            var blockProjectSshKeys = this.AreProjectSshKeysBlocked;

            bool useInstanceKeySet;
            if (allowedMethods.HasFlag(KeyAuthorizationMethods.ProjectMetadata) &&
                allowedMethods.HasFlag(KeyAuthorizationMethods.InstanceMetadata))
            {
                //
                // Both allowed - use project metadata unless:
                // - project keys are blocked
                // - we do not have the permission to update project metadata.
                //
                var canUpdateProjectMetadata = await this.resourceManagerAdapter
                    .IsGrantedPermissionAsync(
                        this.instance.ProjectId,
                        Permissions.ComputeProjectsSetCommonInstanceMetadata,
                        cancellationToken)
                    .ConfigureAwait(false);

                useInstanceKeySet = blockProjectSshKeys || !canUpdateProjectMetadata;
            }
            else if (allowedMethods.HasFlag(KeyAuthorizationMethods.ProjectMetadata))
            {
                // Only project allowed.
                if (blockProjectSshKeys)
                {
                    throw new InvalidOperationException(
                        $"Project {this.instance.ProjectId} does not allow project-level SSH keys");
                }
                else
                {
                    useInstanceKeySet = false;
                }
            }
            else if (allowedMethods.HasFlag(KeyAuthorizationMethods.InstanceMetadata))
            {
                // Only instance allowed.
                useInstanceKeySet = true;
            }
            else
            {
                // Neither project nor instance allowed.
                throw new ArgumentException(nameof(allowedMethods));
            }

            var authorizedKeyPair = AuthorizedKeyPair.ForMetadata(
                key,
                preferredPosixUsername,
                useInstanceKeySet,
                authorization);
            Debug.Assert(authorizedKeyPair.Username != null);

            var metadataKey = new ManagedMetadataAuthorizedPublicKey(
                authorizedKeyPair.Username,
                key.Type,
                key.PublicKeyString,
                new ManagedMetadataAuthorizedPublicKey.PublicKeyMetadata(
                    authorization.Email,
                    DateTime.UtcNow.Add(validity)));

            var existingKeySet = MetadataAuthorizedPublicKeySet.FromMetadata(
                useInstanceKeySet
                    ? instanceMetadata
                    : projectMetadata);

            if (existingKeySet
                .RemoveExpiredKeys()
                .Contains(metadataKey))
            {
                //
                // The key is there already, so we are all set.
                //
                ApplicationTraceSources.Default.TraceVerbose(
                    "Existing SSH key found for {0}",
                    authorizedKeyPair.Username);
            }
            else
            {
                //
                // Key not known yet, so we have to push it to
                // the metadata.
                //
                ApplicationTraceSources.Default.TraceVerbose(
                    "Pushing new SSH key for {0}",
                    authorizedKeyPair.Username);

                await PushPublicKeyToMetadataAsync(
                    useInstanceKeySet,
                    metadataKey,
                    cancellationToken)
                .ConfigureAwait(false);
            }

            return authorizedKeyPair;
        }
    }

    public class UnsupportedLegacySshKeyEncounteredException : Exception, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public UnsupportedLegacySshKeyEncounteredException(
            string message,
            IHelpTopic helpTopic)
            : base(message)
        {
            this.Help = helpTopic;
        }
    }

    public class SshKeyPushFailedException : Exception, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public SshKeyPushFailedException(
            string message,
            IHelpTopic helpTopic)
            : base(message)
        {
            this.Help = helpTopic;
        }
    }
}
