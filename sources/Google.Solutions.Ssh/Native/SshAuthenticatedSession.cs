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
        private readonly SshSession session;

        private bool disposed = false;

        private readonly uint DefaultWindowSize = (2 * 1024 * 1024);
        private readonly uint DefaultPacketSize = 32768;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshAuthenticatedSession(SshSession session)
        {
            this.session= session;
        }

        //---------------------------------------------------------------------
        // Channel.
        //---------------------------------------------------------------------

        private SshChannelHandle OpenChannelInternal(
            LIBSSH2_CHANNEL_EXTENDED_DATA mode)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSources.Default.TraceMethod().WithParameters(mode))
            {
                LIBSSH2_ERROR result;
                var channelHandle = UnsafeNativeMethods.libssh2_channel_open_ex(
                    this.session.Handle,
                    SshSessionChannelBase.Type,
                    (uint)SshSessionChannelBase.Type.Length,
                    DefaultWindowSize,
                    DefaultPacketSize,
                    null,
                    0);

                if (channelHandle.IsInvalid)
                {
                    result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_last_errno(
                        this.session.Handle);
                }
                else
                {
                    result = LIBSSH2_ERROR.NONE;
                }

                if (result != LIBSSH2_ERROR.NONE)
                {
                    throw this.session.CreateException(result);
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
                    throw this.session.CreateException(result);
                }

                return channelHandle;
            }
        }

        private void SetEnvironmentVariable(
            SshChannelHandle channelHandle,
            EnvironmentVariable environmentVariable)
        {
            Debug.Assert(environmentVariable.Name != null);
            Debug.Assert(environmentVariable.Value != null);

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
                    throw this.session.CreateException(result);
                }
                else
                {
                    //
                    // Non-required environment variable - emit a warning
                    // and carry on.
                    //

                    SshTraceSources.Default.TraceWarning(
                        "Environment variable {0} was rejected by server: {1}",
                        environmentVariable.Name,
                        result);
                }
            }
        }

        public SshShellChannel OpenShellChannel(
            LIBSSH2_CHANNEL_EXTENDED_DATA mode,
            string term,
            ushort widthInChars,
            ushort heightInChars,
            IEnumerable<EnvironmentVariable> environmentVariables = null)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();
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
                    null,   // TODO: pass modifiers?
                    0,
                    widthInChars,
                    heightInChars,
                    0,
                    0);

                if (result != LIBSSH2_ERROR.NONE)
                {
                    channelHandle.Dispose();
                    throw this.session.CreateException(result);
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
                    throw this.session.CreateException(result);
                }

                return new SshShellChannel(this.session, channelHandle);
            }
        }

        public SshExecChannel OpenExecChannel(
            string command,
            LIBSSH2_CHANNEL_EXTENDED_DATA mode)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();
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
                    throw this.session.CreateException(result);
                }

                return new SshExecChannel(this.session, channelHandle);
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
                this.session.Handle.CheckCurrentThreadOwnsHandle();
                this.disposed = true;
            }
        }

        //---------------------------------------------------------------------
        // Inner struct.
        //---------------------------------------------------------------------

        public struct EnvironmentVariable
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public bool Required { get; set; }

            public EnvironmentVariable(string name, string value, bool required)
            {
                Utilities.ThrowIfNullOrEmpty(name, nameof(name));

                this.Name = name;
                this.Value = value;
                this.Required = required;
            }
        }
    }
}
