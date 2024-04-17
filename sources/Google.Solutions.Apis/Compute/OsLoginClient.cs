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

using Google.Apis.CloudOSLogin.v1;
using Google.Apis.CloudOSLogin.v1.Data;
using Google.Apis.Discovery;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Auth.Iam;
using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Diagnostics;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Google.Solutions.Apis.Compute.OsLoginClient;

namespace Google.Solutions.Apis.Compute
{
    /// <summary>
    /// Client for OS Login API.
    /// </summary>
    public interface IOsLoginClient : IClient
    {
        /// <summary>
        /// Import user's public key to OS Login.
        /// </summary>
        /// <param name="key">public key, in OpenSSH format</param>
        Task<LoginProfile> ImportSshPublicKeyAsync(
            ProjectLocator project,
            string key,
            TimeSpan validity,
            CancellationToken token);

        /// <summary>
        /// Certify a user's public key.
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="key">public key, in OpenSSH format</param>
        /// <returns></returns>
        Task<string> SignPublicKeyAsync(
            ZoneLocator zone,
            string key,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read user's profile and published SSH keys.
        /// </summary>
        Task<LoginProfile> GetLoginProfileAsync(
           ProjectLocator project,
           CancellationToken token);

        /// <summary>
        /// Delete existing authorized key.
        /// </summary>
        Task DeleteSshPublicKeyAsync(
            string fingerprint,
            CancellationToken cancellationToken);

        /// <summary>
        /// List enrolled U2F and WebAuthn security keys.
        /// </summary>
        Task<IList<SecurityKey>> ListSecurityKeysAsync(
            ProjectLocator project,
            CancellationToken cancellationToken);
    }

    public class OsLoginClient : ApiClientBase, IOsLoginClient
    {
        private readonly IAuthorization authorization;
        private readonly CloudOSLoginService service;

        public OsLoginClient(
            ServiceEndpoint<OsLoginClient> endpoint,
            IAuthorization authorization,
            ApiKey apiKey,
            UserAgent userAgent)
            : base(endpoint, authorization, userAgent)
        {
            if (authorization.Session is IWorkforcePoolSession)
            {
                //
                // When authenticating using workforce identity, we have
                // to pass an API key to charge against.
                //
                this.Initializer.ApiKey = apiKey.Value;
            }

            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.service = new CloudOSLoginService(this.Initializer);
        }

        public static ServiceEndpoint<OsLoginClient> CreateEndpoint(
            ServiceRoute? route = null)
        {
            return new ServiceEndpoint<OsLoginClient>(
                route ?? ServiceRoute.Public,
                "https://oslogin.googleapis.com/");
        }

        internal string EncodedUserPathComponent
        {
            get => this.authorization.Session switch
            {
                //
                // Use the email address without extra encoding.
                //
                IGaiaOidcSession gaiaSession
                    => gaiaSession.Email,

                //
                // Use the full principal idenfifier (yes, that's a URL)
                // and encode it.
                //
                IWorkforcePoolSession wfSession
                    => WebUtility.UrlEncode(wfSession.PrincipalIdentifier),

                _ => throw new ArgumentOutOfRangeException()
            };
        }

        //---------------------------------------------------------------------
        // IOsLoginClient.
        //---------------------------------------------------------------------

