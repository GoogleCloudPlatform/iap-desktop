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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An connected and authenticated Libssh2 session.
    /// </summary>
    public class SshAuthenticatedSession : IDisposable
    {
        // NB. This object does not own this handle and should not dispose it.
        internal SshSession Session { get; }

        private bool disposed = false;

        private readonly uint DefaultWindowSize = (2 * 1024 * 1024);
        private readonly uint DefaultPacketSize = 32768;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshAuthenticatedSession(SshSession session)
        {
            this.Session = session;
        }

        //---------------------------------------------------------------------
        // Channel.
        //---------------------------------------------------------------------

        private SshChannelHandle OpenChannelInternal(
            LIBSSH2_CHANNEL_EXTENDED_DATA mode)
        {
            this.Session.Handle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSource.Log.TraceMethod().WithParameters(mode))
            {
                LIBSSH2_ERROR result;
                var channelHandle = UnsafeNativeMethods.libssh2_channel_open_ex(
                    this.Session.Handle,
                    SshSessionChannelBase.Type,
                    (uint)SshSessionChannelBase.Type.Length,
                    this.DefaultWindowSize,
                    this.DefaultPacketSize,
                    null,
                    0);

                channelHandle.ValidateAndAttachToSession(this.Session);

                //
                // Configure how extended data (stderr, in particular) should
                // be handled.
                //
                result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_handle_extended_data2(
                    channelHandle,
                    mode);
                if (result != LIBSSH2_ERROR.NONE)
                {
                    channelHandle.Dispose();
                    throw this.Session.CreateException(result);
                }

                return channelHandle;
            }
        }

        private void SetEnvironmentVariable(
            SshChannelHandle channelHandle,
            EnvironmentVariable environmentVariable)
        {
            channelHandle.CheckCurrentThreadOwnsHandle();

            //
            // NB. By default, sshd only allows certain environment
            // variables to be specified. Trying to set a non-whiteisted
            // variable causes a CHANNEL_REQUEST_DENIED error.
            //
            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_setenv_ex(
                channelHandle,
                environmentVariable.Name,
                (uint)environmentVariable.Name.Length,
                environmentVariable.Value,
                (uint)environmentVariable.Value.Length);

            if (result != LIBSSH2_ERROR.NONE)
            {
                if (environmentVariable.Required)
                {
                    channelHandle.Dispose();
                    throw this.Session.CreateException(result);
                }
                else
                {
                    //
                    // Non-required environment variable - emit a warning
                    // and carry on.
                    //

                    SshTraceSource.Log.TraceWarning(
                        "Environment variable {0} was rejected by server: {1}",
                        environmentVariable.Name,
                        result);
                }
            }
        }

        /// <summary>
        /// Start an interactive shell.
        /// </summary>
        public SshShellChannel OpenShellChannel(
            LIBSSH2_CHANNEL_EXTENDED_DATA mode,
            string term,
            ushort widthInChars,
            ushort heightInChars,
            IEnumerable<EnvironmentVariable>? environmentVariables = null)
        {
            this.Session.Handle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotNull(term, nameof(term));

            using (SshTraceSource.Log.TraceMethod().WithParameters(
                term,
                widthInChars,
                heightInChars))
            {
                var channelHandle = OpenChannelInternal(mode);

                if (environmentVariables != null)
                {
                    foreach (var environmentVariable in environmentVariables
                        .Where(i => !string.IsNullOrEmpty(i.Value)))
                    {
                        SetEnvironmentVariable(
                            channelHandle,
                            environmentVariable);
                    }
                }

                //
                // Request a pseudoterminal. This must be done before the shell
                // is launched.
                //
                var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_request_pty_ex(
                    channelHandle,
                    term,
                    (uint)term.Length,
                    null,
                    0,
                    widthInChars,
                    heightInChars,
                    0,
                    0);

                if (result != LIBSSH2_ERROR.NONE)
                {
                    channelHandle.Dispose();
                    throw this.Session.CreateException(result);
                }

                //
                // Launch the shell.
                //
                var request = "shell";
                result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_process_startup(
                    channelHandle,
                    request,
                    (uint)request.Length,
                    null,
                    0);

                if (result != LIBSSH2_ERROR.NONE)
                {
                    channelHandle.Dispose();
                    throw this.Session.CreateException(result);
                }

                SshEventSource.Log.ShellChannelOpened(term);

                return new SshShellChannel(this.Session, channelHandle);
            }
        }

        /// <summary>
        /// Open a channel for SFTP operations.
        /// </summary>
        public SshSftpChannel OpenSftpChannel()
        {
            using (SshTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var channelHandle = UnsafeNativeMethods.libssh2_sftp_init(
                    this.Session.Handle);

                channelHandle.ValidateAndAttachToSession(this.Session);

                SshEventSource.Log.SftpChannelOpened();

                return new SshSftpChannel(this.Session, channelHandle);
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
                this.Session.Handle.CheckCurrentThreadOwnsHandle();
                this.disposed = true;
            }
        }
    }
}
