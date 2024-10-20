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

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.IO
{
    /// <summary>
    /// A pseudo-terminal or pty that accepts and produces VT/xterm-formatted
    /// input and output.
    /// </summary>
    public interface IPseudoTerminal : IDisposable
    {
        /// <summary>
        /// Raised when output is available.
        /// </summary>
        /// <remarks>
        /// The event can be delivered on any thread
        /// </remarks>
        event EventHandler<PseudoTerminalDataEventArgs>? OutputAvailable;

        /// <summary>
        /// Raised when a fatal error occured. After a fatal error,
        /// might be in an unusable state and should be closed.
        /// </summary>
        /// <remarks>
        /// The event can be delivered on any thread
        /// </remarks>
        event EventHandler<PseudoTerminalErrorEventArgs>? FatalError;

        /// <summary>
        /// Raised when the console was disconnected unexpectedly.
        /// </summary>
        /// <remarks>
        /// The event can be delivered on any thread
        /// </remarks>
        event EventHandler<EventArgs>? Disconnected;

        /// <summary>
        /// Check if the device is closed, indicating that the
        /// session has ended.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Adjust the size of the current session.
        /// </summary>
        Task ResizeAsync(
            PseudoTerminalSize dimensions,
            CancellationToken cancellationToken);

        /// <summary>
        /// Write Xterm-formatted data to the device
        /// </summary>
        Task WriteAsync(
            string data,
            CancellationToken cancellationToken);

        /// <summary>
        /// Drain output until EOF is received.
        /// </summary>
        Task DrainAsync();

        /// <summary>
        /// Drain output and close the session.
        /// </summary>
        /// <remarks>
        /// Does not raise a Disconnected event.
        /// </remarks>
        Task CloseAsync();
    }

    /// <summary>
    /// Size of a console, in characters.
    /// </summary>
    public readonly struct PseudoTerminalSize
    {
        public static readonly PseudoTerminalSize Default = new PseudoTerminalSize(80, 24);

        public PseudoTerminalSize(ushort width, ushort height)
        {
            Debug.Assert(width > 0 && height > 0);

            this.Width = width;
            this.Height = height;
        }

        public ushort Width { get; }

        public ushort Height { get; }

        public override string ToString()
        {
            return $"{this.Width}x{this.Height}";
        }
    }


    /// <summary>
    /// Event data for console data.
    /// </summary>
    public class PseudoTerminalDataEventArgs : EventArgs
    {
        /// <summary>
        /// Xterm-encoded data.
        /// </summary>
        public string Data { get; }

        public PseudoTerminalDataEventArgs(string data)
        {
            Debug.Assert(data.Length > 0);
            this.Data = data;
        }
    }

    /// <summary>
    /// Event data for a console error.
    /// </summary>
    public class PseudoTerminalErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }

        public PseudoTerminalErrorEventArgs(Exception exception)
        {
            this.Exception = exception;
        }
    }
}