        public async Task<LoginProfile> ImportSshPublicKeyAsync(
            ProjectLocator project,
            string key,
            TimeSpan validity,
            CancellationToken token)
        {
            project.ExpectNotNull(nameof(project));
            key.ExpectNotEmpty(nameof(key));

            Debug.Assert(key.Contains(' '));

            if (this.authorization.Session is IWorkforcePoolSession)
            {
                throw new OsLoginNotSupportedForWorkloadIdentityException();
            }

            using (ApiTraceSource.Log.TraceMethod().WithParameters(project))
            {
                var expiryTimeUsec = new DateTimeOffset(DateTime.UtcNow.Add(validity))
                    .ToUnixTimeMilliseconds() * 1000;

                var request = this.service.Users.ImportSshPublicKey(
                    new SshPublicKey()
                    {
                        Key = key,
                        ExpirationTimeUsec = expiryTimeUsec
                    },
                    $"users/{this.EncodedUserPathComponent}");
                request.ProjectId = project.ProjectId;

                try
                {
                    var response = await request
                        .ExecuteAsync(token)
                        .ConfigureAwait(false);

                    //
                    // Creating the profile succeeded (if it didn't exist
                    // yet -- but we still need to check if the key was actually
                    // added.
                    //
                    // If the 'Allow users to manage their SSH public keys
                    // via the OS Login API' policy is disabled (in Cloud Identity),
                    // then adding the key won't work.
                    //
                    if (response.LoginProfile.SshPublicKeys
                        .EnsureNotNull()
                        .Any(kvp => kvp.Value.Key.Contains(key)))
                    {
                        return response.LoginProfile;
                    }
                    else
                    {
                        //
                        // Key wasn't added.
                        //
                        throw new ResourceAccessDeniedException(
                            "You do not have sufficient permissions to publish an SSH " +
                            "key to OS Login",
                            HelpTopics.ManagingOsLogin,
                            new GoogleApiException("oslogin", response.Details ?? string.Empty));
                    }
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    //
                    // Likely reason: The user account is a consumer account or
                    // an administrator has disabled POSIX account/SSH key information
                    // updates in the Admin Console.
                    //
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to use OS Login: " +
                        e.Error?.Message ?? "access denied",
                        HelpTopics.ManagingOsLogin,
                        e);
                }
            }
        }

