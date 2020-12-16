//
// Copyright 2020 Google LLC
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

using Google.Apis.Util;
using Google.Solutions.Common.Diagnostics;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An connected and authenticated Libssh2 session.
    /// </summary>
    public abstract class SshChannelBase : IDisposable
    {
        internal readonly SshChannelHandle channelHandle;

        private bool closedForWriting = false;
        private bool disposed = false;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshChannelBase(SshChannelHandle channelHandle)
        {
            this.channelHandle = channelHandle;
        }

        //---------------------------------------------------------------------
        // I/O.
        //---------------------------------------------------------------------

        /// <summary>
        /// Indicates whether the server has sent an EOF.
        /// </summary>
        public bool IsEndOfStream
        {
            get
            {
                using (SshTraceSources.Default.TraceMethod().WithoutParameters())
                {
                    return UnsafeNativeMethods.libssh2_channel_eof(
                        this.channelHandle) == 1;
                }
            }
        }

        /// <summary>
        /// Flush buffer containing unread data.
        /// </summary>
        public uint Flush(LIBSSH2_STREAM streamId)
        {
            using (SshTraceSources.Default.TraceMethod().WithParameters(streamId))
            {
                // TODO: Remove lock?
                lock (this.channelHandle.SyncRoot)
                {
                    Debug.Assert(!this.closedForWriting);

                    var bytesFlushed = UnsafeNativeMethods.libssh2_channel_flush_ex(
                        this.channelHandle,
                        (int)streamId);

                    if (bytesFlushed < 0)
                    {
                        throw new SshNativeException((LIBSSH2_ERROR)bytesFlushed);
                    }
                    else
                    {
                        return (uint)bytesFlushed;
                    }
                }
            }
        }

        public uint Flush()
            => Flush(LIBSSH2_STREAM.NORMAL);

        public Task<uint> ReadAsync(
            LIBSSH2_STREAM streamId,
            byte[] buffer)
        {
            Utilities.ThrowIfNull(buffer, nameof(buffer));

            return Task.Run(() =>
            {
                using (SshTraceSources.Default.TraceMethod().WithParameters(streamId))
                {
                    // TODO: Remove lock?
                    lock (this.channelHandle.SyncRoot)
                    {
                        if (this.IsEndOfStream)
                        {
                            // Server sent EOF, trying to read would just
                            // end up in a timeout.
                            return 0u;
                        }

                        var bytesRead = UnsafeNativeMethods.libssh2_channel_read_ex(
                            this.channelHandle,
                            (int)streamId,
                            buffer,
                            new IntPtr(buffer.Length));

                        if (bytesRead == (int)LIBSSH2_ERROR.TIMEOUT)
                        {
                            throw new TimeoutException("Read operation timed out");
                        }
                        else if (bytesRead < 0)
                        {
                            throw new SshNativeException((LIBSSH2_ERROR)bytesRead);
                        }
                        else
                        {
                            return (uint)bytesRead;
                        }
                    }
                }
            });
        }

        public Task<uint> ReadAsync(byte[] buffer)
            => ReadAsync(LIBSSH2_STREAM.NORMAL, buffer);

        public Task<uint> WriteAsync(
            LIBSSH2_STREAM streamId,
            byte[] buffer)
        {
            Utilities.ThrowIfNull(buffer, nameof(buffer));

            return Task.Run(() =>
            {
                using (SshTraceSources.Default.TraceMethod().WithParameters(streamId))
                {
                    // TODO: Remove lock?
                    lock (this.channelHandle.SyncRoot)
                    {
                        Debug.Assert(!this.closedForWriting);

                        var bytesWritten = UnsafeNativeMethods.libssh2_channel_write_ex(
                            this.channelHandle,
                            (int)streamId,
                            buffer,
                            new IntPtr(buffer.Length));

                        if (bytesWritten == (int)LIBSSH2_ERROR.TIMEOUT)
                        {
                            throw new TimeoutException("Read operation timed out");
                        }
                        else if (bytesWritten < 0)
                        {
                            throw new SshNativeException((LIBSSH2_ERROR)bytesWritten);
                        }
                        else
                        {
                            return (uint)bytesWritten;
                        }
                    }
                }
            });
        }

        public Task<uint> WriteAsync(byte[] buffer)
            => WriteAsync(LIBSSH2_STREAM.NORMAL, buffer);

        public Task CloseAsync()
        {
            if (this.closedForWriting)
            {
                return Task.CompletedTask;
            }

            return Task.Run(() =>
            {
                using (SshTraceSources.Default.TraceMethod().WithoutParameters())
                {
                    // TODO: Remove lock?
                    lock (this.channelHandle.SyncRoot)
                    {
                        // Avoid closing more than once.
                        if (!this.closedForWriting)
                        {
                            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_close(
                                this.channelHandle);
                            Debug.Assert(result == LIBSSH2_ERROR.NONE);

                            this.closedForWriting = true;
                        }
                    }
                }
            });
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                Debug.Assert(this.closedForWriting);
                this.channelHandle.Dispose();
                this.disposed = true;
            }
        }
    }
}
