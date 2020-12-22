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

using Google.Apis.Compute.v1;
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1032 // Implement standard exception constructors

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    /// <summary>
    /// Extend 'InstancesResource' by a 'ResetWindowsUserAsync' method.
    /// </summary>
    internal static class ResetWindowsUserExtensions
    {
        private const int RsaKeySize = 2048;
        private const int SerialPort = 4;
        private const string MetadataKey = "windows-keys";

        /// <summary>
        /// Reset a SAM account password. If the SAM account does not exist,
        /// it is created and made a local Administrator.
        /// </summary>
        /// <see href="https://cloud.google.com/compute/docs/instances/windows/automate-pw-generation"/>
        public static Task<NetworkCredential> ResetWindowsUserAsync(
            this InstancesResource resource,
            string project,
            string zone,
            string instance,
            string username,
            CancellationToken token)
        {
            return ResetWindowsUserAsync(
                resource,
                new InstanceLocator(project, zone, instance),
                username,
                token);
        }

        /// <summary>
        /// Reset a SAM account password. If the SAM account does not exist,
        /// it is created and made a local Administrator.
        /// </summary>
        /// <see href="https://cloud.google.com/compute/docs/instances/windows/automate-pw-generation"/>
        public static async Task<NetworkCredential> ResetWindowsUserAsync(
            this InstancesResource resource,
            InstanceLocator instanceRef,
            string username,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(instanceRef, username))
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(RsaKeySize))
            {
                var keyParameters = rsa.ExportParameters(false);

                var requestPayload = new RequestPayload()
                {
                    ExpireOn = DateTime.UtcNow.AddMinutes(5),
                    Username = username,
                    Email = username,
                    Modulus = Convert.ToBase64String(keyParameters.Modulus),
                    Exponent = Convert.ToBase64String(keyParameters.Exponent),
                };

                // Send the request to the instance via a special metadata entry.
                try
                {
                    var requestJson = JsonConvert.SerializeObject(requestPayload);
                    await resource.AddMetadataAsync(
                        instanceRef,
                        MetadataKey,
                        requestJson,
                        token).ConfigureAwait(false);
                }
                catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 404)
                {
                    ApplicationTraceSources.Default.TraceVerbose("Instance does not exist: {0}", e.Message);

                    throw new PasswordResetException(
                        $"Instance {instanceRef.Name} was not found.");
                }
                catch (GoogleApiException e) when (e.Error == null || e.Error.Code == 403)
                {
                    ApplicationTraceSources.Default.TraceVerbose(
                        "Setting request payload metadata failed with 403: {0} ({1})",
                        e.Message,
                        e.Error?.Errors.EnsureNotNull().Select(er => er.Reason).FirstOrDefault());

                    // Setting metadata failed due to lack of permissions. Note that
                    // the Error object is not always populated, hence the OR filter.

                    throw new PasswordResetException(
                        "You do not have sufficient permissions to reset a Windows password. " +
                        "You need the 'Service Account User' and " +
                        "'Compute Instance Admin' roles (or equivalent custom roles) " +
                        "to perform this action.");
                }
                catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 400 && e.Error.Message == "BAD REQUEST")
                {
                    ApplicationTraceSources.Default.TraceVerbose(
                        "Setting request payload metadata failed with 400: {0} ({1})",
                        e.Message,
                        e.Error?.Errors.EnsureNotNull().Select(er => er.Reason).FirstOrDefault());

                    // This slightly weirdly encoded error happens if the user has the necessary
                    // permissions on the VM, but lacks ActAs permission on the associated 
                    // service account.

                    throw new PasswordResetException(
                        "You do not have sufficient permissions to reset a Windows password. " +
                        "Because this VM instance uses a service account, you also need the " +
                        "'Service Account User' role.");
                }

                // Read response from serial port.
                using (var serialPortStream = resource.GetSerialPortOutputStream(
                    instanceRef,
                    SerialPort))
                {
                    // It is rare, but sometimes a single JSON can be split over multiple
                    // API reads. Therefore, maintain a buffer.
                    var logBuffer = new StringBuilder(64 * 1024);
                    while (true)
                    {
                        ApplicationTraceSources.Default.TraceVerbose("Waiting for agent to supply response...");

                        token.ThrowIfCancellationRequested();

                        string logDelta = await serialPortStream.ReadAsync(token).ConfigureAwait(false);
                        if (string.IsNullOrEmpty(logDelta))
                        {
                            // Reached end of stream, wait and try again.
                            await Task.Delay(500, token).ConfigureAwait(false);
                            continue;
                        }

                        logBuffer.Append(logDelta);

                        // NB. Old versions of the Windows guest agent wrongly added a '\'
                        // before every '/' in base64-encoded data. This affects the search
                        // for the modulus.
                        var response = logBuffer.ToString()
                            .Split('\n')
                            .Where(line => line.Contains(requestPayload.Modulus) ||
                                           line.Replace("\\/", "/").Contains(requestPayload.Modulus))
                            .FirstOrDefault();
                        if (response == null)
                        {
                            // That was not the output we are looking for, keep reading.
                            continue;
                        }

                        var responsePayload = JsonConvert.DeserializeObject<ResponsePayload>(response);
                        if (!string.IsNullOrEmpty(responsePayload.ErrorMessage))
                        {
                            throw new PasswordResetException(responsePayload.ErrorMessage);
                        }

                        var password = rsa.Decrypt(
                            Convert.FromBase64String(responsePayload.EncryptedPassword),
                            true);

                        return new NetworkCredential(
                            username,
                            new UTF8Encoding().GetString(password),
                            null);
                    }
                }
            }
        }

        public static async Task<NetworkCredential> ResetWindowsUserAsync(
            this InstancesResource resource,
            InstanceLocator instanceRef,
            string username,
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
                        return await ResetWindowsUserAsync(
                            resource,
                            instanceRef,
                            username,
                            combinedCts.Token).ConfigureAwait(false);
                    }
                    catch (Exception e) when (e.IsCancellation() && timeoutCts.IsCancellationRequested)
                    {
                        ApplicationTraceSources.Default.TraceError(e);
                        // This task was cancelled because of a timeout, not because
                        // the enclosing job was cancelled.
                        throw new PasswordResetException(
                            $"Timeout waiting for Compute Engine agent to reset password for user {username}. " +
                            "Verify that the agent is running.");
                    }
                }
            }
        }

        internal class RequestPayload
        {
            [JsonProperty("userName")]
            public string Username { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("expireOn")]
            public DateTime ExpireOn { get; set; }

            [JsonProperty("modulus")]
            public string Modulus { get; set; }

            [JsonProperty("exponent")]
            public string Exponent { get; set; }
        }

        internal class ResponsePayload
        {
            [JsonProperty("encryptedPassword")]
            public string EncryptedPassword { get; set; }

            [JsonProperty("errorMessage")]
            public string ErrorMessage { get; set; }
        }
    }

    [Serializable]
    public class PasswordResetException : Exception
    {
        public PasswordResetException(string message) : base(message)
        {
        }
    }
}
