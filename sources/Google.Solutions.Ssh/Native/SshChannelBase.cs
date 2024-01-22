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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An connected and authenticated Libssh2 session.
    /// </summary>
    public abstract class SshChannelBase : IDisposable
    {
        // NB. This object does not own this handle and should not dispose it.
        protected SshSession Session { get; }

        internal SshChannelHandle ChannelHandle { get; }

        private bool closedForWriting = false;
        private bool disposed = false;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshChannelBase(
            SshSession session,
            SshChannelHandle channelHandle)
        {
            this.Session = session;
            this.ChannelHandle = channelHandle;
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
                this.ChannelHandle.CheckCurrentThreadOwnsHandle();

                using (SshTraceSource.Log.TraceMethod().WithoutParameters())
                {
                    return NativeMethods.libssh2_channel_eof(
                        this.ChannelHandle) == 1;
                }
            }
        }

        public virtual uint Read(
            byte[] buffer,
            LIBSSH2_STREAM streamId = LIBSSH2_STREAM.NORMAL)
        {
            this.ChannelHandle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotNull(buffer, nameof(buffer));

            using (SshTraceSource.Log.TraceMethod().WithParameters(streamId))
            {
                if (this.IsEndOfStream)
                {
                    // Server sent EOF, trying to read would just
                    // end up in a timeout.
                    return 0u;
                }

                if (SshEventSource.Log.IsEnabled())
                {
                    SshEventSource.Log.ChannelReadInitiated(buffer.Length);
                }

                var bytesRead = NativeMethods.libssh2_channel_read_ex(
                    this.ChannelHandle,
                    (int)streamId,
                    buffer,
                    new IntPtr(buffer.Length));

                if (SshEventSource.Log.IsEnabled())
                {
                    SshEventSource.Log.ChannelReadCompleted(bytesRead);
                }

                if (bytesRead >= 0)
                {
                    return (uint)bytesRead;
                }
                else
                {
                    throw this.Session.CreateException((LIBSSH2_ERROR)bytesRead);
                }
            }
        }

        public virtual uint Write(
            byte[] buffer,
            LIBSSH2_STREAM streamId = LIBSSH2_STREAM.NORMAL)
        {
            this.ChannelHandle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotNull(buffer, nameof(buffer));

            using (SshTraceSource.Log.TraceMethod().WithParameters(streamId))
            {
                Debug.Assert(!this.closedForWriting);

                if (SshEventSource.Log.IsEnabled())
                {
                    SshEventSource.Log.ChannelWriteInitiated(buffer.Length);
                }

                var bytesWritten = NativeMethods.libssh2_channel_write_ex(
                    this.ChannelHandle,
                    (int)streamId,
                    buffer,
                    new IntPtr(buffer.Length));

                if (SshEventSource.Log.IsEnabled())
                {
                    SshEventSource.Log.ChannelWriteCompleted(bytesWritten);
                }

                if (bytesWritten >= 0)
                {
                    return (uint)bytesWritten;
                }
                else
                {
                    throw this.Session.CreateException((LIBSSH2_ERROR)bytesWritten);
                }
            }
        }

        public void WaitForEndOfStream()
        {
            this.ChannelHandle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var result = (LIBSSH2_ERROR)NativeMethods.libssh2_channel_wait_eof(
                    this.ChannelHandle);

                if (result != LIBSSH2_ERROR.NONE)
                {
                    throw this.Session.CreateException((LIBSSH2_ERROR)result);
                }
            }
        }

        public void Close()
        {
            this.ChannelHandle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSource.Log.TraceMethod().WithoutParameters())
            {
                // Avoid closing more than once.
                if (!this.closedForWriting)
                {
                    SshEventSource.Log.ChannelCloseInitiated();

                    var result = (LIBSSH2_ERROR)NativeMethods.libssh2_channel_close(
                        this.ChannelHandle);

                    if (result == LIBSSH2_ERROR.SOCKET_SEND)
                    {
                        // Broken connection, nevermind then.
                    }
                    else if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw this.Session.CreateException((LIBSSH2_ERROR)result);
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
                this.ChannelHandle.Dispose();
                this.disposed = true;
            }
        }
    }
}
