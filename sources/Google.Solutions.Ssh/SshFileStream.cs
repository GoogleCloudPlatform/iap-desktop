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

using Google.Solutions.Ssh.Native;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh
{
    internal class SshFileStream : Stream // TODO: test
    {
        /// <summary>
        /// Suggested buffer size to use for reading from, or
        /// writing to the stream.
        ///
        /// SFTP effectively limits the size of a packet to 32 KB, see
        /// <https://datatracker.ietf.org/doc/html/draft-ietf-secsh-filexfer-13#section-4>
        ///
        /// libssh2 uses a slightly smaller limit of 30000 bytes 
        /// (MAX_SFTP_OUTGOING_SIZE, MAX_SFTP_READ_SIZE).
        /// Using a buffer larger than 30000 bytes therefore doen't
        /// provide much value.
        ///
        /// Note that IAP/SSH Relay uses 16KB as maximum message size,
        /// so a 32000 byte packet will be split into 2 messages. 
        /// That's still more efficient than using a SFTP packet
        /// size below 16 KB as it at least limits the number of 
        /// SSH_FXP_STATUS packets that need to be exchanged.
        ///
        /// </summary>
        public const int CopyBufferSize = 30000;

        /// <summary>
        /// Native channel, can only be accessed on the worker thread.
        /// </summary>
        private readonly Libssh2SftpFileChannel nativeChannel;

        /// <summary>
        /// Connection used by this stream.
        /// </summary>
        public SshConnection Connection { get; }

        /// <summary>
        /// Flags the file has been opened with.
        /// </summary>
        internal LIBSSH2_FXF_FLAGS Flags { get; }

        internal SshFileStream(
            SshConnection connection,
            Libssh2SftpFileChannel nativeChannel,
            LIBSSH2_FXF_FLAGS flags)
        {
            this.Connection = connection;
            this.nativeChannel = nativeChannel;
            this.Flags = flags;
        }

        protected override void Dispose(bool disposing)
        {
            this.nativeChannel.Dispose();
            base.Dispose(disposing);
        }

        //----------------------------------------------------------------------
        // Reading.
        //----------------------------------------------------------------------

        public override bool CanRead
        {
            get => this.Flags.HasFlag(LIBSSH2_FXF_FLAGS.READ);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(
                "The stream can only be accessed asynchonously");
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            if (!this.CanRead)
            {
                throw new NotSupportedException("Stream is not readable");
            }

            cancellationToken.ThrowIfCancellationRequested();

            //
            // Perform a synchronous read on the worker thread.
            //
            return await this.Connection
                .RunThrowingOperationAsync(session =>
                {
                    Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                    using (session.Session.AsBlocking())
                    {
                        if (offset == 0 && count == buffer.Length) 
                        {
                            //
                            // Use the supplied buffer.
                            //
                            return (int)this.nativeChannel.Read(buffer);
                        }
                        else
                        {
                            var readBuffer = new byte[count];
                            var bytesRead = this.nativeChannel.Read(readBuffer);
                            Array.Copy(readBuffer, 0, buffer, offset, count);
                            return (int)bytesRead;
                        }
                    }
                })
                .ConfigureAwait(false);
        }

        //----------------------------------------------------------------------
        // Writing.
        //----------------------------------------------------------------------
        
        public override bool CanWrite
        {
            get => this.Flags.HasFlag(LIBSSH2_FXF_FLAGS.WRITE);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(
                "The stream can only be accessed asynchonously");
        }

        public override async Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            if (!this.CanWrite)
            {
                throw new NotSupportedException("Stream is not writable");
            }

            cancellationToken.ThrowIfCancellationRequested();

            //
            // Perform a synchronous write on the worker thread.
            //
            await this.Connection
                .RunThrowingOperationAsync<object?>(session =>
                {
                    Debug.Assert(this.Connection.IsRunningOnWorkerThread);

                    using (session.Session.AsBlocking())
                    {
                        if (offset == 0)
                        {
                            //
                            // Use the supplied buffer.
                            //
                            this.nativeChannel.Write(buffer, count);
                        }
                        else
                        {
                            var writeBuffer = new byte[count];
                            Array.Copy(buffer, offset, writeBuffer, 0, count);
                            this.nativeChannel.Write(writeBuffer, count);
                        }
                    }

                    return session;
                })
                .ConfigureAwait(false);
        }

        public override void Flush()
        {
        }

        //----------------------------------------------------------------------
        // Seeking (not supported).
        //----------------------------------------------------------------------

        public override bool CanSeek
        {
            get => false;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override long Length
        {
            get => throw new NotSupportedException();
        }

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }
}
