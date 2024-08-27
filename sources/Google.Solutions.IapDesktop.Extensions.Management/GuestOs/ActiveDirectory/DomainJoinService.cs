//
// Copyright 2022 Google LLC
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

using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Text;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.GuestOs.ActiveDirectory
{
    public interface IDomainJoinService
    {
        /// <summary>
        /// Join VM to an Active Directory domain.
        /// </summary>
        Task JoinDomainAsync(
            InstanceLocator instance,
            string domain,
            string? newComputerName,
            NetworkCredential domainCredential,
            CancellationToken cancellationToken);
    }

    [Service(typeof(IDomainJoinService))]
    public sealed class DomainJoinService : IDomainJoinService
    {
        private readonly Service<IComputeEngineClient> computeClient;
        private const int SerialPort = 4;

        public DomainJoinService(Service<IComputeEngineClient> computeClient)
        {
            this.computeClient = computeClient.ExpectNotNull(nameof(computeClient));
        }

        //---------------------------------------------------------------------
        // Helper methods. Internal to facilitate testing.
        //---------------------------------------------------------------------

        internal static string CreateStartupScript(Guid operationId)
        {
            var assembly = typeof(DomainJoinService).Assembly;
            var resourceName = assembly
                .GetManifestResourceNames()
                .First(s => s.EndsWith($"{typeof(DomainJoinService).Name}.StartupScript.ps1"));
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader
                    .ReadToEnd()
                    .Replace("{{OPERATION_ID}}", operationId.ToString());
            }
        }

        internal async Task<TMessage> AwaitMessageAsync<TMessage>(
            IStartupScriptOperation operation,
            string messageType,
            CancellationToken cancellationToken)
            where TMessage : MessageBase
        {
            using (ApplicationTraceSource.Log.TraceMethod()
                .WithParameters(messageType))
            using (var serialPortStream = operation
                .ComputeClient
                .GetSerialPortOutput(operation.Instance, SerialPort))
            {
                //
                // It is rare, but sometimes a single JSON can be split over multiple
                // API reads. Therefore, maintain a buffer.
                //
                var logBuffer = new StringBuilder(64 * 1024);
                while (true)
                {
                    ApplicationTraceSource.Log.TraceVerbose("Waiting for VM response...");

                    cancellationToken.ThrowIfCancellationRequested();

                    var logDelta = await serialPortStream
                        .ReadAsync(cancellationToken)
                        .ConfigureAwait(false);
                    if (string.IsNullOrEmpty(logDelta))
                    {
                        // Reached end of stream, wait and try again.
                        await Task
                            .Delay(500, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }

                    logBuffer.Append(logDelta);

                    var match = logBuffer.ToString()
                        .Split('\n')
                        .Where(line =>
                               line.Contains(operation.OperationId.ToString()) &&
                               line.Contains(messageType))
                        .FirstOrDefault();
                    if (match != null &&
                        JsonConvert.DeserializeObject<TMessage>(match) is TMessage message &&
                        message.MessageType == messageType)
                    {
                        return message;
                    }
                }
            }
        }

        internal async Task JoinDomainAsync(
            IStartupScriptOperation operation,
            string domain,
            string? newComputerName,
            NetworkCredential domainCredential,
            CancellationToken cancellationToken)
        {
            domain.ExpectNotEmpty(nameof(domain));
            domainCredential.UserName.ExpectNotNull("username");
            domainCredential.Password.ExpectNotNull("password");

            //
            // Swap existing startup scripts against the
            // domain-join script.
            //
            // NB. If the user has permissions to change
            // metadata, then they're very likely to also have
            // sufficient permissions to restart the instance.
            //
            await operation.ReplaceStartupScriptAsync(
                    CreateStartupScript(operation.OperationId),
                    cancellationToken)
                .ConfigureAwait(false);

            try
            {
                //
                // Reset the VM to trigger the domain-join script.
                //
                await operation.ComputeClient.ControlInstanceAsync(
                        operation.Instance,
                        InstanceControlCommand.Reset,
                        cancellationToken)
                    .ConfigureAwait(false);

                //
                // Wait for VM to publish its public key.
                //
                var hello = await AwaitMessageAsync<HelloMessage>(
                        operation,
                        HelloMessage.MessageTypeString,
                        cancellationToken)
                    .ConfigureAwait(false);

                //
                // Write a join request to metadata. To protect the
                // domain user's credentials, encrypt the password
                // using the (ephemeral) key we received in the "Hello"
                // message.
                //
                using (var key = new RSACng())
                {
                    key.ImportParameters(new RSAParameters()
                    {
                        Exponent = Convert.FromBase64String(hello.Exponent),
                        Modulus = Convert.FromBase64String(hello.Modulus)
                    });

                    var encryptedPassword = Convert.ToBase64String(
                        key.Encrypt(
                            Encoding.UTF8.GetBytes(domainCredential.Password),
                            RSAEncryptionPadding.OaepSHA256));

                    var joinRequest = new JoinRequest()
                    {
                        OperationId = operation.OperationId.ToString(),
                        MessageType = JoinRequest.MessageTypeString,
                        DomainName = domain,
                        NewComputerName = newComputerName.NullIfEmpty(),
                        Username = domainCredential.Normalize().UserName,
                        EncryptedPassword = encryptedPassword
                    };

                    await operation.SetMetadataAsync(
                            MetadataKeys.JoinDomain,
                            JsonConvert.SerializeObject(joinRequest),
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                //
                // Wait for VM to complete the join.
                //
                var joinResponse = await AwaitMessageAsync<JoinResponse>(
                        operation,
                        JoinResponse.MessageTypeString,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (joinResponse.Succeeded != true)
                {
                    throw new DomainJoinFailedException(
                        $"The domain join failed: {joinResponse.ErrorDetails}");
                }
            }
            finally
            {
                //
                // Restore the previous startup scripts and remove the
                // keys we added.
                //
                await operation
                    .RestoreStartupScriptsAsync(CancellationToken.None) // Run even if cancelled
                    .ConfigureAwait(false);
            }
        }

        //---------------------------------------------------------------------
        // IAdJoinService.
        //---------------------------------------------------------------------

        public async Task JoinDomainAsync(
            InstanceLocator instance,
            string domain,
            string? newComputerName,
            NetworkCredential domainCredential,
            CancellationToken cancellationToken)
        {

            using (ApplicationTraceSource.Log.TraceMethod()
                .WithParameters(instance, domain, newComputerName))
            {
                using (var operation = new StartupScriptOperation(
                    instance,
                    MetadataKeys.JoinDomainGuard,
                    this.computeClient.Activate()))
                {
                    await JoinDomainAsync(
                            operation,
                            domain,
                            newComputerName,
                            domainCredential,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        //---------------------------------------------------------------------
        // Messages.
        //---------------------------------------------------------------------

        internal abstract class MessageBase
        {
            /// <summary>
            /// Unique identifier for the join operation.
            /// </summary>
            public string? OperationId { get; set; }

            /// <summary>
            /// Identifier for the message.
            /// </summary>
            public string? MessageType { get; set; }
        }

        /// <summary>
        /// VM -> Client: Indicates that the VM is awaiting
        /// a join request.
        /// </summary>
        internal class HelloMessage : MessageBase
        {
            internal const string MessageTypeString = "hello";

            public string? Modulus { get; set; }
            public string? Exponent { get; set; }
        }

        /// <summary>
        /// Client -> VM: Initiate a join request.
        /// </summary>
        internal class JoinRequest : MessageBase
        {
            internal const string MessageTypeString = "join-request";

            public string? DomainName { get; set; }
            public string? Username { get; set; }
            public string? EncryptedPassword { get; set; }
            public string? NewComputerName { get; set; }
        }

        /// <summary>
        /// VM -> Client: Result of a join request.
        /// </summary>
        internal class JoinResponse : MessageBase
        {
            internal const string MessageTypeString = "join-response";

            public bool? Succeeded { get; set; }
            public string? ErrorDetails { get; set; }
        }

        internal static class MetadataKeys
        {
            /// <summary>
            /// Key used for join-domain message,
            /// </summary>
            public const string JoinDomain = "iapdesktop-join";

            /// <summary>
            /// Guard value used to inidicate that a domin is in progress.
            /// </summary>
            public const string JoinDomainGuard = "iapdesktop-join-in-progress";
        }
    }

    public class DomainJoinFailedException : Exception
    {
        public DomainJoinFailedException(string message) : base(message)
        {
        }
    }
}
