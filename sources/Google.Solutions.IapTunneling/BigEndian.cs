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

using System;

namespace Google.Solutions.IapTunneling
{
    /// <summary>
    /// BigEndian de/encoding helper methods.
    /// </summary>
    internal static class BigEndian
    {
        public static UInt16 DecodeUInt16(byte[] buffer, int offset)
        {
            return (UInt16)((int)buffer[offset] << 8 | (int)buffer[offset + 1]);
        }

        public static void EncodeUInt16(UInt16 value, byte[] buffer, int offset)
        {
            buffer[offset] = (byte)(value >> 8);
            buffer[offset + 1] = (byte)(value);
        }

        public static UInt32 DecodeUInt32(byte[] buffer, int offset)
        {
            return (UInt32)(
                (UInt32)buffer[offset + 0] << 24 |
                (UInt32)buffer[offset + 1] << 16 |
                (UInt32)buffer[offset + 2] << 8 |
                (UInt32)buffer[offset + 3]);
        }

        public static void EncodeUInt32(UInt32 value, byte[] buffer, int offset)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)(value);
        }

        public static UInt64 DecodeUInt64(byte[] buffer, int offset)
        {
            return (UInt64)(
                (UInt64)buffer[offset + 0] << 56 |
                (UInt64)buffer[offset + 1] << 48 |
                (UInt64)buffer[offset + 2] << 40 |
                (UInt64)buffer[offset + 3] << 32 |
                (UInt64)buffer[offset + 4] << 24 |
                (UInt64)buffer[offset + 5] << 16 |
                (UInt64)buffer[offset + 6] << 8 |
                (UInt64)buffer[offset + 7]);
        }

        public static void EncodeUInt64(UInt64 value, byte[] buffer, int offset)
        {
            buffer[offset] = (byte)(value >> 56);
            buffer[offset + 1] = (byte)(value >> 48);
            buffer[offset + 2] = (byte)(value >> 40);
            buffer[offset + 3] = (byte)(value >> 32);
            buffer[offset + 4] = (byte)(value >> 24);
            buffer[offset + 5] = (byte)(value >> 16);
            buffer[offset + 6] = (byte)(value >> 8);
            buffer[offset + 7] = (byte)(value);
        }
    }
}
