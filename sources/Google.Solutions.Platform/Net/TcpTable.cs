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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Google.Solutions.Platform.Net
{
    public static class TcpTable
    {
        public struct Entry
        {
            public MibTcpState State;
            public IPEndPoint LocalEndpoint;
            public IPEndPoint RemoteEndpoint;
            public uint ProcessId;
        }

        private static IEnumerable<UnsafeNativeMethods.MIB_TCPROW2> GetRawTcpTable2()
        {
            while (true)
            {
                //
                // Get buffer length.
                //
                var bufferSize = 0u;
                var error = UnsafeNativeMethods.GetTcpTable2(
                    LocalAllocSafeHandle.Zero,
                    ref bufferSize,
                    false);

                switch (error)
                {
                    case UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER:
                        using (var buffer = LocalAllocSafeHandle.LocalAlloc(bufferSize))
                        {
                            if (UnsafeNativeMethods.GetTcpTable2(
                                buffer,
                                ref bufferSize,
                                false) == UnsafeNativeMethods.NO_ERROR)
                            {
                                // Got the data.
                                var ptr = buffer.DangerousGetHandle();
                                var tcpTable = (UnsafeNativeMethods.MIB_TCPTABLE2)Marshal.PtrToStructure(
                                    ptr,
                                    typeof(UnsafeNativeMethods.MIB_TCPTABLE2));
                                var list = new List<UnsafeNativeMethods.MIB_TCPROW2>();

                                if (tcpTable.dwNumEntries > 0)
                                {
                                    // Move pointer to the beginning of the anysize array.
                                    ptr = (IntPtr)((long)ptr + Marshal.SizeOf(tcpTable.dwNumEntries));

                                    // Read array entries, one by one.
                                    for (var i = 0; i < tcpTable.dwNumEntries; i++)
                                    {
                                        var row = (UnsafeNativeMethods.MIB_TCPROW2)Marshal.PtrToStructure(
                                            ptr,
                                            typeof(UnsafeNativeMethods.MIB_TCPROW2));

                                        list.Add(row);
                                        ptr = (IntPtr)((long)ptr + Marshal.SizeOf(row));
                                    }
                                }

                                return list;
                            }
                            else
                            {
                                // Buffer size not correct anymore, table must have
                                // changed in the mean time.
                                break;
                            }
                        }

                    case UnsafeNativeMethods.NO_ERROR:
                    case UnsafeNativeMethods.ERROR_NO_DATA:
                        return Enumerable.Empty<UnsafeNativeMethods.MIB_TCPROW2>();

                    default:
                        throw new Win32Exception(
                            (int)error,
                            "Failed to query TCP table from GetTcpTable2");
                }
            }
        }

        private static uint ConvertPort(uint raw)
        {
            // The upper 16 bits may contain uninitialized data.
            // The lower 16 bot contain the port in network byte order.
            return ((raw & 0x0000FF00) >> 8) | ((raw & 0x000000FF) << 8);
        }

        public static IEnumerable<Entry> GetTcpTable2()
        {
            return GetRawTcpTable2()
                .Select(row => new Entry()
                {
                    State = row.dwState,
                    LocalEndpoint = new IPEndPoint(
                        new IPAddress(row.dwLocalAddr),
                        (int)ConvertPort(row.dwLocalPort)),
                    RemoteEndpoint = new IPEndPoint(
                        new IPAddress(row.dwRemoteAddr),
                        (int)ConvertPort(row.dwRemotePort)),
                    ProcessId = row.dwOwningPid
                });
        }

        //---------------------------------------------------------------------
        // P/Invoke definitions.
        //---------------------------------------------------------------------

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

        private static class UnsafeNativeMethods
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
        }
    }
}
