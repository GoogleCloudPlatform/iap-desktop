//
// Copyright 2023 Google LLC
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

using Google.Solutions.Common.Util;
using System.Diagnostics;

namespace Google.Solutions.Common.Format
{
    /// <summary>
    /// BSD checksum implementation, adapted to allow 
    /// checksums of arbitraty bit size (not just 16).
    /// </summary>
    internal static class BsdChecksum
    {
        public static uint Create(byte[] data, ushort lengthInBits)
        {
            data.ExpectNotNull(nameof(data));
            Debug.Assert(lengthInBits > 0 && lengthInBits <= 32);

            uint mask = (uint)(1 << lengthInBits) - 1;
            uint checksum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                checksum = (checksum >> 1) + ((checksum & 1) << (lengthInBits - 1));
                checksum += data[i];
                checksum &= mask;
            }

            return checksum;
        }
    }
}
