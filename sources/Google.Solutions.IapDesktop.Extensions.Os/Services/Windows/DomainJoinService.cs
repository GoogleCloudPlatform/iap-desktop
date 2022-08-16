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

using Google.Apis.Compute.v1.Data;
using Google.Apis.Util;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Os.Services.Windows
{
    public interface IDomainJoinService : IDisposable
    {
        /// <summary>
        /// Join VM to an Active Directory domain.
        /// </summary>
        Task JoinDomainAsync(
            InstanceLocator instance,
            string domain,
            string newComputerName,
            NetworkCredential domainCredential,
            CancellationToken cancellationToken);
    }

    [Service(typeof(IDomainJoinService))]
    public sealed class DomainJoinService : IDomainJoinService
    {
        private const int SerialPort = 4;

        private readonly IComputeEngineAdapter computeEngineAdapter;

        public DomainJoinService(IComputeEngineAdapter computeEngineAdapter)
        {
            this.computeEngineAdapter = computeEngineAdapter;
        }

        public DomainJoinService(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IComputeEngineAdapter>())
        {
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

        internal async Task<List<Metadata.ItemsData>> ReplaceMetadataItemsAsync(
            InstanceLocator instance,
            string guardKey,
            ICollection<string> keysToReplace,
            List<Metadata.ItemsData> newItems,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod()
                .WithParameters(string.Join(", ", keysToReplace)))
            {
                List<Metadata.ItemsData> oldItems = null;
                await this.computeEngineAdapter.UpdateMetadataAsync(
                        instance,
                        metadata =>
                        {
                            Debug.Assert(metadata != null);

                            if (guardKey != null)
                            {
                                //
                                // Fail if the guard key exists.
                                //
                                if (metadata.Items
                                    .EnsureNotNull()
                                    .Any(i => i.Key == guardKey))
                                {
                                    throw new InvalidOperationException(
                                        $"Found metadata key '{guardKey}', indicating that a " +
                                        $"domain-join operation is already in progress");
                                }
                            }

                            //
                            // Read and remove existing items.
                            //
                            oldItems = metadata.Items
                                .EnsureNotNull()
                                .Where(i => keysToReplace.Contains(i.Key))
                                .ToList();

                            foreach (var item in oldItems)
                            {
                                metadata.Items.Remove(item);
                            }

                            if (metadata.Items == null)
                            {
                                metadata.Items = new List<Metadata.ItemsData>();
                            }

                            //
                            // Add new items.
                            //
                            foreach (var item in newItems)
                            {
                                metadata.Items.Add(item);
                            }
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                Debug.Assert(oldItems != null);
                return oldItems;
            }
        }

        internal async Task<string> AwaitMessageAsync(
            InstanceLocator instance,
            Guid operationId,
            string messageType,
            CancellationToken cancellationToken)
        {
            using (ApplicationTraceSources.Default.TraceMethod()
                .WithParameters(messageType))
            using (var serialPortStream = this.computeEngineAdapter
                .GetSerialPortOutput(instance, SerialPort))
            {
                //
                // It is rare, but sometimes a single JSON can be split over multiple
                // API reads. Therefore, maintain a buffer.
                //
                var logBuffer = new StringBuilder(64 * 1024);
                while (true)
                {
                    ApplicationTraceSources.Default.TraceVerbose("Waiting for VM response...");

                    cancellationToken.ThrowIfCancellationRequested();

                    string logDelta = await serialPortStream
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
                                line.Contains(operationId.ToString()) &&
                                line.Contains(messageType))
                        .FirstOrDefault();
                    if (match != null)
                    {
                        return match;
                    }
                }
            }
        }

        internal async Task<TMessage> AwaitMessageAsync<TMessage>(
            InstanceLocator instance,
            Guid operationId,
            string messageType,
            CancellationToken cancellationToken)
            where TMessage : MessageBase
        {
            var json = await AwaitMessageAsync(
                    instance,
                    operationId,
                    messageType,
                    cancellationToken)
                .ConfigureAwait(false);

            var message = JsonConvert.DeserializeObject<TMessage>(json);
            Debug.Assert(message.MessageType == messageType);

            return message;
        }

        internal async Task JoinDomainAsync(
            InstanceLocator instance,
            string domain,
            string newComputerName,
            NetworkCredential domainCredential,
            Guid operationId,
            CancellationToken cancellationToken)
        {
            domain.ThrowIfNullOrEmpty(nameof(domain));
            domainCredential?.UserName.ThrowIfNull("username");
            domainCredential?.Password.ThrowIfNull("password");

            using (ApplicationTraceSources.Default.TraceMethod()
                .WithParameters(instance, domain, operationId))
            {
                //
                // Swap existing startup scripts against the
                // domain-join script.
                //
                // NB. If the user has permissions to change
                // metadata, then they're very likely to also have
                // sufficient permissions to restart the instance.
                //
                var existingStartupScripts = await ReplaceMetadataItemsAsync(
                        instance,
                        MetadataKeys.JoinDomainGuard,
                        MetadataKeys.WindowsStartupScripts,
                        new List<Metadata.ItemsData>
                        {
                            new Metadata.ItemsData()
                            {
                                Key = MetadataKeys.WindowsStartupScriptPs1,
                                Value = CreateStartupScript(operationId)
                            },
                            new Metadata.ItemsData()
                            {
                                Key = MetadataKeys.JoinDomainGuard,
                                Value = operationId.ToString()
                            },
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                //
                // Reset the VM to trigger the domain-join script.
                //
                await this.computeEngineAdapter.ControlInstanceAsync(
                        instance,
                        InstanceControlCommand.Reset,
                        cancellationToken)
                    .ConfigureAwait(false);

                //
                // Wait for VM to publish its public key.
                //
                var hello = await AwaitMessageAsync<HelloMessage>(
                        instance,
                        operationId,
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
                            RSAEncryptionPadding.Pkcs1));

                    var joinRequest = new JoinRequest()
                    {
                        OperationId = operationId.ToString(),
                        MessageType = JoinRequest.MessageTypeString,
                        DomainName = domain,
                        NewComputerName = newComputerName,
                        Username = domainCredential.UserName, // TODO: Normalize UPN/NetBios format
                        EncryptedPassword = encryptedPassword
                    };

                    await ReplaceMetadataItemsAsync(
                            instance,
                            null,
                            new[] { MetadataKeys.JoinDomain },
                            new List<Metadata.ItemsData>
                            {
                                new Metadata.ItemsData()
                                {
                                    Key = MetadataKeys.JoinDomain,
                                    Value = JsonConvert.SerializeObject(joinRequest)
                                }
                            },
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                var joinResponse = await AwaitMessageAsync<JoinResponse>(
                        instance,
                        operationId,
                        JoinResponse.MessageTypeString,
                        cancellationToken)
                    .ConfigureAwait(false);

                //
                // The domain-join script will now do the join and restart
                // the computer.
                //
                // Restore the previous startup scripts and remove the
                // keys we added.
                //
                await ReplaceMetadataItemsAsync(
                        instance,
                        null,
                        MetadataKeys.WindowsStartupScripts
                            .Union(new[]
                            {
                                MetadataKeys.JoinDomain,
                                MetadataKeys.JoinDomainGuard
                            })
                            .ToList(),
                        existingStartupScripts,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (!joinResponse.Succeeded)
                {
                    throw new DomainJoinFailedException(
                        $"The domain join failed: {joinResponse.ErrorDetails}");
                }
            }
        }

        //---------------------------------------------------------------------
        // IAdJoinService.
        //---------------------------------------------------------------------

        public Task JoinDomainAsync(
            InstanceLocator instance,
            string domain, 
            string newComputerName,
            NetworkCredential domainCredential,
            CancellationToken cancellationToken)
            => JoinDomainAsync(
                instance,
                domain,
                newComputerName,
                domainCredential,
                Guid.NewGuid(),
                cancellationToken);


        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.computeEngineAdapter.Dispose();
        }

        //---------------------------------------------------------------------
        // Messages.
        //---------------------------------------------------------------------

        internal abstract class MessageBase
        {
            /// <summary>
            /// Unique identifier for the join operation.
            /// </summary>
            public string OperationId { get; set; }

            /// <summary>
            /// Identifier for the message.
            /// </summary>
            public string MessageType { get; set; }
        }

        /// <summary>
        /// VM -> Client: Indicates that the VM is awaiting
        /// a join request.
        /// </summary>
        internal class HelloMessage : MessageBase
        {
            internal const string MessageTypeString = "hello";

            public string Modulus { get; set; }
            public string Exponent { get; set; }
        }

        /// <summary>
        /// Client -> VM: Initiate a join request.
        /// </summary>
        internal class JoinRequest : MessageBase
        {
            internal const string MessageTypeString = "join-request";

            public string DomainName { get; set; }
            public string Username { get; set; }
            public string EncryptedPassword { get; set; }
            public string NewComputerName { get; set; }
        }

        /// <summary>
        /// VM -> Client: Result of a join request.
        /// </summary>
        internal class JoinResponse : MessageBase
        {
            internal const string MessageTypeString = "join-response";

            public bool Succeeded { get; set; }
            public string ErrorDetails { get; set; }
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

            /// <summary>
            /// PowerShell startup script.
            /// </summary>
            public const string WindowsStartupScriptPs1 = "windows-startup-script-ps1";
            public static readonly string[] WindowsStartupScripts = new[]
            {
                WindowsStartupScriptPs1,
                "windows-startup-script-cmd",
                "windows-startup-script-bat",
                "windows-startup-script-url"
            };
        }
    }

    public class DomainJoinFailedException : Exception
    {
        public DomainJoinFailedException(string message) : base(message)
        {
        }
    }
}
