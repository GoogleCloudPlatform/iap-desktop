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
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An connected and authenticated Libssh2 session.
    /// </summary>
    public class SshAuthenticatedSession : IDisposable
    {
        private readonly SshSessionHandle sessionHandle;
        private bool disposed = false;

        private readonly uint DefaultWindowSize = (2 * 1024 * 1024);
        private readonly uint DefaultPacketSize = 32768;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshAuthenticatedSession(SshSessionHandle sessionHandle)
        {
            this.sessionHandle = sessionHandle;
        }

        //---------------------------------------------------------------------
        // Channel.
        //---------------------------------------------------------------------

        private SshChannelHandle OpenChannelInternal(
            LIBSSH2_CHANNEL_EXTENDED_DATA mode)
        {
            this.sessionHandle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSources.Default.TraceMethod().WithParameters(mode))
            {
                LIBSSH2_ERROR result;
                var channelHandle = UnsafeNativeMethods.libssh2_channel_open_ex(
                    this.sessionHandle,
                    SshSessionChannelBase.Type,
                    (uint)SshSessionChannelBase.Type.Length,
                    DefaultWindowSize,
                    DefaultPacketSize,
                    null,
                    0);

                if (channelHandle.IsInvalid)
                {
                    result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_last_errno(
                        this.sessionHandle);
                }
                else
                {
                    result = LIBSSH2_ERROR.NONE;
                }

                if (result != LIBSSH2_ERROR.NONE)
                {
                    throw new SshNativeException(result);
                }

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
                    throw new SshNativeException(result);
                }

                return channelHandle;
            }
        }

        private static void SetEnvironmentVariable(
            SshChannelHandle channelHandle,
            string key,
            string value)
        {
            channelHandle.CheckCurrentThreadOwnsHandle();

            //
            // NB. By default, sshd only allows certain environment
            // variables to be specified. Trying to set a non-whiteisted
            // variable causes a CHANNEL_REQUEST_DENIED error.
            //
            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_setenv_ex(
                channelHandle,
                key,
                (uint)key.Length,
                value,
                (uint)value.Length);

            if (result != LIBSSH2_ERROR.NONE)
            {
                channelHandle.Dispose();
                throw new SshNativeException(result);
            }
        }

        public SshShellChannel OpenShellChannel(
            LIBSSH2_CHANNEL_EXTENDED_DATA mode,
            string term,
            ushort widthInChars,
            ushort heightInChars,
            IDictionary<string, string> environmentVariables = null)
        {
            this.sessionHandle.CheckCurrentThreadOwnsHandle();
            Utilities.ThrowIfNull(term, nameof(term));

            using (SshTraceSources.Default.TraceMethod().WithParameters(
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
                            environmentVariable.Key,
                            environmentVariable.Value);
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
                    null,   // TODO: pass modifiers?
                    0,
                    widthInChars,
                    heightInChars,
                    0,
                    0);

                if (result != LIBSSH2_ERROR.NONE)
                {
                    channelHandle.Dispose();
                    throw new SshNativeException(result);
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
                    throw new SshNativeException(result);
                }

                return new SshShellChannel(channelHandle);
            }
        }

        public SshExecChannel OpenExecChannel(
            string command,
            LIBSSH2_CHANNEL_EXTENDED_DATA mode)
        {
            this.sessionHandle.CheckCurrentThreadOwnsHandle();
            Utilities.ThrowIfNull(command, nameof(command));

            using (SshTraceSources.Default.TraceMethod().WithParameters(
                command,
                mode))
            {
                var channelHandle = OpenChannelInternal(mode);

                //
                // Launch the process.
                //
                var request = "exec";
                var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_process_startup(
                    channelHandle,
                    request,
                    (uint)request.Length,
                    command,
                    command == null ? 0 : (uint)command.Length);

                if (result != LIBSSH2_ERROR.NONE)
                {
                    channelHandle.Dispose();
                    throw new SshNativeException(result);
                }

                return new SshExecChannel(channelHandle);
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
                this.sessionHandle.CheckCurrentThreadOwnsHandle();
                this.disposed = true;
            }
        }
    }
}
