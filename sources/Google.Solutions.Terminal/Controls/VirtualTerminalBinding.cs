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
using Google.Solutions.Platform.IO;
using System;
using System.Diagnostics;
using System.Threading;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Connection between a Terminal and a Pty.
    /// </summary>
    internal class VirtualTerminalBinding : IDisposable
    {
        private readonly VirtualTerminal terminal;

        /// <summary>
        /// Pty to read from and write to.
        /// </summary>
        internal IPseudoTerminal Device { get; }

        public VirtualTerminalBinding(VirtualTerminal terminal, IPseudoTerminal device)
        {
            this.terminal = terminal.ExpectNotNull(nameof(terminal));
            this.Device = device.ExpectNotNull(nameof(device));

            this.terminal.UserInput += OnTerminalUserInput;
            this.terminal.DimensionsChanged += OnTerminalDimensionsChanged;
            this.terminal.Disposed += OnTerminalDisposed;
            this.Device.OutputAvailable += OnDeviceOutput;
            this.Device.FatalError += OnDeviceError;
            this.Device.Disconnected += OnDeviceDisconnected;
        }

        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        private void OnTerminalDisposed(object sender, EventArgs args)
        {
            try
            {
                this.Device.Dispose();
            }
            catch (Exception e)
            {
                Debug.Fail($"Disposing device failed: {e}");
                throw;
            }
        }

        private async void OnTerminalUserInput(object sender, VirtualTerminalInputEventArgs args)
        {
            //
            // The user hit keystrokes in the terminal. Forward this to the device
            // without blocking the UI thread.
            //
            try
            {
                if (!this.Device.IsClosed)
                {
                    await this.Device
                        .WriteAsync(args.Data, CancellationToken.None)
                        .ConfigureAwait(true);
                }
            }
            catch (Exception e)
            {
                this.terminal.ReceiveError(e);
            }
        }

        private async void OnTerminalDimensionsChanged(object sender, EventArgs args)
        {
            //
            // The user resized the terminal. Forward this to the device
            // without blocking the UI thread.
            //
            try
            {
                if (!this.Device.IsClosed)
                {
                    await this.Device
                        .ResizeAsync(this.terminal.Dimensions, CancellationToken.None)
                        .ConfigureAwait(true);
                }
            }
            catch (Exception e)
            {
                this.terminal.ReceiveError(e);
            }
        }

        private void OnDeviceError(object sender, PseudoTerminalErrorEventArgs args)
        {
            //
            // The device encountered an error.
            //
            this.terminal.ReceiveError(args.Exception);
        }

        private void OnDeviceOutput(object sender, PseudoTerminalDataEventArgs args)
        {
            //
            // The device produced some output. Forward to the terminal
            // for rendering.
            //
            try
            {
                this.terminal.ReceiveOutput(args.Data);
            }
            catch (Exception e)
            {
                this.terminal.ReceiveError(e);
            }
        }

        private void OnDeviceDisconnected(object sender, EventArgs args)
        {
            try
            {
                this.terminal.ReceiveClose();
            }
            catch (Exception e)
            {
                this.terminal.ReceiveError(e);
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.terminal.UserInput -= OnTerminalUserInput;
            this.terminal.DimensionsChanged -= OnTerminalDimensionsChanged;
            this.Device.OutputAvailable -= OnDeviceOutput;
            this.Device.FatalError -= OnDeviceError;
        }
    }
}
