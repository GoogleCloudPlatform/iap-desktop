//
// Copyright 2019 Google LLC
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

namespace Google.Solutions.IapTunneling.Iap
{
    /// <summary>
    /// Message tags used by SSH Relay v4.
    /// </summary>
    internal enum SshRelayMessageTag : ushort
    {
        UNUSED_0 = 0,
        CONNECT_SUCCESS_SID = 1,
        RECONNECT_SUCCESS_ACK = 2,
        DEPRECATED = 3,
        DATA = 4,
        UNUSED_5 = 5,
        UNUSED_6 = 6,
        ACK = 7,
        UNUSED_8 = 8,
        LONG_CLOSE = 10
    };

    /// <summary>
    /// Connection close codes used by SSH Relay v4. This is an
    /// extension of the close codes defined in RFC 6455.
    /// </summary>
    public enum SshRelayCloseCode : uint
    {
        NORMAL = 1000,
        ERROR_UNKNOWN = 4000,
        SID_UNKNOWN = 4001,
        SID_IN_USE = 4002,
        FAILED_TO_CONNECT_TO_BACKEND = 4003,
        REAUTHENTICATION_REQUIRED = 4004,
        BAD_ACK = 4005,
        INVALID_ACK = 4006,
        INVALID_WEBSOCKET_OPCODE = 4007,
        INVALID_TAG = 4008,
        DESTINATION_WRITE_FAILED = 4009,
        DESTINATION_READ_FAILED = 4010,

        INVALID_DATA = 4013,
        NOT_AUTHORIZED = 4033,
        LOOKUP_FAILED = 4047,
        LOOKUP_FAILED_RECONNECT = 4051
    }
}

