using Google.Apis.Compute.v1.Data;
using Google.Apis.Util;
using Google.Solutions.Common.ApiExtensions;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.Ssh;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth
{
    interface IPublicKeyService
    {
        Task PushPublicKeyAsync(
            InstanceLocator instance,
            string username,
            ISshKey key,
            CancellationToken token);
    }

    public class PublicKeyService : IPublicKeyService
    {
        private const string EnableOsLoginFlag = "enable-oslogin";
        private const string BlockProjectSshKeysFlag = "block-project-ssh-keys";

        private readonly IComputeEngineAdapter computeEngineAdapter;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public PublicKeyService(IComputeEngineAdapter computeEngineAdapter)
        {
            this.computeEngineAdapter = computeEngineAdapter;
        }

        //---------------------------------------------------------------------
        // Privates.
        //---------------------------------------------------------------------

        private bool IsOsLoginEnabled(Metadata metadata)
        {
            var enabled = metadata.GetValue(EnableOsLoginFlag);
            return enabled != null &&
                enabled.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsLegacySshKeyPresent(Metadata metadata)
        {
            return !string.IsNullOrEmpty(
                metadata.GetValue(MetadataAuthorizedKeySet.LegacyMetadataKey));
        }

        private bool IsProjectSshKeysBlocked(Project project)
        {
            return !string.IsNullOrEmpty(
                project.CommonInstanceMetadata.GetValue(BlockProjectSshKeysFlag));
        }

        private Task PushPublicKeyToInstanceMetadataAsync(
            InstanceLocator instance,
            string username,
            ISshKey key,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instance))
            {
                throw new NotImplementedException();
            }
        }

        private Task PushPublicKeyToProjectMetadataAsync(
            InstanceLocator instance,
            string username,
            ISshKey key,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instance))
            {
                throw new NotImplementedException();
            }
        }

        private Task PushPublicKeyToOsLoginAsync(
            InstanceLocator instance,
            string username,
            ISshKey key,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instance))
            {
                throw new NotImplementedException();
            }
        }

        //---------------------------------------------------------------------
        // IPublicKeyService.
        //---------------------------------------------------------------------

        public async Task PushPublicKeyAsync(
            InstanceLocator instance, 
            string username, 
            ISshKey key, 
            CancellationToken token)
        {
            Utilities.ThrowIfNull(instance, nameof(instance));
            Utilities.ThrowIfNullOrEmpty(username, nameof(username));
            Utilities.ThrowIfNull(instance, nameof(key));

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instance))
            {
                //
                // Query metadata for instance and project in parallel.
                //
                var instanceDetailsTask = this.computeEngineAdapter.GetInstanceAsync(
                        instance,
                        token)
                    .ConfigureAwait(false);
                var projectDetailsTask = this.computeEngineAdapter.GetProjectAsync(
                        instance.ProjectId,
                        token)
                    .ConfigureAwait(false);

                var instanceMetadata = (await instanceDetailsTask).Metadata;
                var projectMetadata = (await projectDetailsTask).CommonInstanceMetadata;
                
                var osLoginEnabled = IsOsLoginEnabled(instanceMetadata) || 
                                     IsOsLoginEnabled(projectMetadata);

                ApplicationTraceSources.Default.TraceVerbose(
                    "OS Login status for {0}: {1}", instance, osLoginEnabled);

                if (osLoginEnabled)
                {
                    try
                    {
                        await PushPublicKeyToOsLoginAsync(
                                instance,
                                username,
                                key,
                                token)
                            .ConfigureAwait(false);
                    }
                    catch (GoogleApiException)
                    {
                        // TODO: handle denied-by-policy
                        throw;
                    }
                }
                else
                {
                    //
                    // Check if there is a legacy SSH key. If there is one,
                    // other keys are ignored.
                    //
                    // NB. legacy SSH keys were instance-only, so checking
                    // the instance metadata is sufficient.
                    //
                    if (IsLegacySshKeyPresent(instanceMetadata))
                    {
                        throw new UnsupportedLegacySshKeyEncounteredException(
                            $"Instance {instance} uses legacy SSH keys",
                            HelpTopics.ManagingMetadataAuthorizedKeys);
                    }

                    try
                    {
                        if (IsProjectSshKeysBlocked(await projectDetailsTask))
                        {
                            //
                            // Project-level SSH keys are disabled, so push the
                            // key to the instance.
                            //

                            await PushPublicKeyToInstanceMetadataAsync(
                                    instance,
                                    username,
                                    key,
                                    token)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            //
                            // Push key to project (default behavior).
                            //

                            await PushPublicKeyToProjectMetadataAsync(
                                    instance,
                                    username,
                                    key,
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
            }
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
