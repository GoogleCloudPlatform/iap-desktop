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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Solutions.Compute;
using Google.Solutions.Compute.Iap;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.IapClient
{
    internal class IapClient
    {
        private enum ExitCode : int
        {
            Success = 0,
            InvalidParameters = 1,
            UnhandledException = 2
        }

        private const string ProgramName = "IapClient";

        public VmInstanceReference Instance { get; }
        public ushort Port { get; }

        public IapClient(VmInstanceReference instance, ushort port)
        {
            this.Instance = instance;
            this.Port = port;
        }

        public async Task RunAsync(CancellationToken token)
        {
            ICredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets()
                {
                    ClientId = "78381520511-4fu6ve6b49kknk3dkdnpudoi0tivq6jn.apps.googleusercontent.com",
                    ClientSecret = "dRgZl1efp_JKcUqQusuaVIrS"
                },
                new[] { IapTunnelingEndpoint.RequiredScope },
                Environment.UserName,
                CancellationToken.None,
                new FileDataStore("Google.Solutions.CloudIap"),
                new LocalServerCodeReceiver(Resources.AuthorizationSuccessful));

            var iapEndpoint = new IapTunnelingEndpoint(
                    credential,
                    this.Instance,
                    this.Port,
                    IapTunnelingEndpoint.DefaultNetworkInterface);

            // Probe connection.
            using (var stream = new SshRelayStream(iapEndpoint))
            {
                await stream.TestConnectionAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }

            // Start listener to enable clients to connect.
            var listener = SshRelayListener.CreateLocalListener(iapEndpoint);

            Console.WriteLine($"Forwarding localhost:{listener.LocalPort} "+
                $"to {this.Instance}:{this.Port}...");

            listener.ClientConnected += (sender, args) =>
            {
                using (new CustomConsoleColor(ConsoleColor.Yellow))
                {
                    Console.WriteLine($"Client {args.Client} connected");
                }
            };
            listener.ClientDisconnected += (sender, args) =>
            {
                using (new CustomConsoleColor(ConsoleColor.Yellow))
                {
                    Console.WriteLine($"Client {args.Client} disconnected");
                }
            };
            listener.ConnectionFailed += (sender, exception) =>
            {
                DisplayError(exception.Exception);
            };

            await listener.ListenAsync(token);
        }

        //---------------------------------------------------------------------
        // Command line handling
        //---------------------------------------------------------------------

        public static void Main(string[] args)
        {
            try
            {
                var commandLine = CommandLine.Parse(args);

                try
                {
                    var tokenSource = new CancellationTokenSource();
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        tokenSource.Cancel();
                    };

                    new IapClient(commandLine.InstanceReference, commandLine.Port)
                        .RunAsync(tokenSource.Token)
                        .Wait();

                    Environment.Exit((int)ExitCode.Success);
                }
                catch (AggregateException e)
                {
                    DisplayError(e);
                    Environment.Exit((int)ExitCode.UnhandledException);
                }
            }
            catch (ArgumentException e)
            {
                Environment.Exit((int)DisplayHelp(e.Message));
                return;
            }
        }

        private static ExitCode DisplayHelp(string error)
        {
            using (new CustomConsoleColor(ConsoleColor.Red))
            {
                if (error != null)
                {
                    Console.WriteLine(error);
                    Console.WriteLine();
                }
                Console.WriteLine(
                    $"Usage: {ProgramName} INSTANCE_NAME:PORT --project= --zone=");
            }

            return ExitCode.InvalidParameters;
        }

        private static void DisplayError(Exception exception)
        {
            using (new CustomConsoleColor(ConsoleColor.Red))
            {
                if (exception is AggregateException aggregateEx)
                {
                    foreach (var innerException in aggregateEx.InnerExceptions)
                    {
                        Console.WriteLine($"{innerException.GetType().Name}: {innerException.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"{exception.GetType().Name}: {exception.Message}");
                }
            }
        }
    }
}
