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

using Google.Solutions.Common.Interop;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel
{
    internal static class UnsafeNativeMethods
    {
        public const uint NO_ERROR = 0;
        public const uint ERROR_INSUFFICIENT_BUFFER = 122;
        public const uint ERROR_NO_DATA = 232;

        [DllImport("iphlpapi.dll")]
        internal static extern uint GetTcpTable2(
            LocalAllocSafeHandle pTcpTable, 
            ref uint dwOutBufLen, 
            bool order);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        internal struct MIB_TCPTABLE2
        {
            internal uint dwNumEntries;

            // Followed by an anysize-array of MIB_TCPROW2.
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        internal struct MIB_TCPROW2
        {
            internal MibTcpState dwState;
            internal uint dwLocalAddr;
            internal uint dwLocalPort;
            internal uint dwRemoteAddr;
            internal uint dwRemotePort;
            internal uint dwOwningPid;
            internal uint dwOffloadState;
        }

        public enum MibTcpState : uint
        {
            MIB_TCP_STATE_CLOSED = 1,
            MIB_TCP_STATE_LISTEN = 2,
            MIB_TCP_STATE_SYN_SENT = 3,
            MIB_TCP_STATE_SYN_RCVD = 4,
            MIB_TCP_STATE_ESTAB = 5,
            MIB_TCP_STATE_FIN_WAIT1 = 6,
            MIB_TCP_STATE_FIN_WAIT2 = 7,
            MIB_TCP_STATE_CLOSE_WAIT = 8,
            MIB_TCP_STATE_CLOSING = 9,
            MIB_TCP_STATE_LAST_ACK = 10,
            MIB_TCP_STATE_TIME_WAIT = 11,
            MIB_TCP_STATE_DELETE_TCB = 12
        }
    }
}
