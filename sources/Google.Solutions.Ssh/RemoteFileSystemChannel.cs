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

using Google.Solutions.Ssh.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    /// <summary>
    /// Channel for interacting with remote file.
    /// </summary>
    public class RemoteFileSystemChannel : RemoteChannelBase
    {
        /// <summary>
        /// Channel handle, must only be accessed on worker thread.
        /// </summary>
        private readonly SshSftpChannel nativeChannel;

        public override RemoteConnection Connection { get; }

        internal RemoteFileSystemChannel(
            RemoteConnection connection,
            SshSftpChannel nativeChannel)
        {
            this.Connection = connection;
            this.nativeChannel = nativeChannel;
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void Close()
        {
            Debug.Assert(this.Connection.IsRunningOnWorkerThread);

            this.nativeChannel.Dispose();
        }

        internal override void OnReceive()
        {
            //
            // We're never expecting any unsolicited data from the
            // server, so ignore the callback.
            //
        }

        internal override void OnReceiveError(Exception exception)
        {
            Debug.Assert(false, "OnReceiveError should never be called");
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public Task<IReadOnlyCollection<SshSftpFileInfo>> ListFilesAsync(string remotePath)
        {
            return this.Connection
                .RunSendOperationAsync(_ =>
                {
                    Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                    return this.nativeChannel.ListFiles(remotePath);
                });
        }
    }
}
