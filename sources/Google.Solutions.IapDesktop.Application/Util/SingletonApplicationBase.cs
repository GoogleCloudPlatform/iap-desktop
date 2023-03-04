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
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable CA1031 // catch Exception

namespace Google.Solutions.IapDesktop.Application.Util
{
    public abstract class SingletonApplicationBase
    {
        internal uint SessionId { get; }
        public string Name { get; }

        internal string MutexName { get; }
        internal string PipeName { get; }

        protected SingletonApplicationBase(string name)
        {
            this.Name = name;

            var processId = (uint)Process.GetCurrentProcess().Id;
            if (UnsafeNativeMethods.ProcessIdToSessionId(
                processId,
                out var sessionId))
            {
                this.SessionId = sessionId;
            }
            else
            {
                //
                // Use a fake session ID that's high enough to be unlikely
                // to ever conflict with real session IDs.
                //
                this.SessionId = 0xF0000000 | (uint)processId;
            }

            //
            // NB. Mutex names are case-sensitive, but pipes aren't.
            // Normalize casing to prevent a situation where a second
            // instance can claim the mutex (because it uses a different
            // casing) but then can't open another pipe server because
            // the pipe name is taken (with a different casing).
            //
            // Named pipes are always global, and there's no equivalent
            // for Local\. We therefore incorporate the session ID into
            // the name.
            //

            var uniqueName = $"{this.Name.ToLower()}_{this.SessionId:X}_{Environment.UserName.ToLower()}";
            this.MutexName = $"Local\\{uniqueName}";
            this.PipeName = uniqueName;
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

            try
            {
                using (var mutex = new Mutex(
                    true,   // Try to claim ownership.
                    MutexName,
                    out bool ownsMutex,
                    mutexSecurity))
                {
                    if (ownsMutex)
                    {
                        //
                        // Successfully took ownership of mutex, so this is the first process.
                        //
                        // Start named pipe server in background.
                        //
                        using (var cts = new CancellationTokenSource())
                        {
                            var serverTask = Task.Factory.StartNew(
                                async () => await RunNamedPipeServer(cts.Token).ConfigureAwait(false),
                                TaskCreationOptions.LongRunning);

                            // Run main invocation in foreground and wait for it to finish.
                            var result = HandleFirstInvocation(args);

                            // Stop the server.
                            cts.Cancel();
                            serverTask.Wait();
                            return result;
                        }
                    }
                    else
                    {
                        //
                        // Failed to take ownership of mutex, so this is a subsequent process.
                        //
                        return PostCommandToNamedPipeServer(args);
                    }
                }
            }
            catch (IOException e)
            {
                ApplicationTraceSources.Default.TraceError(
                    "Singleton: Failed to communicate with mutex owner ({0})",
                    e.Message);

                //
                // Ignore the existing instance and start a new instance.
                //
                return HandleFirstInvocation(args);
            }
            catch (UnauthorizedAccessException)
            {
                ApplicationTraceSources.Default.TraceError(
                    "Singleton: Failed to access mutex");

                //
                // Failed to access mutex. Most likely, that's because the Mutex
                // has been created at a different integrity level (for ex, the first
                // process was launched elevated, but this process is non-elevared).
                //
                // Ignore the existing instance and start a new instance.
                //
                return HandleFirstInvocation(args);
            }
        }

        private static void TrySetForegroundWindow(int processId)
        {
            // Try to pass focus to other instance. Note that the 
            // main instance's process cannot claim the focus because
            // Windows does not allow that.
            try
            {
                var mainProcess = Process.GetProcessById(processId);
                var mainHwnd = mainProcess.MainWindowHandle;
                UnsafeNativeMethods.SetForegroundWindow(mainHwnd);

                if (UnsafeNativeMethods.IsIconic(mainHwnd))
                {
                    UnsafeNativeMethods.ShowWindow(mainHwnd, UnsafeNativeMethods.SW_RESTORE);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MessageBox.Show(
                    e.Message,
                    "Failed to pass focus to main instance",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
#endif
                // Nevermind.
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

                    int returnCode = reader.ReadInt32();
                    int processIdOfMainInstance = reader.ReadInt32();

                    TrySetForegroundWindow(processIdOfMainInstance);

                    return returnCode;
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
                        await pipe
                            .WaitForConnectionAsync(token)
                            .ConfigureAwait(false);

                        //
                        // The server expects:
                        // IN: <int32> number of arguments
                        // IN: <string> argument #n
                        // IN:  (repeat...)
                        // OUT: return code
                        // OUT: process ID
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
                                writer.Write(Process.GetCurrentProcess().Id);
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

        //---------------------------------------------------------------------
        // P/Invoke definitions.
        //---------------------------------------------------------------------

        private static class UnsafeNativeMethods
        {
            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern bool IsIconic(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            public const int SW_RESTORE = 9;

            [DllImport("kernel32.dll")]
            public static extern bool ProcessIdToSessionId(
                uint dwProcessId,
                out uint pSessionId);
        }
    }
}
