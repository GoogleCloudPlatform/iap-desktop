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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Adapter
{
    public interface IComputeEngineKeysAdapter : IDisposable
    {
        Task PushPublicKeyAsync(
            InstanceLocator instance,
            string username,
            ISshKey key,
            CancellationToken token);
    }

    [Service(typeof(IComputeEngineKeysAdapter))]
    public class ComputeEngineKeysAdapter : IComputeEngineKeysAdapter
    {
        private readonly IComputeEngineAdapter computeEngineAdapter;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public ComputeEngineKeysAdapter(IComputeEngineAdapter computeEngineAdapter)
        {
            this.computeEngineAdapter = computeEngineAdapter;
        }

        public ComputeEngineKeysAdapter(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IComputeEngineAdapter>())
        {
        }

        //---------------------------------------------------------------------
        // IComputeEngineKeysAdapter.
        //---------------------------------------------------------------------

        public async Task PushPublicKeyAsync(
            InstanceLocator instance,
            string username,
            ISshKey key,
            CancellationToken token)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(
                instance,
                username))
            {
                var rsaPublicKey = Convert.ToBase64String(key.PublicKey);

                // TODO: Keep existing keys instead of overriding.
                // TODO: Set expiry
                await this.computeEngineAdapter.AddMetadataAsync(
                        instance,
                        "ssh-keys",
                        $"{username}:ssh-rsa {rsaPublicKey} {username}",
                        token)
                    .ConfigureAwait(false);
            }
        }


        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.computeEngineAdapter.Dispose();
            }
        }

        internal void PushPublicKeyAsync(InstanceLocator instanceLocator, string v, RSACng key, object canellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
