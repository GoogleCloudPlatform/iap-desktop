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
using Google.Solutions.Ssh.Format;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Cryptography
{
    /// <summary>
    /// An ECDsa public key.
    /// </summary>
    public class EcdsaPublicKey : PublicKey
    {
        private readonly ECDsaCng key;

        public EcdsaPublicKey(ECDsaCng key)
        {
            this.key = key.ExpectNotNull(nameof(key));
        }

        //---------------------------------------------------------------------
        // Factory methods.
        //---------------------------------------------------------------------

        /// <summary>
        /// Read a key from its wire format (i.e., RFC5656 section 3.1).
        /// </summary>
        public static EcdsaPublicKey FromWireFormat(byte[] encodedKey)
        {
            encodedKey.ExpectNotNull(nameof(encodedKey));

            using (var reader = new SshReader(new MemoryStream(encodedKey)))
            {
                throw new NotImplementedException();
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string Type
        {
            get => "ecdsa-sha2-nistp" + this.key.KeySize;
        }

        public override byte[] WireFormatValue
        {
            get
            {
                using (var buffer = new MemoryStream())
                using (var writer = new SshWriter(buffer))
                {
                    //
                    // Encode public key according to RFC5656 section 3.1.
                    //

                    writer.WriteString(this.Type);
                    writer.WriteString("nistp" + this.key.KeySize);
                    writer.WriteString(this.key.EncodePublicKey());
                    writer.Flush();

                    return buffer.ToArray();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.key.Dispose();
            base.Dispose(disposing);
        }
    }
}
