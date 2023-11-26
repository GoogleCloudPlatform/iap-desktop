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
using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// SFTP channel.
    /// </summary>
    public class SshSftpChannel : IDisposable
    {
        private const uint MaxFilenameLength = 256;

        private readonly SshSession session;
        private readonly SshSftpChannelHandle channelHandle;

        private bool disposed = false;

        internal SshSftpChannel(
            SshSession session,
            SshSftpChannelHandle channelHandle)
        {
            this.session = session;
            this.channelHandle = channelHandle;
        }

        //---------------------------------------------------------------------

        public IReadOnlyCollection<SshSftpFileInfo> ListFiles(string path)
        {
            this.channelHandle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotEmpty(path, nameof(path));

            var files = new LinkedList<SshSftpFileInfo>();

            using (SshTraceSource.Log.TraceMethod().WithParameters(path))
            using (var dirHandle = NativeMethods.libssh2_sftp_open_ex(
                this.channelHandle,
                path,
                (uint)path.Length,
                0,
                0,
                LIBSSH2_OPENTYPE.OPENDIR))
            {
                try
                {
                    dirHandle.ValidateAndAttachToSession(this.session);

                    //
                    // NB. longEntry is a human-readable listing as produced by `ls`
                    // and isn't useful for us.
                    //
                    using (var fileNameBuffer = GlobalAllocSafeHandle.GlobalAlloc(MaxFilenameLength))
                    using (var longEntryBuffer = GlobalAllocSafeHandle.GlobalAlloc(MaxFilenameLength))
                    {
                        while (true)
                        {
                            var bytesInBuffer = NativeMethods.libssh2_sftp_readdir_ex(
                                dirHandle,
                                fileNameBuffer.DangerousGetHandle(),
                                new IntPtr(MaxFilenameLength),
                                longEntryBuffer.DangerousGetHandle(),
                                new IntPtr(MaxFilenameLength),
                                out var attributes);
                            if (bytesInBuffer == 0)
                            {
                                //
                                // End of list reached.
                                //
                                return files;
                            }
                            else if (bytesInBuffer < 0)
                            {
                                throw this.session.CreateException((LIBSSH2_ERROR)bytesInBuffer);
                            }
                            else
                            {
                                files.AddLast(new SshSftpFileInfo(
                                    Marshal.PtrToStringAnsi(fileNameBuffer.DangerousGetHandle()),
                                    attributes));
                            }
                        }
                    }
                }
                catch (SshNativeException e) when (e.ErrorCode == LIBSSH2_ERROR.SFTP_PROTOCOL)
                {
                    throw SshSftpNativeException.GetLastError(
                        this.channelHandle,
                        path);
                }
            }
        }

        public void CreateDirectory(
            string path,
            FilePermissions filePermissions)
        {
            this.channelHandle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotEmpty(path, nameof(path));

            using (SshTraceSource.Log.TraceMethod().WithParameters(path))
            {
                try
                {
                    var result = (LIBSSH2_ERROR)NativeMethods.libssh2_sftp_mkdir_ex(
                        this.channelHandle,
                        path,
                        (uint)path.Length,
                        filePermissions);

                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw this.session.CreateException(result);
                    }
                }
                catch (SshNativeException e) when (e.ErrorCode == LIBSSH2_ERROR.SFTP_PROTOCOL)
                {
                    throw SshSftpNativeException.GetLastError(
                        this.channelHandle,
                        path);
                }
            }
        }

        public void DeleteDirectory(string path)
        {
            this.channelHandle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotEmpty(path, nameof(path));

            using (SshTraceSource.Log.TraceMethod().WithParameters(path))
            {
                try
                {
                    var result = (LIBSSH2_ERROR)NativeMethods.libssh2_sftp_rmdir_ex(
                        this.channelHandle,
                        path,
                        (uint)path.Length);

                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw this.session.CreateException(result);
                    }
                }
                catch (SshNativeException e) when (e.ErrorCode == LIBSSH2_ERROR.SFTP_PROTOCOL)
                {
                    throw SshSftpNativeException.GetLastError(
                        this.channelHandle,
                        path);
                }
            }
        }

        public SshSftpFileChannel CreateFile(
            string path,
            LIBSSH2_FXF_FLAGS flags,
            FilePermissions mode)
        {
            this.channelHandle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotEmpty(path, nameof(path));

            using (SshTraceSource.Log.TraceMethod()
                .WithParameters(path, flags, mode))
            {
                try
                {
                    var fileHandle = NativeMethods.libssh2_sftp_open_ex(
                        this.channelHandle,
                        path,
                        (uint)path.Length,
                        flags,
                        mode,
                        LIBSSH2_OPENTYPE.OPENFILE);

                    fileHandle.ValidateAndAttachToSession(this.session);

                    return new SshSftpFileChannel(
                        this.session,
                        this.channelHandle,
                        fileHandle,
                        path);
                }
                catch (SshNativeException e) when (e.ErrorCode == LIBSSH2_ERROR.SFTP_PROTOCOL)
                {
                    throw SshSftpNativeException.GetLastError(
                        this.channelHandle,
                        path);
                }
            }
        }

        public void DeleteFile(string path)
        {
            this.channelHandle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotEmpty(path, nameof(path));

            using (SshTraceSource.Log.TraceMethod().WithParameters(path))
            {
                try
                {
                    var result = (LIBSSH2_ERROR)NativeMethods.libssh2_sftp_unlink_ex(
                        this.channelHandle,
                        path,
                        (uint)path.Length);

                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw this.session.CreateException(result);
                    }
                }
                catch (SshNativeException e) when (e.ErrorCode == LIBSSH2_ERROR.SFTP_PROTOCOL)
                {
                    throw SshSftpNativeException.GetLastError(
                        this.channelHandle,
                        path);
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
                this.channelHandle.Dispose();
                this.disposed = true;
            }
        }
    }

    public struct SshSftpFileInfo
    {
        private readonly LIBSSH2_SFTP_ATTRIBUTES attributes;

        /// <summary>
        /// Name of file (without path).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// File attributes.
        /// </summary>

        public FilePermissions Permissions
            => (FilePermissions)this.attributes.permissions;

        public bool IsDirectory
            => this.Permissions.IsDirectory();

        public uint UserId
            => this.attributes.uid;

        public uint GroupId
            => this.attributes.gid;

        public DateTime LastAccessDate
            => DateTimeOffset.FromUnixTimeSeconds(this.attributes.atime).DateTime;

        public DateTime LastModifiedDate
            => DateTimeOffset.FromUnixTimeSeconds(this.attributes.mtime).DateTime;

        public ulong Size
            => this.attributes.filesize;

        internal SshSftpFileInfo(
            string name,
            LIBSSH2_SFTP_ATTRIBUTES attributes)
        {
            this.Name = name;
            this.attributes = attributes;
        }
    }
}
