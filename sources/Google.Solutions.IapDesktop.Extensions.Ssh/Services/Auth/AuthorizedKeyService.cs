using Google.Apis.Compute.v1.Data;
using Google.Apis.Util;
using Google.Solutions.Common.ApiExtensions;
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Ssh.Services.Adapter;
using Google.Solutions.Ssh;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth
{
    interface IAuthorizedKeyService : IDisposable
    {
        Task<AuthorizedKey> AuthorizeKeyAsync(
            InstanceLocator instance,
            ISshKey key,
            TimeSpan keyValidity,
            string preferredPosixUsername,
            CancellationToken token);
    }

    [Service(typeof(IAuthorizedKeyService))]
    public class AuthorizedKeyService : IAuthorizedKeyService
    {
        private const string EnableOsLoginFlag = "enable-oslogin";
        private const string BlockProjectSshKeysFlag = "block-project-ssh-keys";

        private readonly IAuthorizationAdapter authorizationAdapter;
        private readonly IComputeEngineAdapter computeEngineAdapter;
        private readonly IOsLoginAdapter osLoginAdapter;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public AuthorizedKeyService(
            IAuthorizationAdapter authorizationAdapter,
            IComputeEngineAdapter computeEngineAdapter,
            IOsLoginAdapter osLoginAdapter)
        {
            this.authorizationAdapter = authorizationAdapter;
            this.computeEngineAdapter = computeEngineAdapter;
            this.osLoginAdapter = osLoginAdapter;
        }

        public AuthorizedKeyService(IServiceProvider serviceProvider)
            : this(
                  serviceProvider.GetService<IAuthorizationAdapter>(),
                  serviceProvider.GetService<IComputeEngineAdapter>(),
                  serviceProvider.GetService<IOsLoginAdapter>())
        { }

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

        private static void MergeKeyIntoMetadata(
            Metadata metadata,
            MetadataAuthorizedKey newKey)
        {
            //
            // Merge new key into existing keyset, and take 
            // the opportunity to purge expired keys.
            //
            var newKeySet = MetadataAuthorizedKeySet.FromMetadata(metadata)
                .RemoveExpiredKeys()
                .Add(newKey);
            metadata.Add(MetadataAuthorizedKeySet.MetadataKey, newKeySet.ToString());
        }

        private async Task PushPublicKeyToMetadataAsync(
            InstanceLocator instance,
            bool useInstanceKeySet,
            ManagedMetadataAuthorizedKey metadataKey,
            CancellationToken token)
        {
            try
            {
                if (useInstanceKeySet)
                {
                    await this.computeEngineAdapter.UpdateMetadataAsync(
                        instance,
                        metadata => MergeKeyIntoMetadata(metadata, metadataKey),
                        token)
                    .ConfigureAwait(false);
                }
                else
                {
                    await this.computeEngineAdapter.UpdateCommonInstanceMetadataAsync(
                       instance.ProjectId,
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
        // IPublicKeyService.
        //---------------------------------------------------------------------

        public async Task<AuthorizedKey> AuthorizeKeyAsync(
            InstanceLocator instance,
            ISshKey key,
            TimeSpan validity,
            string preferredPosixUsername,
            CancellationToken token)
        {
            Utilities.ThrowIfNull(instance, nameof(key));
            Utilities.ThrowIfNull(key, nameof(key));

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
                    //
                    // If OS Login is enabled for a project, we have to use
                    // the Posix username from the OS Login login profile.
                    //
                    // Note that:
                    //  - The username differs based on the organization the
                    //    project is part of.
                    //  - The login profile is empty if no public key has beem
                    //    pushed yet for this organization.
                    //

                    try
                    {
                        //
                        // NB. It's cheaper to unconditionally push the key than
                        // to check for previous keys first.
                        // 
                        return await this.osLoginAdapter.ImportSshPublicKeyAsync(
                                instance.ProjectId,
                                key,
                                validity,
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

                    //
                    // There is no legacy key, so we're good to push a new key.
                    // 
                    // Now figure out which username to use and where to push it.
                    //
                    var profile = AuthorizedKey.ForMetadata(
                        key,
                        preferredPosixUsername,
                        this.authorizationAdapter.Authorization);
                    Debug.Assert(profile.Username != null);

                    var metadataKey = new ManagedMetadataAuthorizedKey(
                        profile.Username,
                        key.Type,
                        key.PublicKeyString,
                        new ManagedKeyMetadata(
                            this.authorizationAdapter.Authorization.Email,
                            DateTime.UtcNow.Add(validity)));

                    var useInstanceKeySet = IsProjectSshKeysBlocked(await projectDetailsTask);
                    var existingKeySet = MetadataAuthorizedKeySet.FromMetadata(
                        useInstanceKeySet
                            ? instanceMetadata
                            : projectMetadata);

                    if (existingKeySet.Contains(metadataKey))
                    {
                        //
                        // The key is there already, so we are all set.
                        //
                        ApplicationTraceSources.Default.TraceVerbose(
                            "Existing SSH key found for {0}",
                            profile.Username);
                    }
                    else
                    {
                        ApplicationTraceSources.Default.TraceVerbose(
                            "Pushing new SSH key for {0}",
                            profile.Username);

                        await PushPublicKeyToMetadataAsync(
                            instance,
                            useInstanceKeySet,
                            metadataKey,
                            token)
                        .ConfigureAwait(false);
                    }

                    return profile;
                }
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.computeEngineAdapter.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
