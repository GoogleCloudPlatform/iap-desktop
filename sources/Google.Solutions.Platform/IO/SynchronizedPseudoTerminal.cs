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
    /// <summary>
    /// PseudoTerminal that delivers events on a defined synchronization
    /// context.
    /// </summary>
    public class SynchronizedPseudoTerminal : IPseudoTerminal
    {
        private readonly IPseudoTerminal target;
        private readonly SynchronizationContext callbackContext;

        public SynchronizedPseudoTerminal(
            IPseudoTerminal target,
            SynchronizationContext context)
        {
            this.target = target.ExpectNotNull(nameof(target));
            this.callbackContext = context.ExpectNotNull(nameof(context));

            this.target.OutputAvailable += OnOutputAvailable;
            this.target.FatalError += OnFatalError;
            this.target.Disconnected += OnDisconnected;
        }

        private void OnFatalError(object sender, PseudoTerminalErrorEventArgs args)
        {
            this.callbackContext.Send(
                _ => this.FatalError?.Invoke(this, args),
                null);
        }

        private void OnOutputAvailable(object sender, PseudoTerminalDataEventArgs args)
        {
            this.callbackContext.Send(
                _ => this.OutputAvailable?.Invoke(this, args),
                null);
        }
        private void OnDisconnected(object sender, EventArgs args)
        {
            this.callbackContext.Send(
                _ => this.Disconnected?.Invoke(this, args),
                null);
        }

        //---------------------------------------------------------------------
        // IPseudoTerminal.
        //---------------------------------------------------------------------

        public event EventHandler<PseudoTerminalDataEventArgs>? OutputAvailable;
        public event EventHandler<PseudoTerminalErrorEventArgs>? FatalError;
        public event EventHandler<EventArgs>? Disconnected;

        public bool IsClosed
        {
            get => this.target.IsClosed;
        }

        public Task ResizeAsync(
            PseudoTerminalSize size,
            CancellationToken cancellationToken)
        {
            return this.target.ResizeAsync(size, cancellationToken);
        }

        public Task WriteAsync(string data, CancellationToken cancellationToken)
        {
            return this.target.WriteAsync(data, cancellationToken);
        }

        public Task DrainAsync()
        {
            return this.target.DrainAsync();
        }

        public Task CloseAsync()
        {
            return this.target.CloseAsync();
        }

        public void Dispose()
        {
            this.target.OutputAvailable -= OnOutputAvailable;
            this.target.FatalError -= OnFatalError;
            this.target.Disconnected -= OnDisconnected;

            this.target.Dispose();
        }
    }
}