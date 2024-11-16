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
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// Represents an open file that can be read from/written to.
    /// </summary>
    internal class Libssh2SftpFileChannel : IDisposable
    {
        private readonly Libssh2Session session;
        private readonly Libssh2SftpChannelHandle channelHandle;
        private readonly Libssh2SftpFileHandle fileHandle;
        private readonly string filePath;

        private bool disposed = false;

        private SshException CreateException(LIBSSH2_ERROR error)
        {
            if (error == LIBSSH2_ERROR.SFTP_PROTOCOL)
            {
                return Libssh2SftpException.GetLastError(
                    this.channelHandle,
                    this.filePath);
            }
            else
            {
                return this.session.CreateException((LIBSSH2_ERROR)error);
            }
        }

        internal Libssh2SftpFileChannel(
            Libssh2Session session,
            Libssh2SftpChannelHandle channelHandle,
            Libssh2SftpFileHandle fileHandle,
            string filePath)
        {
            this.session = session;
            this.channelHandle = channelHandle;
            this.fileHandle = fileHandle;
            this.filePath = filePath;
        }

        public LIBSSH2_SFTP_ATTRIBUTES Attributes
        {
            get
            {
                var result = (LIBSSH2_ERROR)NativeMethods.libssh2_sftp_fstat_ex(
                    this.fileHandle,
                    out var attributes,
                    0);
                if (result != LIBSSH2_ERROR.NONE)
                {
                    throw this.session.CreateException(result);
                }

                return attributes;
            }
        }

        public uint Read(byte[] buffer)
        {
            this.fileHandle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotNull(buffer, nameof(buffer));

            using (SshTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var bytesRead = NativeMethods.libssh2_sftp_read(
                    this.fileHandle,
                    buffer,
                    new IntPtr(buffer.Length));

                if (bytesRead >= 0)
                {
                    return (uint)bytesRead;
                }
                else
                {
                    throw CreateException((LIBSSH2_ERROR)bytesRead);
                }
            }
        }

        public void Write(byte[] buffer, int length)
        {
            this.channelHandle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotNull(buffer, nameof(buffer));

            Debug.Assert(length <= buffer.Length);

            using (SshTraceSource.Log.TraceMethod().WithParameters(length))
            {
                //
                // NB. libssh2 doesn't guarantee that all data is written
                // at once. 
                //
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {

                    var totalBytesWritten = 0;
                    while (totalBytesWritten < length)
                    {
                        var bytesWritten = NativeMethods.libssh2_sftp_write(
                            this.fileHandle,
                            Marshal.UnsafeAddrOfPinnedArrayElement(buffer, totalBytesWritten),
                            new IntPtr(length - totalBytesWritten));
                        Debug.Assert(bytesWritten != 0);

                        if (bytesWritten >= 0)
                        {
                            totalBytesWritten += bytesWritten;

                            SshTraceSource.Log.TraceVerbose(
                                "SFTP wrote {0} bytes, {1} left",
                                bytesWritten,
                                length - totalBytesWritten);
                        }

                        if (bytesWritten == (int)LIBSSH2_ERROR.TIMEOUT)
                        {
                            SshTraceSource.Log.TraceVerbose(
                                "SFTP write timed out after writing {0} bytes, retrying",
                                totalBytesWritten);
                            continue;
                        }
                        else if (bytesWritten < 0)
                        {
                            SshTraceSource.Log.TraceWarning(
                                "SFTP write failed after {0} bytes",
                                totalBytesWritten);

                            throw CreateException((LIBSSH2_ERROR)bytesWritten);
                        }
                    }
                }
                finally
                {
                    handle.Free();
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

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.fileHandle.Dispose();
                this.disposed = true;
            }
        }
    }
}
