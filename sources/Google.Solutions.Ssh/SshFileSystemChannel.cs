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
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    /// <summary>
    /// Channel for interacting with remote file.
    /// </summary>
    public class SshFileSystemChannel : SshChannelBase
    {
        /// <summary>
        /// Channel handle, must only be accessed on worker thread.
        /// </summary>
        private readonly Libssh2SftpChannel nativeChannel;

        /// <summary>
        /// Connection used by this channel.
        /// </summary>
        public override SshConnection Connection { get; }

        internal SshFileSystemChannel(
            SshConnection connection,
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

        /// <summary>
        /// List contents of a directory.
        /// </summary>
        public Task<IReadOnlyCollection<Libssh2SftpFileInfo>> ListFilesAsync(
            string remotePath)
        {
            Precondition.ExpectNotEmpty(remotePath, nameof(remotePath));

            return this.Connection.RunThrowingOperationAsync(c =>
            {
                Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                using (c.Session.AsBlocking())
                {
                    return this.nativeChannel.ListFiles(remotePath);
                }
            });
        }

        /// <summary>
        /// Create or open a file.
        /// </summary>
        public Task<Stream> CreateFileAsync(
            string remotePath,
            FileMode mode,
            FileAccess access,
            FilePermissions permissions)
        {
            Precondition.ExpectNotEmpty(remotePath, nameof(remotePath));

            var flags = mode switch
            {
                FileMode.Create => LIBSSH2_FXF_FLAGS.CREAT,
                FileMode.CreateNew => LIBSSH2_FXF_FLAGS.CREAT | LIBSSH2_FXF_FLAGS.EXCL,
                FileMode.OpenOrCreate => LIBSSH2_FXF_FLAGS.CREAT,
                FileMode.Open => (LIBSSH2_FXF_FLAGS)0,
                FileMode.Truncate => LIBSSH2_FXF_FLAGS.TRUNC,
                FileMode.Append => LIBSSH2_FXF_FLAGS.APPEND,
                
                _ => throw new ArgumentException(nameof(mode)),
            };

            if (access.HasFlag(FileAccess.Read))
            {
                flags |= LIBSSH2_FXF_FLAGS.READ;
            }

            if (access.HasFlag(FileAccess.Write))
            {
                flags |= LIBSSH2_FXF_FLAGS.WRITE;
            }

            return this.Connection.RunThrowingOperationAsync<Stream>(c =>
            {
                Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                using (c.Session.AsBlocking())
                {
                    return new SshFileStream(
                        this.Connection,
                        this.nativeChannel.CreateFile(
                            remotePath,
                            flags,
                            permissions),
                        flags);
                }
            });
        }
    }
}
