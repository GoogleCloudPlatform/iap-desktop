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

using Google.Solutions.Common.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    /// <summary>
    /// Base class for channels that support async use.
    /// </summary>
    public abstract class SshChannelBase : IDisposable
    {
        public bool IsClosed { get; private set; } = false;

        public abstract SshConnection Connection { get; }

        /// <summary>
        /// Perform receive operation. Called on SSH worker thread.
        /// </summary>
        internal abstract void OnReceive();

        /// <summary>
        /// Receive failed. Called on SSH worker thread.
        /// </summary>
        internal abstract void OnReceiveError(Exception exception);

        /// <summary>
        /// Close handles. Called on SSH worker thread.
        /// </summary>
        protected abstract void Close();

        /// <summary>
        /// Close handles. Can be called from any thread.
        /// </summary>
        public async Task CloseAsync()
        {
            if (this.IsClosed)
            {
                return;
            }

            //
            // Switch to worker thread and dispose.
            //
            try
            {
                await this.Connection.RunAsync(c =>
                {
                    using (c.Session.AsBlocking())
                    {
                        Dispose();
                    }
                });
            }
            catch (SshConnectionClosedException)
            {
                //
                // Connection closed already. This can happen
                // if the connection was disconnected by the
                // server, and the channel is being disposed
                // as a result of that.
                //
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.Connection.IsRunningOnWorkerThread)
            {
                try
                {
                    if (!this.IsClosed)
                    {
                        Close();
                        this.IsClosed = true;
                    }
                }
                catch (Exception e)
                {
                    //
                    // NB. This is non-fatal - we're tearing down the 
                    // connection anyway.
                    //
                    SshTraceSource.Log.TraceError(
                        "Closing connection failed for {0}: {1}",
                        Thread.CurrentThread.Name,
                        e);
                }
            }
            else if (disposing)
            {
                //
                // Initiate dispose, but don't block the caller.
                //
                _ = this.CloseAsync();
            }
        }
    }
}
