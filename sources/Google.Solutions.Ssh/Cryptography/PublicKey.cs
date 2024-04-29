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

using Google.Solutions.Common.Runtime;
using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Google.Solutions.Ssh.Cryptography
{
    /// <summary>
    /// An SSH public key.
    /// </summary>
    public abstract class PublicKey : DisposableBase, IEquatable<PublicKey>
    {
        /// <summary>
        /// Key type, such as rsa-ssh.
        /// </summary>
        public abstract string Type { get; }

        /// <summary>
        /// Wire format of public key as defined in:
        /// * RFC4253 (6.6) for RSA and DSA keys
        /// * RFC5656 (3.1) for ECDSA keys
        /// * PROTOCOL.certkeys for the OpenSSH certificate formats.
        /// </summary>
        public abstract byte[] WireFormatValue { get; }

        public string ToString(Format format)
        {
            //
            // NB. SSH itself (RFC 4251) only specifies the wire format,
            // not any serialization format.
            //
            if (format == Format.OpenSsh)
            {
                // 
                // Format as defined in the OpenSSH PROTOCOL file:
                //
                //   OpenSSH public keys [...] are formatted as a single line
                //   of text consisting of the public key algorithm name
                //   followed by a base64-encoded key blob.
                //   
                //   The public key blob (before base64 encoding) is the same
                //   format used for the encoding of public keys sent on the wire
                //   [...].
                //
                //
                return $"{this.Type} {Convert.ToBase64String(this.WireFormatValue)}";
            }
            else
            {
                //
                // Format as defiend in RFC 4716.
                //
                return new StringBuilder()
                    .AppendLine(Ssh2FileFormat.Header)
                    .AppendLine(Convert.ToBase64String(this.WireFormatValue))
                    .AppendLine(Ssh2FileFormat.Footer)
                    .ToString();
            }
        }

        public bool Equals(PublicKey other)
        {
            return
                this.Type == other.Type &&
                Enumerable.SequenceEqual(this.WireFormatValue, other.WireFormatValue);
        }

        public override bool Equals(object obj)
        {
            return obj is PublicKey key && Equals(key);
        }

        public override int GetHashCode()
        {
            return new BigInteger(this.WireFormatValue).GetHashCode();
        }

        public override string ToString()
        {
            return ToString(Format.OpenSsh);
        }

        public enum Format
        {
            /// <summary>
            /// Single-line format as used by OpenSSH.
            /// </summary>
            OpenSsh,

            /// <summary>
            /// Multi-line format as defiend in RFC 4716.
            /// </summary>
            Ssh2
        }

        protected static class Ssh2FileFormat
        {
            internal const string Header = "---- BEGIN SSH2 PUBLIC KEY ----";
            internal const string Footer = "---- END SSH2 PUBLIC KEY ----";
        }
    }
}
