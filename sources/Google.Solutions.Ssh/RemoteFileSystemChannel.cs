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

using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    /// <summary>
    /// Channel for interacting with remote file.
    /// </summary>
    public class RemoteFileSystemChannel : RemoteChannelBase
    {
        //
        // SFTP effectively limits the size of a packet to 32 KB, see
        // <https://datatracker.ietf.org/doc/html/draft-ietf-secsh-filexfer-13#section-4>
        //
        // libssh2 uses a slightly smaller limit of 30000 bytes 
        // (MAX_SFTP_OUTGOING_SIZE, MAX_SFTP_READ_SIZE).
        // Using a buffer larger than 30000 bytes therefore doen't
        // provide much value.
        //
        // Note that IAP/SSH Relay uses 16KB as maximum message size,
        // so a 32000 byte packet will be split into 2 messages. 
        // That's still more efficient than using a SFTP packet
        // size below 16 KB as it at least limits the number of 
        // SSH_FXP_STATUS packets that need to be exchanged.
        //
        internal const int CopyBufferSize = 30000;

        /// <summary>
        /// Channel handle, must only be accessed on worker thread.
        /// </summary>
        private readonly Libssh2SftpChannel nativeChannel;

        public override RemoteConnection Connection { get; }

        internal RemoteFileSystemChannel(
            RemoteConnection connection,
            Libssh2SftpChannel nativeChannel)
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

        public Task<IReadOnlyCollection<Libssh2SftpFileInfo>> ListFilesAsync(
            string remotePath)
        {
            Precondition.ExpectNotEmpty(remotePath, nameof(remotePath));

            return this.Connection
                .RunThrowingOperationAsync(c =>
                {
                    Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                    using (c.Session.AsBlocking())
                    {
                        return this.nativeChannel.ListFiles(remotePath);
                    }
                });
        }

        public Task UploadFileAsync(
            string remotePath,
            Stream source,
            LIBSSH2_FXF_FLAGS flags,
            FilePermissions permissions,
            IProgress<uint> progress,
            CancellationToken cancellationToken)
        {
            Precondition.ExpectNotEmpty(remotePath, nameof(remotePath));
            Precondition.ExpectNotNull(source, nameof(source));
            Precondition.ExpectNotNull(progress, nameof(progress));

            Debug.Assert(source.CanRead);

            return this.Connection
                .RunThrowingOperationAsync<object>(c =>
                {
                    Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                    //
                    // Upload the entire file as one operation, possibly
                    // blocking other channels.
                    //
                    // Temporarily disable the timeout.
                    //
                    using (c.Session.AsBlocking())
                    using (var file = this.nativeChannel.CreateFile(
                            remotePath,
                            flags,
                            permissions))
                    {
                        var buffer = new byte[CopyBufferSize];
                        int bytesRead;

                        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            file.Write(buffer, bytesRead);

                            progress.Report((uint)bytesRead);
                        }

                        return bytesRead;
                    }
                });
        }

        public Task DownloadFileAsync(
            string remotePath,
            Stream target,
            IProgress<uint> progress,
            CancellationToken cancellationToken)
        {
            Precondition.ExpectNotEmpty(remotePath, nameof(remotePath));
            Precondition.ExpectNotNull(target, nameof(target));
            Precondition.ExpectNotNull(progress, nameof(progress));

            Debug.Assert(target.CanWrite);

            return this.Connection
                .RunThrowingOperationAsync<object>(c =>
                {
                    Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                    //
                    // Upload the entire file as one operation, possibly
                    // blocking other channels.
                    //
                    // Temporarily disable the timeout.
                    //
                    using (c.Session.AsBlocking())
                    using (var file = this.nativeChannel.CreateFile(
                            remotePath,
                            LIBSSH2_FXF_FLAGS.READ,
                            (FilePermissions)0))
                    {
                        var buffer = new byte[CopyBufferSize];
                        uint bytesRead;

                        while ((bytesRead = file.Read(buffer)) > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            target.Write(buffer, 0, (int)bytesRead);

                            progress.Report((uint)bytesRead);
                        }

                        return bytesRead;
                    }
                });
        }
    }
}