        public async Task<LoginProfile> GetLoginProfileAsync(
            ProjectLocator project,
            CancellationToken token)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(project))
            {
                if (this.authorization.Session is IWorkforcePoolSession)
                {
                    throw new OsLoginNotSupportedForWorkloadIdentityException();
                }

                var request = this.service.Users.GetLoginProfile(
                    $"users/{this.EncodedUserPathComponent}");
                request.ProjectId = project.ProjectId;

                try
                {
                    return await request
                        .ExecuteAsync(token)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to use OS Login: " +
                        e.Error?.Message ?? "access denied",
                        HelpTopics.ManagingOsLogin,
                        e);
                }
                catch (GoogleApiException e) when (e.IsNotFound())
                {
                    throw new ResourceNotFoundException(
                        "The login profile could not be found, it it has not " +
                        "been allocated yet",
                        e);
                }
            }
        }

        public async Task DeleteSshPublicKeyAsync(
            string fingerprint,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(fingerprint))
            {
                if (this.authorization.Session is IWorkforcePoolSession)
                {
                    throw new OsLoginNotSupportedForWorkloadIdentityException();
                }

                try
                {
                    await this.service.Users.SshPublicKeys
                        .Delete($"users/{this.EncodedUserPathComponent}/sshPublicKeys/{fingerprint}")
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to use OS Login: " +
                        e.Error?.Message ?? "access denied",
                        HelpTopics.ManagingOsLogin,
                        e);
                }
            }
        }

        public async Task<string> SignPublicKeyAsync(
            ZoneLocator zone,
            string key,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(zone))
            {
                try
                {
                    var request = new BetaSignSshPublicKeyRequest(
                        this.service,
                        new BetaSignSshPublicKeyRequestData()
                        {
                            SshPublicKey = key
                        },
                        $"users/{this.EncodedUserPathComponent}/projects/{zone.ProjectId}/locations/{zone.Name}");

                    if (this.authorization.Session is IWorkforcePoolSession)
                    {
                        //
                        // This is a non-resourceful API. Charging to a client
                        // project doesn't work with workforce identity, so we
                        // have to do one of the following:
                        //
                        // (1) Pass an API key that's from the same project as the
                        //     OAuth client.
                        //
                        // (2) Pass an API key from any project, and set the
                        //     quota project.
                        //
                        //     This requires the user to have the
                        //     serviceusage.services.use permission.
                        //
                        // Option (1) isn't viable currently, so we need to do (2).
                        //

                        request.UserProject = zone.ProjectId;
                    }

                    var response = await request
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);

                    Invariant.ExpectNotNull(
                        response.SignedSshPublicKey, 
                        "SignedSshPublicKey");

                    return response.SignedSshPublicKey!;
                }
                catch (GoogleApiException e) when (
                    e.Error != null &&
                    e.Error.Code == 400 &&
                    e.Error.Message != null &&
                    e.Error.Message.Contains("google.posix_username"))
                {
                    throw new ExternalIdpNotConfiguredForOsLoginException(
                        "Your workforce identity provider configuration doesn't contain " +
                        "an attribute mapping for 'google.posix_username'. This mapping is " +
                        "required for using OS Login.",
                        e);
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    if (e.Error?.Message is var message &&
                        message != null &&
                        message.Contains("roles/serviceusage.serviceUsageConsumer"))
                    {
                        throw new ResourceAccessDeniedException(
                            "You do not have sufficient access to log in.\n\n" +
                            "Because you've authenticated using workforce identity federation, " +
                            "you additionally need the 'Service Usage Consumer' role " +
                            "(or an equivalent custom role) to log in.",
                            HelpTopics.UseOsLoginWithWorkforceIdentity,
                            e);
                    }
                    else
                    {
                        throw new ResourceAccessDeniedException(
                            "You do not have sufficient access to log in: " +
                            e.Error?.Message ?? "access denied",
                            HelpTopics.ManagingOsLogin,
                            e);
                    }
                }
            }
        }

        public async Task<IList<SecurityKey>> ListSecurityKeysAsync(
            ProjectLocator project,
            CancellationToken cancellationToken)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(project))
            {

                if (this.authorization.Session is IWorkforcePoolSession)
                {
                    throw new OsLoginNotSupportedForWorkloadIdentityException();
                }

                try
                {
                    var request = new BetaGetLoginProfileRequest(
                        this.service,
                        $"users/{this.EncodedUserPathComponent}")
                    {
                        ProjectId = project.Name,
                        View = BetaGetLoginProfileRequest.ViewEnum.SECURITYKEY
                    };

                    var response = await request
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);

                    return response.SecurityKeys ?? new List<SecurityKey>();
                }
                catch (GoogleApiException e) when (e.IsAccessDenied())
                {
                    throw new ResourceAccessDeniedException(
                        "You do not have sufficient permissions to use OS Login: " +
                        e.Error?.Message ?? "access denied",
                        HelpTopics.ManagingOsLogin,
                        e);
                }
            }
        }

        //---------------------------------------------------------------------
        // v1beta1 entities. These can be removed once the methods have been
        // promoted to v1.
        //---------------------------------------------------------------------

        #region Request entities

        private class BetaSignSshPublicKeyResponseData : IDirectResponseSchema
        {
            [JsonProperty("signedSshPublicKey")]
            public virtual string? SignedSshPublicKey { get; set; }

            public virtual string? ETag { get; set; }
        }

        private class BetaSignSshPublicKeyRequestData : IDirectResponseSchema
        {
            [JsonProperty("sshPublicKey")]
            public virtual string? SshPublicKey { get; set; }

            public virtual string? ETag { get; set; }
        }

        private class BetaSignSshPublicKeyRequest
            : CloudOSLoginBaseServiceRequest<BetaSignSshPublicKeyResponseData>
        {
            [RequestParameter("parent")]
            public virtual string Parent { get; private set; }
            private BetaSignSshPublicKeyRequestData Body { get; set; }
            public override string MethodName => "signSshPublicKey";
            public override string HttpMethod => "POST";
            public override string RestPath => "v1beta/{+parent}:signSshPublicKey";

            [RequestParameter("$userProject")]
            public virtual string? UserProject { get; set; }

            public BetaSignSshPublicKeyRequest(
                IClientService service,
                BetaSignSshPublicKeyRequestData body,
                string parent)
                : base(service)
            {
                this.Parent = parent;
                this.Body = body;
                InitParameters();
            }

            protected override object GetBody()
            {
                return this.Body;
            }

            protected override void InitParameters()
            {
                base.InitParameters();
                this.RequestParameters.Add("parent", new Parameter
                {
                    Name = "parent",
                    IsRequired = true,
                    ParameterType = "path",
                    DefaultValue = null,
                    Pattern = "^users/[^/]+/projects/[^/]+/locations/[^/]+$"
                });
                this.RequestParameters.Add("$userProject", new Parameter
                {
                    Name = "$userProject",
                    IsRequired = false,
                    ParameterType = "query",
                    DefaultValue = null
                });
            }
        }

        private class BetaLoginProfile : IDirectResponseSchema
        {
            /// <summary>The registered security key credentials for a user.</summary>
            [JsonProperty("securityKeys")]
            public virtual IList<SecurityKey>? SecurityKeys { get; set; }

            public virtual string? ETag { get; set; }
        }

        public class SecurityKey : IDirectResponseSchema
        {
            [JsonProperty("deviceNickname")]
            public virtual string? DeviceNickname { get; set; }

            [JsonProperty("privateKey")]
            public virtual string? PrivateKey { get; set; }

            [JsonProperty("publicKey")]
            public virtual string? PublicKey { get; set; }

            [JsonProperty("universalTwoFactor")]
            public virtual UniversalTwoFactor? UniversalTwoFactor { get; set; }

            [JsonProperty("webAuthn")]
            public virtual WebAuthn? WebAuthn { get; set; }

            public virtual string? ETag { get; set; }
        }

        public class UniversalTwoFactor : IDirectResponseSchema
        {
            [JsonProperty("appId")]
            public virtual string? AppId { get; set; }

            public virtual string? ETag { get; set; }
        }

        public class WebAuthn : IDirectResponseSchema
        {
            [JsonProperty("rpId")]
            public virtual string? RpId { get; set; }

            public virtual string? ETag { get; set; }
        }

        private class BetaGetLoginProfileRequest : CloudOSLoginBaseServiceRequest<BetaLoginProfile>
        {
            public enum ViewEnum
            {
                [StringValue("SECURITY_KEY")]
                SECURITYKEY
            }

            [RequestParameter("name")]
            public virtual string Name { get; private set; }

            [RequestParameter("projectId")]
            public virtual string? ProjectId { get; set; }

            [RequestParameter("systemId")]
            public virtual string? SystemId { get; set; }

            [RequestParameter("view")]
            public virtual ViewEnum? View { get; set; }

            [RequestParameter("$userProject")]
            public virtual string? UserProject { get; set; }

            public override string MethodName => "getLoginProfile";

            public override string HttpMethod => "GET";

            public override string RestPath => "v1beta/{+name}/loginProfile";

            public BetaGetLoginProfileRequest(IClientService service, string name)
                : base(service)
            {
                this.Name = name;
                InitParameters();
            }

            protected override void InitParameters()
            {
                base.InitParameters();
                this.RequestParameters.Add("name", new Parameter
                {
                    Name = "name",
                    IsRequired = true,
                    ParameterType = "path",
                    DefaultValue = null,
                    Pattern = "^users/[^/]+$"
                });
                this.RequestParameters.Add("projectId", new Parameter
                {
                    Name = "projectId",
                    IsRequired = false,
                    ParameterType = "query",
                    DefaultValue = null,
                    Pattern = null
                });
                this.RequestParameters.Add("systemId", new Parameter
                {
                    Name = "systemId",
                    IsRequired = false,
                    ParameterType = "query",
                    DefaultValue = null,
                    Pattern = null
                });
                this.RequestParameters.Add("view", new Parameter
                {
                    Name = "view",
                    IsRequired = false,
                    ParameterType = "query",
                    DefaultValue = null,
                    Pattern = null
                });
                this.RequestParameters.Add("$userProject", new Parameter
                {
                    Name = "$userProject",
                    IsRequired = false,
                    ParameterType = "query",
                    DefaultValue = null
                });
            }
        }

        #endregion
    }

    internal class OsLoginNotSupportedForWorkloadIdentityException :
        NotSupportedForWorkloadIdentityException, IExceptionWithHelpTopic
    {
        public OsLoginNotSupportedForWorkloadIdentityException()
            : base(
                "This OS Login operation is not supported for " +
                "workforce identity federation.")
        {
        }
    }

    public class ExternalIdpNotConfiguredForOsLoginException :
        ClientException, IExceptionWithHelpTopic
    {
        public IHelpTopic? Help { get; }

        public ExternalIdpNotConfiguredForOsLoginException(
            string message,
            Exception inner)
            : base(message, inner)
        {
            this.Help = HelpTopics.UseOsLoginWithWorkforceIdentity;
        }
    }
}
