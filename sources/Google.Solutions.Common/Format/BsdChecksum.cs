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
    /// checksums of arbitrary bit size (not just 16).
    /// </summary>
    public class BsdChecksum
    {
        private readonly ushort lengthInBits;
        private uint checksum = 0;

        public BsdChecksum(ushort lengthInBits)
        {
            Precondition.Expect(
                lengthInBits > 0 && lengthInBits <= 32,
                "Invalid checksum length");

            this.lengthInBits = lengthInBits;
        }

        /// <summary>
        /// Get the checksum so far.
        /// </summary>
        public uint Value
        {
            get
            {
                return this.checksum;
            }
        }

        /// <summary>
        /// Incorporate data into the checksum.
        /// </summary>
        public void Add(byte[] data)
        {
            data.ExpectNotNull(nameof(data));

            var mask = (uint)(1 << this.lengthInBits) - 1;
            for (var i = 0; i < data.Length; i++)
            {
                this.checksum = (this.checksum >> 1) + 
                    ((this.checksum & 1) << (this.lengthInBits - 1));
                this.checksum += data[i];
                this.checksum &= mask;
            }
        }
    }
}
