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
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Extensions
{
    /// <summary>
    /// Extend 'InstancesResource' by a 'ResetWindowsUserAsync' method.
    /// </summary>
    public static class ResetWindowsUserExtensions
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
            string username)
        {
            return ResetWindowsUserAsync(
                resource,
                new VmInstanceReference(project, zone, instance),
                username);
        }

        /// <summary>
        /// Reset a SAM account password. If the SAM account does not exist,
        /// it is created and made a local Administrator.
        /// </summary>
        /// <see href="https://cloud.google.com/compute/docs/instances/windows/automate-pw-generation"/>
        public static async Task<NetworkCredential> ResetWindowsUserAsync(
            this InstancesResource resource,
            VmInstanceReference instanceRef,
            string username)
        {
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
                await resource.AddMetadataAsync(
                    instanceRef,
                    MetadataKey,
                    JsonConvert.SerializeObject(requestPayload)).ConfigureAwait(false);

                // Read response from serial port.
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    string buffer = await resource.GetSerialPortOutputStream(
                        instanceRef, 
                        SerialPort).ReadAsync().ConfigureAwait(false);
                    var response = buffer.Split('\n')
                        .Where(line => line.Contains(requestPayload.Modulus))
                        .FirstOrDefault();
                    if (response == null)
                    {
                        await Task.Delay(500).ConfigureAwait(false);
                    }
                    else
                    {
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

                throw new TimeoutException("Timeout waiting for password reset");
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
    internal class PasswordResetException : Exception
    {
        public PasswordResetException(string message) : base(message)
        {
        }
    }
}
