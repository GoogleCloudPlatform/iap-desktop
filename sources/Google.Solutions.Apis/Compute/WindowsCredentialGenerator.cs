//
// Copyright 2019 Google LLC
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
using Google.Solutions.Apis.Diagnostics;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Compute
{
    /// <summary>
    /// Uses the OS Agent's account manager to generate Windows
    /// logon credentials.
    /// </summary>
    /// <see href="https://cloud.google.com/compute/docs/instances/windows/automate-pw-generation"/>
    public interface IWindowsCredentialGenerator
    {
        Task<bool> IsGrantedPermissionToCreateWindowsCredentialsAsync(
            InstanceLocator instanceRef);

        /// <summary>
        /// Reset a SAM account password. If the SAM account does not exist,
        /// it is created and made a local Administrator.
        /// </summary>
        Task<NetworkCredential> CreateWindowsCredentialsAsync(
            InstanceLocator instanceRef,
            string username,
            UserFlags tyerType,
            CancellationToken token);

        /// <summary>
        /// Reset a SAM account password. If the SAM account does not exist,
        /// it is created and made a local Administrator.
        /// </summary>
        Task<NetworkCredential> CreateWindowsCredentialsAsync(
            InstanceLocator instanceRef,
            string username,
            UserFlags tyerType,
            TimeSpan timeout,
            CancellationToken token);
    }

    [Flags]
    public enum UserFlags
    {
        /// <summary>
        /// Add to local Administrators group. This is the default
        /// behavior.
        /// </summary>
        AddToAdministrators = 1,

        /// <summary>
        /// Don't modify group memverships. This is only supported by
        /// newer OS agent versions (Jan 2020 and later).
        /// </summary>
        None = 0
    }

    public sealed class WindowsCredentialGenerator : IWindowsCredentialGenerator
    {
        private const int RsaKeySize = 2048;
        private const int SerialPort = 4;
        private const string MetadataKey = "windows-keys";

        //
        // Default settings for password encryption.
        //
        private const string DefaultHashFunction = "sha256";
        private static readonly RSAEncryptionPadding DefaultEncryptionPadding 
            = RSAEncryptionPadding.OaepSHA256;

        private readonly IComputeEngineClient computeClient;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public WindowsCredentialGenerator(
            IComputeEngineClient computeClient)
        {
            this.computeClient = computeClient;
        }

        //---------------------------------------------------------------------
        // IWindowsCredentialService.
        //---------------------------------------------------------------------

        public async Task<NetworkCredential> CreateWindowsCredentialsAsync(
            InstanceLocator instanceRef,
            string username,
            UserFlags userType,
            CancellationToken token)
        {
            using (ApiTraceSource.Log.TraceMethod().WithParameters(instanceRef, username))
            using (var rsa = new RSACng(RsaKeySize))
            {
                var keyParameters = rsa.ExportParameters(false);

                var requestPayload = new RequestPayload()
                {
                    ExpireOn = DateTime.UtcNow.AddMinutes(5),
                    Username = username,
                    Email = username,
                    Modulus = Convert.ToBase64String(keyParameters.Modulus),
                    Exponent = Convert.ToBase64String(keyParameters.Exponent),
                    AddToAdministrators = userType.HasFlag(UserFlags.AddToAdministrators),
                    HashFunction = DefaultHashFunction
                };

                //
                // Send the request to the instance via a special metadata entry.
                //
                try
                {
                    var requestJson = JsonConvert.SerializeObject(requestPayload);
                    await this.computeClient.UpdateMetadataAsync(
                            instanceRef,
                            existingMetadata =>
                            {
                                existingMetadata.Add(new Metadata()
                                {
                                    Items = new[]
                                    {
                                        new Metadata.ItemsData()
                                        {
                                            Key = MetadataKey,
                                            Value = requestJson
                                        }
                                    }
                                });
                            },
                            token)
                        .ConfigureAwait(false);
                }
                catch (ResourceNotFoundException e)
                {
                    ApiTraceSource.Log.TraceVerbose("Instance does not exist: {0}", e.Message);

                    throw new WindowsCredentialCreationFailedException(
                        $"Instance {instanceRef.Name} was not found.");
                }
                catch (ResourceAccessDeniedException e)
                {
                    ApiTraceSource.Log.TraceVerbose(
                        "Setting request payload metadata failed with 403: {0}",
                        e.FullMessage());

                    //
                    // Setting metadata failed due to lack of permissions. Note that
                    // the Error object is not always populated, hence the OR filter.
                    //

                    throw new WindowsCredentialCreationFailedException(
                        "You do not have sufficient permissions to reset a Windows password. " +
                        "You need the 'Service Account User' and " +
                        "'Compute Instance Admin' roles (or equivalent custom roles) " +
                        "to perform this action.",
                        HelpTopics.PermissionsToResetWindowsUser);
                }
                catch (GoogleApiException e) when (e.IsBadRequest())
                {
                    ApiTraceSource.Log.TraceVerbose(
                        "Setting request payload metadata failed with 400: {0} ({1})",
                        e.Message,
                        e.Error?.Errors.EnsureNotNull().Select(er => er.Reason).FirstOrDefault());

                    //
                    // This slightly weirdly encoded error happens if the user has the necessary
                    // permissions on the VM, but lacks ActAs permission on the associated 
                    // service account.
                    //

                    throw new WindowsCredentialCreationFailedException(
                        "You do not have sufficient permissions to reset a Windows password. " +
                        "Because this VM instance uses a service account, you also need the " +
                        "'Service Account User' role.",
                        HelpTopics.PermissionsToResetWindowsUser);
                }

                //
                // Read response from serial port.
                //
                using (var serialPortStream = this.computeClient.GetSerialPortOutput(
                    instanceRef,
                    SerialPort))
                {
                    //
                    // It is rare, but sometimes a single JSON can be split over multiple
                    // API reads. Therefore, maintain a buffer.
                    //

                    var logBuffer = new StringBuilder(64 * 1024);
                    while (true)
                    {
                        ApiTraceSource.Log.TraceVerbose("Waiting for agent to supply response...");

                        token.ThrowIfCancellationRequested();

                        var logDelta = await serialPortStream.ReadAsync(token).ConfigureAwait(false);
                        if (string.IsNullOrEmpty(logDelta))
                        {
                            // Reached end of stream, wait and try again.
                            await Task.Delay(500, token).ConfigureAwait(false);
                            continue;
                        }

                        logBuffer.Append(logDelta);

                        //
                        // NB. Old versions of the Windows guest agent wrongly added a '\'
                        // before every '/' in base64-encoded data. This affects the search
                        // for the modulus.
                        //

                        var response = logBuffer.ToString()
                            .Split('\n')
                            .Where(line => line.Contains(requestPayload.Modulus) ||
                                           line.Replace("\\/", "/").Contains(requestPayload.Modulus))
                            .FirstOrDefault();
                        if (response == null)
                        {
                            //
                            // That was not the output we are looking for, keep reading.
                            //
                            continue;
                        }

                        var responsePayload = JsonConvert.DeserializeObject<ResponsePayload>(response);
                        if (responsePayload == null ||
                            !string.IsNullOrEmpty(responsePayload.ErrorMessage))
                        {
                            throw new WindowsCredentialCreationFailedException(
                                responsePayload?.ErrorMessage ?? "The response is empty");
                        }

                        //
                        // Old versions of the Windows guest agent unconditionally used
                        // OaepSHA1. Current versions vary the padding based on the
                        // hash function passed in the request, and echo the hash function
                        // in the response.
                        //
                        // If the response contains the hash function we requested, then
                        // we know it's a current version and we can use the appropriate
                        // OAEP padding. If the response contains no hash function field,
                        // then we're dealing with an old version and need to fall back
                        // to OaepSHA1.
                        //

                        var padding = responsePayload.HashFunction == DefaultHashFunction
                            ? DefaultEncryptionPadding
                            : RSAEncryptionPadding.OaepSHA1;

                        var password = rsa.Decrypt(
                            Convert.FromBase64String(responsePayload.EncryptedPassword),
                            padding);

                        return new NetworkCredential(
                            username,
                            new UTF8Encoding().GetString(password),
                            null);
                    }
                }
            }
        }

        public async Task<NetworkCredential> CreateWindowsCredentialsAsync(
            InstanceLocator instanceRef,
            string username,
            UserFlags userType,
            TimeSpan timeout,
            CancellationToken token)
        {
            using (var timeoutCts = new CancellationTokenSource())
            {
                timeoutCts.CancelAfter(timeout);

                using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, token))
                {
                    try
                    {
                        return await CreateWindowsCredentialsAsync(
                            instanceRef,
                            username,
                            userType,
                            combinedCts.Token).ConfigureAwait(false);
                    }
                    catch (Exception e) when (e.IsCancellation() && timeoutCts.IsCancellationRequested)
                    {
                        ApiTraceSource.Log.TraceError(e);

                        //
                        // This task was cancelled because of a timeout, not because
                        // the enclosing job was cancelled.
                        //
                        throw new WindowsCredentialCreationFailedException(
                            $"Timeout waiting for Compute Engine agent to reset password for user {username}. " +
                            "Verify that the agent is running and that the account manager feature is enabled.");
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Permission check.
        //---------------------------------------------------------------------

        public Task<bool> IsGrantedPermissionToCreateWindowsCredentialsAsync(InstanceLocator instanceRef)
        {
            //
            // Resetting a user requires
            //  (1) compute.instances.setMetadata
            //  (2) iam.serviceAccounts.actAs (if the instance runs as service account)
            //
            // For performance reasons, only check (1).
            //
            return this.computeClient.IsGrantedPermission(
                instanceRef,
                Permissions.ComputeInstancesSetMetadata);
        }

        //---------------------------------------------------------------------
        // Data classes.
        //---------------------------------------------------------------------

        internal class RequestPayload
        {
            [JsonProperty("userName")]
            public string? Username { get; set; }

            [JsonProperty("email")]
            public string? Email { get; set; }

            [JsonProperty("expireOn")]
            public DateTime? ExpireOn { get; set; }

            [JsonProperty("modulus")]
            public string? Modulus { get; set; }

            [JsonProperty("exponent")]
            public string? Exponent { get; set; }

            [JsonProperty("addToAdministrators")]
            public bool? AddToAdministrators { get; set; }

            [JsonProperty("hashFunction")]
            public string? HashFunction { get; set; }
        }

        internal class ResponsePayload
        {
            [JsonProperty("encryptedPassword")]
            public string? EncryptedPassword { get; set; }

            [JsonProperty("errorMessage")]
            public string? ErrorMessage { get; set; }

            [JsonProperty("hashFunction")]
            public string? HashFunction { get; set; }
        }
    }

    public class WindowsCredentialCreationFailedException : Exception, IExceptionWithHelpTopic
    {
        public IHelpTopic? Help { get; }

        public WindowsCredentialCreationFailedException(string message) : base(message)
        {
        }

        public WindowsCredentialCreationFailedException(string message, IHelpTopic helpTopic)
            : base(message)
        {
            this.Help = helpTopic;
        }
    }
}
