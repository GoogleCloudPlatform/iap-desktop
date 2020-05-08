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

using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1031 // catch Exception

namespace Google.Solutions.IapDesktop.Application.Util
{
    public abstract class SingletonApplicationBase
    {
        public string Name { get; }

        protected string MutexName => $"Local\\{Name}_{Environment.UserName}";
        protected string PipeName => $"{Name}_{Environment.UserName}";

        protected SingletonApplicationBase(string name)
        {
            this.Name = name;
        }

        protected abstract int HandleFirstInvocation(string[] args);

        protected abstract int HandleSubsequentInvocation(string[] args);

        protected abstract void HandleSubsequentInvocationException(Exception e);

        public int Run(string[] args)
        {
            //
            // Create a mutex to distinguish whether this is the first process
            // or a subsequent process.
            //
            // The mutex is locked down so that it is only visible within the
            // current session and can only be accessed by the current user.
            //
            var mutexSecurity = new MutexSecurity();
            mutexSecurity.AddAccessRule(
                new MutexAccessRule(
                    WindowsIdentity.GetCurrent().Owner,
                    MutexRights.Synchronize | MutexRights.Modify,
                    AccessControlType.Allow));

            using (var mutex = new Mutex(
                true,   // Try to claim ownership.
                MutexName,
                out bool ownsMutex,
                mutexSecurity))
            {
                if (ownsMutex)
                {
                    // Successfully took ownership of mutex, so this is the first process.

                    // Start named pipe server in background.
                    var cts = new CancellationTokenSource();
                    var serverTask = Task.Factory.StartNew(
                        async () => await RunNamedPipeServer(cts.Token),
                        TaskCreationOptions.LongRunning);

                    // Run main invocation in foreground and wait for it to finish.
                    var result = HandleFirstInvocation(args);

                    // Stop the server.
                    cts.Cancel();
                    serverTask.Wait();
                    return result;
                }
                else
                {
                    // Failed to take ownership of mutex, so this is a subsequent process.
                    return PostCommandToNamedPipeServer(args);
                }
            }
        }

        private int PostCommandToNamedPipeServer(string[] args)
        {
            using (var pipe = new NamedPipeClientStream(
                ".",
                PipeName,
                PipeDirection.InOut))
            {
                pipe.Connect();

                using (var reader = new BinaryReader(pipe))
                using (var writer = new BinaryWriter(pipe))
                {
                    writer.Write(args.Length);

                    for (int i = 0; i < args.Length; i++)
                    {
                        writer.Write(args[i]);
                    }

                    return reader.ReadInt32();
                }
            }
        }

        private async Task RunNamedPipeServer(CancellationToken token)
        {
            var pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(
                new PipeAccessRule(
                    WindowsIdentity.GetCurrent().Owner,
                    PipeAccessRights.FullControl,
                    AccessControlType.Allow));

            while (true)
            {
                try
                {
                    using (var pipe = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        1,  // Translates to FILE_FLAG_FIRST_PIPE_INSTANCE
                        PipeTransmissionMode.Message,
                        PipeOptions.None,
                        0,
                        0,
                        pipeSecurity))
                    {
                        await pipe.WaitForConnectionAsync(token);

                        //
                        // The server expects:
                        // IN: <int32> number of arguments
                        // IN: <string> argument #n
                        // IN:  (repeat...)
                        // OUT: return code
                        //

                        using (var reader = new BinaryReader(pipe))
                        using (var writer = new BinaryWriter(pipe))
                        {
                            int argsCount = reader.ReadInt32();
                            var args = new string[argsCount];
                            for (int i = 0; i < argsCount; i++)
                            {
                                args[i] = reader.ReadString();
                            }

                            try
                            {
                                int result = HandleSubsequentInvocation(args);
                                writer.Write(result);
                            }
                            catch (Exception e)
                            {
                                HandleSubsequentInvocationException(e);
                                writer.Write(-1);
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch (IOException e)
                {
                    HandleSubsequentInvocationException(e);
                }
            }
        }
    }
}
