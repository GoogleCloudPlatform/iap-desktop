﻿//
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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection
{
    public interface IRdpCredentialCallbackService
    {
        Task<RdpCredentials> GetCredentialsAsync(
            Uri callbackUrl,
            CancellationToken cancellationToken);
    }

    [Service(typeof(IRdpCredentialCallbackService))]
    public class RdpCredentialCallbackService : IRdpCredentialCallbackService
    {
        private readonly IExternalRestAdapter restAdapter;

        public RdpCredentialCallbackService(IExternalRestAdapter restAdapter)
        {
            this.restAdapter = restAdapter;
        }

        public async Task<RdpCredentials> GetCredentialsAsync( // TODO: Test
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
                    return new RdpCredentials(
                        response.User,
                        response.Domain,
                        SecureStringExtensions.FromClearText(response.Password));
                }
                else
                {
                    return RdpCredentials.Empty;
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
