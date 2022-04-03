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

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An connected and authenticated Libssh2 session.
    /// </summary>
    public abstract class SshChannelBase : IDisposable
    {
        internal readonly SshChannelHandle channelHandle;

        // NB. This object does not own this handle and should not dispose it.
        protected readonly SshSession session;

        private bool closedForWriting = false;
        private bool disposed = false;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshChannelBase(
            SshSession session,
            SshChannelHandle channelHandle)
        {
            this.session = session;
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
                this.channelHandle.CheckCurrentThreadOwnsHandle();

                using (SshTraceSources.Default.TraceMethod().WithoutParameters())
                {
                    return UnsafeNativeMethods.libssh2_channel_eof(
                        this.channelHandle) == 1;
                }
            }
        }

        public virtual uint Read(
            byte[] buffer,
            LIBSSH2_STREAM streamId = LIBSSH2_STREAM.NORMAL)
        {
            this.channelHandle.CheckCurrentThreadOwnsHandle();
            Utilities.ThrowIfNull(buffer, nameof(buffer));

            using (SshTraceSources.Default.TraceMethod().WithParameters(streamId))
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

                if (bytesRead >= 0)
                {
                    return (uint)bytesRead;
                }
                else
                {
                    throw this.session.CreateException((LIBSSH2_ERROR)bytesRead);
                }
            }
        }

        public virtual uint Write(
            byte[] buffer,
            LIBSSH2_STREAM streamId = LIBSSH2_STREAM.NORMAL)
        {
            this.channelHandle.CheckCurrentThreadOwnsHandle();
            Utilities.ThrowIfNull(buffer, nameof(buffer));

            using (SshTraceSources.Default.TraceMethod().WithParameters(streamId))
            {
                Debug.Assert(!this.closedForWriting);

                var bytesWritten = UnsafeNativeMethods.libssh2_channel_write_ex(
                    this.channelHandle,
                    (int)streamId,
                    buffer,
                    new IntPtr(buffer.Length));

                if (bytesWritten >= 0)
                {
                    return (uint)bytesWritten;
                }
                else
                {
                    throw this.session.CreateException((LIBSSH2_ERROR)bytesWritten);
                }
            }
        }

        public void WaitForEndOfStream()
        {
            this.channelHandle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSources.Default.TraceMethod().WithoutParameters())
            {
                var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_wait_eof(
                    this.channelHandle);

                if (result != LIBSSH2_ERROR.NONE)
                {
                    throw this.session.CreateException((LIBSSH2_ERROR)result);
                }
            }
        }

        public void Close()
        {
            this.channelHandle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSources.Default.TraceMethod().WithoutParameters())
            {
                // Avoid closing more than once.
                if (!this.closedForWriting)
                {
                    var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_close(
                        this.channelHandle);

                    if (result == LIBSSH2_ERROR.SOCKET_SEND)
                    {
                        // Broken connection, nevermind then.
                    }
                    else if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw this.session.CreateException((LIBSSH2_ERROR)result);
                    }

                    this.closedForWriting = true;
                }
            }
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
                this.channelHandle.Dispose();
                this.disposed = true;
            }
        }
    }
}
