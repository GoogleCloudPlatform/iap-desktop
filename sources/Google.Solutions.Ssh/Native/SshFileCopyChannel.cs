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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// SCP file uploda/download channel.
    /// </summary>
    public abstract class SshFileCopyChannel : SshChannelBase
    {
        internal SshFileCopyChannel(
            SshSession session,
            SshChannelHandle channelHandle)
            : base(session, channelHandle)
        {
            // TODO: Remove session as it's contained in channel handle
        }
    }

    /// <summary>
    /// SCP upload channel.
    /// </summary>
    public class SshFileUploadChannel : SshFileCopyChannel
    {
        internal SshFileUploadChannel(
            SshSession session,
            SshChannelHandle channelHandle)
            : base(session, channelHandle)
        {
        }

        public override uint Read(
            byte[] buffer, 
            LIBSSH2_STREAM streamId = LIBSSH2_STREAM.NORMAL)
        {
            throw new InvalidOperationException(
                "Reading from an upload channel is not supported");
        }
    }

    /// <summary>
    /// SCP download channel.
    /// </summary>
    public class SshFileDownloadChannel : SshFileCopyChannel
    {
        private readonly LIBSSH2_STAT fileStat;

        public uint FileSize => this.fileStat.st_size; // TODO: 64 bit?

        internal SshFileDownloadChannel(
            SshSession session,
            SshChannelHandle channelHandle,
            LIBSSH2_STAT fileStat)
            : base(session, channelHandle)
        {
            this.fileStat = fileStat;
        }

        public override uint Write(byte[] buffer, LIBSSH2_STREAM streamId = LIBSSH2_STREAM.NORMAL)
        {
            throw new InvalidOperationException(
                "Writing to a download channel is not supported");
        }
    }
}
