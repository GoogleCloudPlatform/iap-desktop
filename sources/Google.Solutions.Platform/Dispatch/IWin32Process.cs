//
// Copyright 2023 Google LLC
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

using Google.Solutions.Platform.IO;
using Microsoft.Win32.SafeHandles;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.Dispatch
{
    /// <summary>
    /// A Win32 process.
    /// </summary>
    public interface IWin32Process : IDisposable
    {
        /// <summary>
        /// Raised when the process exited.
        /// </summary>
        event EventHandler? Exited;

        /// <summary>
        /// Image name, without path.
        /// </summary>
        string ImageName { get; }

        /// <summary>
        /// Process ID.
        /// </summary>
        uint Id { get; }

        /// <summary>
        /// Handle for awaiting process termination.
        /// </summary>
        WaitHandle WaitHandle { get; }

        /// <summary>
        /// Process handle.
        /// </summary>
        SafeProcessHandle Handle { get; }

        /// <summary>
        /// Get job this process is associated with, if any.
        /// </summary>
        IWin32Job? Job { get; }

        /// <summary>
        /// Resume the process.
        /// </summary>
        void Resume();

        /// <summary>
        /// Send WM_CLOSE to process and wait for the process to
        /// terminate gracefully. Otherwise, terminate forcefully.
        /// </summary>
        /// <returns>true if the process terminated gracefully.</returns>
        Task<bool> CloseAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Wait for process to terminate.
        /// </summary>
        /// <returns>the exit code.</returns>
        Task<uint> WaitAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Forcefully terminate the process.
        /// </summary>
        void Terminate(uint exitCode);

        /// <summary>
        /// Indicates whether the process is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Numer of top-level windows owned by this process.
        /// </summary>
        int WindowCount { get; }

        /// <summary>
        /// The NT/WTS session that this process is running in.
        /// </summary>
        IWtsSession Session { get; }

        /// <summary>
        /// Pseudo-console for this process, if any.
        /// </summary>
        IPseudoConsole? PseudoConsole { get; }
    }
}
