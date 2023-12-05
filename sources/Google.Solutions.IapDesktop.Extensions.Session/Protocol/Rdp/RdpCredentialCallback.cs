//
// Copyright 2023 Google LLC
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

using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp
{
    public interface IRdpCredentialCallback
    {
        Task<RdpCredential> GetCredentialsAsync(
            Uri callbackUrl,
            CancellationToken cancellationToken);
    }

    [Service(typeof(IRdpCredentialCallback))]
    public class RdpCredentialCallback : IRdpCredentialCallback
    {
        private readonly IExternalRestAdapter restAdapter;

        public RdpCredentialCallback(IExternalRestAdapter restAdapter)
        {
            this.restAdapter = restAdapter;
        }

        public async Task<RdpCredential> GetCredentialsAsync(
            Uri callbackUrl,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.restAdapter
                    .GetAsync<CredentialCallbackResponse>(callbackUrl, cancellationToken)
                    .ConfigureAwait(false);

                if (response != null)
                {
                    return new RdpCredential(
                        response.User,
                        response.Domain,
                        SecureStringExtensions.FromClearText(response.Password));
                }
                else
                {
                    return RdpCredential.Empty;
                }
            }
            catch (HttpRequestException e)
            {
                throw new CredentialCallbackException(
                    $"Invoking the credential callback endpoint at {callbackUrl} failed " +
                    $"and no credentials were obtained",
                    e);
            }
            catch (JsonException e)
            {
                throw new CredentialCallbackException(
                    $"The credential callback endpoint at {callbackUrl} returned " +
                    $"an invalid result",
                    e);
            }
            catch (Exception e)
            {
                throw new CredentialCallbackException(
                    $"Obtaining credentials from the callback endpoint at {callbackUrl} failed", e);
            }
        }

        //---------------------------------------------------------------------
        // Response entity. 
        //---------------------------------------------------------------------

        public class CredentialCallbackResponse
        {
            [JsonProperty("User")]
            public string User { get; set; }

            [JsonProperty("Domain")]
            public string Domain { get; set; }

            [JsonProperty("Password")]
            public string Password { get; set; }
        }
    }

    public class CredentialCallbackException : Exception
    {
        public CredentialCallbackException(
            string message,
            Exception innerException) : base(message, innerException)
        {
        }
    }
}
