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
using System;
using System.Runtime.InteropServices;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An interactive SSH channel.
    /// </summary>
    public abstract class SshSessionChannelBase : SshChannelBase
    {
        public const string Type = "session";

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshSessionChannelBase(
            SshSession session,
            SshChannelHandle channelHandle)
            : base(session, channelHandle)
        {
        }

        public int ExitCode
        {
            get
            {
                this.ChannelHandle.CheckCurrentThreadOwnsHandle();

                using (SshTraceSource.Log.TraceMethod().WithoutParameters())
                {
                    // 
                    // NB. This call does not cause network traffic and therefore
                    // should not block.
                    //
                    return NativeMethods.libssh2_channel_get_exit_status(
                        this.ChannelHandle);
                }
            }
        }

        public string? ExitSignal
        {
            get
            {
                this.ChannelHandle.CheckCurrentThreadOwnsHandle();

                using (SshTraceSource.Log.TraceMethod().WithoutParameters())
                {
                    // 
                    // NB. This call does not cause network traffic and therefore
                    // should not block.
                    //

                    var result = (LIBSSH2_ERROR)NativeMethods.libssh2_channel_get_exit_signal(
                        this.ChannelHandle,
                        out var signalPtr,
                        out var signalLength,
                        out var errmsgPtr,
                        out var errmsgLength,
                        out var langTagPtr,
                        out var langTagLength);
                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw this.Session.CreateException(result);
                    }

                    //
                    // NB. Currently, libssh2 only populates the signal
                    // parameter, errmsg and langtag are always NULL. 
                    //
                    try
                    {
                        if (signalPtr != IntPtr.Zero)
                        {
                            return Marshal.PtrToStringAnsi(signalPtr, signalLength.ToInt32());
                        }
                        else
                        {
                            return null;
                        }
                    }
                    finally
                    {
                        SshSession.Free(signalPtr, IntPtr.Zero);
                        SshSession.Free(errmsgPtr, IntPtr.Zero);
                        SshSession.Free(langTagPtr, IntPtr.Zero);
                    }
                }
            }
        }
    }

    public class SshShellChannel : SshSessionChannelBase
    {
        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshShellChannel(
            SshSession session,
            SshChannelHandle channelHandle)
            : base(session, channelHandle)
        {
        }

        public void ResizePseudoTerminal(
            ushort widthInChars,
            ushort heightInChars)
        {
            this.ChannelHandle.CheckCurrentThreadOwnsHandle();

            var result = (LIBSSH2_ERROR)NativeMethods.libssh2_channel_request_pty_size_ex(
                this.ChannelHandle,
                widthInChars,
                heightInChars,
                0,
                0);

            if (result != LIBSSH2_ERROR.NONE)
            {
                throw this.Session.CreateException(result);
            }
        }
    }
}
