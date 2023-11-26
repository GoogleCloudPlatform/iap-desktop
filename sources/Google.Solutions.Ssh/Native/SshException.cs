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

using System;
using System.Diagnostics;

namespace Google.Solutions.Ssh.Native
{
    public class SshNativeException : SshException
    {
        public LIBSSH2_ERROR ErrorCode { get; }

        internal SshNativeException(
            LIBSSH2_ERROR code,
            string errorMessage)
            : base(errorMessage)
        {
            Debug.Assert(code != LIBSSH2_ERROR.NONE);

            this.ErrorCode = code;
        }
    }

    public class SshSftpNativeException : SshException
    {
        public LIBSSH2_FX_ERROR ErrorCode { get; }

        private SshSftpNativeException(
            LIBSSH2_FX_ERROR errorCode,
            string message)
            : base(message)
        {
            this.ErrorCode = errorCode;
        }

        internal static SshSftpNativeException GetLastError(
            SshSftpChannelHandle channelHandle,
            string path)
        {
            var errno = (LIBSSH2_FX_ERROR)
                NativeMethods.libssh2_sftp_last_error(channelHandle);

            return new SshSftpNativeException(
                errno,
                $"{path}: SFTP operation failed: {errno}");
        }
    }

    //-------------------------------------------------------------------------
    // Error codes.
    //-------------------------------------------------------------------------

    public enum LIBSSH2_ERROR : Int32
    {
        NONE = 0,
        SOCKET_NONE = -1,
        BANNER_RECV = -2,
        BANNER_SEND = -3,
        INVALID_MAC = -4,
        KEX_FAILURE = -5,
        ALLOC = -6,
        SOCKET_SEND = -7,
        KEY_EXCHANGE_FAILURE = -8,
        TIMEOUT = -9,
        HOSTKEY_INIT = -10,
        HOSTKEY_SIGN = -11,
        DECRYPT = -12,
        SOCKET_DISCONNECT = -13,
        PROTO = -14,
        PASSWORD_EXPIRED = -15,
        FILE = -16,
        METHOD_NONE = -17,
        AUTHENTICATION_FAILED = -18,
        PUBLICKEY_UNRECOGNIZED = -18,
        PUBLICKEY_UNVERIFIED = -19,
        CHANNEL_OUTOFORDER = -20,
        CHANNEL_FAILURE = -21,
        CHANNEL_REQUEST_DENIED = -22,
        CHANNEL_UNKNOWN = -23,
        CHANNEL_WINDOW_EXCEEDED = -24,
        CHANNEL_PACKET_EXCEEDED = -25,
        CHANNEL_CLOSED = -26,
        CHANNEL_EOF_SENT = -27,
        SCP_PROTOCOL = -28,
        ZLIB = -29,
        SOCKET_TIMEOUT = -30,
        SFTP_PROTOCOL = -31,
        REQUEST_DENIED = -32,
        METHOD_NOT_SUPPORTED = -33,
        INVAL = -34,
        INVALID_POLL_TYPE = -35,
        PUBLICKEY_PROTOCOL = -36,
        EAGAIN = -37,
        BUFFER_TOO_SMALL = -38,
        BAD_USE = -39,
        COMPRESS = -40,
        OUT_OF_BOUNDARY = -41,
        AGENT_PROTOCOL = -42,
        SOCKET_RECV = -43,
        ENCRYPT = -44,
        BAD_SOCKET = -45,
        KNOWN_HOSTS = -46,
        CHANNEL_WINDOW_FULL = -47,
        KEYFILE_AUTH_FAILED = -48,
        ALGO_UNSUPPORTED =-51
    }

    public enum LIBSSH2_FX_ERROR
    {
        OK = 0,
        EOF = 1,
        NO_SUCH_FILE = 2,
        PERMISSION_DENIED = 3,
        FAILURE = 4,
        BAD_MESSAGE = 5,
        NO_CONNECTION = 6,
        CONNECTION_LOST = 7,
        OP_UNSUPPORTED = 8,
    }
}
