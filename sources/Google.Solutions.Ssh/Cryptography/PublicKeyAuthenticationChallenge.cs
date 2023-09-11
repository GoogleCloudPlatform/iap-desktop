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
using System.IO;

namespace Google.Solutions.Ssh.Cryptography
{
    /// <summary>
    /// Challenge sent by server that the client must create
    /// a signature for.
    /// </summary>
    internal struct PublicKeyAuthenticationChallenge
    {
        public PublicKeyAuthenticationChallenge(byte[] value)
        {
            this.Value = value.ExpectNotNull(nameof(value));

            //
            // From RFC4252:
            //
            // The value of 'signature' is a signature by the corresponding private
            // key over the following data, in the following order:
            // 
            //   string     session identifier
            //   byte       SSH_MSG_USERAUTH_REQUEST
            //   string     user name
            //   string     service name
            //   string     "publickey"
            //   boolean    TRUE
            //   string     public key algorithm name
            //   string     public key to be used for authentication
            //

            using (var reader = new SshReader(new MemoryStream(value)))
            {
                this.SessionId = reader.ReadString();

                var code = reader.ReadByte();
                if (code != (byte)MessageCodes.SSH_MSG_USERAUTH_REQUEST)
                {
                    throw new SshFormatException(
                        $"The signature contains an unrecognized message code: {code}");
                }

                this.Username = reader.ReadString();
                this.Service = reader.ReadString();

                var type = reader.ReadString();
                if (type != "publickey")
                {
                    throw new SshFormatException(
                        $"The signature contains an unrecognized type: {type}");
                }

                reader.ReadBoolean();

                this.Algorithm = reader.ReadString();
                this.PublicKey = reader.ReadString();
            }
        }

        /// <summary>
        /// The raw value over which a signature must be returned.
        /// </summary>
        public byte[] Value { get; }

        /// <summary>
        /// Session ID.
        /// </summary>
        public string SessionId { get; private set; }

        /// <summary>
        /// Username that's used for authentication.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Service, always set to "ssh-connection".
        /// </summary>
        public string Service { get; private set; }

        /// <summary>
        /// Algorithm such as 'rsa-sha2-512'.
        /// </summary>
        public string Algorithm { get; private set; }

        /// <summary>
        /// Encoded public key.
        /// </summary>
        public string PublicKey { get; private set; }
    }
}
