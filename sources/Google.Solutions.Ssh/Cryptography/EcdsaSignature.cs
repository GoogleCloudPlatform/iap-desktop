//
// Copyright 2021 Google LLC
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

using Google.Solutions.Ssh.Format;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Google.Solutions.Ssh.Cryptography
{
    internal readonly struct ECDsaSignature
    {
        private readonly byte[] R;
        private readonly byte[] S;

        public ECDsaSignature(byte[] r, byte[] s)
        {
            Debug.Assert(r.Length == s.Length);

            this.R = r;
            this.S = s;
        }

        /// <summary>
        /// Parse IEEE-1363 formatted signature.
        /// </summary>
        public static ECDsaSignature FromIeee1363(byte[] signature)
        {
            // Input is (r, s), each of them exactly half of the array.
            Debug.Assert(signature.Length % 2 == 0);
            Debug.Assert(signature.Length > 1);
            var halfLength = signature.Length / 2;

            return new ECDsaSignature(
                signature.Take(halfLength).ToArray(),
                signature.Skip(halfLength).ToArray());
        }

        /// <summary>
        /// Format signature according to RFC5656 section 3.1.2.
        /// </summary>
        /// <returns></returns>
        public byte[] ToSshBlob()
        {
            using (var buffer = new MemoryStream())
            using (var writer = new SshWriter(buffer))
            {
                writer.WriteMultiPrecisionInteger(this.R);
                writer.WriteMultiPrecisionInteger(this.S);
                writer.Flush();

                return buffer.ToArray();
            }
        }
    }
}
