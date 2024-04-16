//
// Copyright 2024 Google LLC
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

using Google.Solutions.Common.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs

namespace Google.Solutions.Platform.IO
{
    public static class PseudoTerminalExtensions
    {
        /// <summary>
        /// Wrap a pseudo-console so that all callbacks are forced
        /// onto a synchronization context.
        /// </summary>
        public static IPseudoConsole BindToSynchronizationContext(
            this IPseudoConsole console,
            SynchronizationContext callbackContext)
        {
            return new SynchronizationContextBoundPseudoConsole(
                console,
                callbackContext);
        }

        private class SynchronizationContextBoundPseudoConsole : IPseudoConsole
        {
            private readonly IPseudoConsole console;
            private readonly SynchronizationContext callbackContext;

            public SynchronizationContextBoundPseudoConsole(
                IPseudoConsole console,
                SynchronizationContext context)
            {
                this.console = console.ExpectNotNull(nameof(console));
                this.callbackContext = context.ExpectNotNull(nameof(context));

                this.console.OutputAvailable += OnOutputAvailable;
                this.console.OutputFailed += OnOutputFailed;
            }

            private void OnOutputFailed(object sender, PseudoConsoleErrorEventArgs args)
            {
                this.callbackContext.Send(
                    _ => this.OutputFailed?.Invoke(this, args),
                    null);
            }

            private void OnOutputAvailable(object sender, PseudoConsoleDataEventArgs args)
            {
                this.callbackContext.Send(
                    _ => this.OutputAvailable?.Invoke(this, args),
                    null);
            }

            //---------------------------------------------------------------------
            // IPseudoConsole.
            //---------------------------------------------------------------------

            public event EventHandler<PseudoConsoleDataEventArgs>? OutputAvailable;
            public event EventHandler<PseudoConsoleErrorEventArgs>? OutputFailed;

            public bool IsClosed
            {
                get => this.console.IsClosed;
            }

            public Task ResizeAsync(
                PseudoConsoleSize size,
                CancellationToken cancellationToken)
            {
                return this.console.ResizeAsync(size, cancellationToken);
            }

            public Task WriteAsync(string data, CancellationToken cancellationToken)
            {
                return this.console.WriteAsync(data, cancellationToken);
            }

            public Task DrainAsync()
            {
                return this.console.DrainAsync();
            }

            public Task CloseAsync()
            {
                return this.console.CloseAsync();
            }

            public void Dispose()
            {
                this.console.OutputAvailable -= OnOutputAvailable;
                this.console.OutputFailed -= OnOutputFailed;

                this.console.Dispose();
            }
        }
    }
}