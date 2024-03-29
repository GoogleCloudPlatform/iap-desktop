﻿//
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
using System.Linq;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Cryptography
{
    /// <summary>
    /// ECPoint encoding as defined in SEC 1: Elliptic Curve Cryptography.
    /// </summary>
    internal static class ECPointEncoding
    {
        private const byte UncompressedTag = 4;

        /// <summary>
        /// Encode point without compression.
        /// </summary>
        internal static byte[] Encode(
            ECPoint point,
            ushort keySizeInBits)
        {
            var qX = point.X;
            var qY = point.Y;

            var keySizeInBytes = (keySizeInBits + 7) / 8;
            if (qX.Length > keySizeInBytes || qY.Length > keySizeInBytes)
            {
                throw new SshFormatException(
                    "Point coordinates do not match field size");
            }

            var buffer = new byte[keySizeInBytes * 2 + 1];
            buffer[0] = UncompressedTag;
            Array.Copy(qX, 0, buffer, keySizeInBytes - qX.Length + 1, qX.Length);
            Array.Copy(qY, 0, buffer, buffer.Length - qY.Length, qY.Length);
            return buffer;
        }

        /// <summary>
        /// Decode point. Only uncompressed encoding is supported.
        /// </summary>
        internal static ECPoint Decode(
            byte[] encoded,
            ushort keySizeInBits)
        {
            encoded.ExpectNotNull(nameof(encoded));

            var keySizeInBytes = (keySizeInBits + 7) / 8;
            if (encoded.Length == 2 * keySizeInBytes + 1)
            {
                //
                // Point is uncompressed.
                //

                if (encoded[0] != UncompressedTag)
                {
                    throw new SshFormatException(
                        "The data contains a malformed uncomressed point");
                }

                return new ECPoint()
                {
                    X = encoded.Skip(1).Take(keySizeInBytes).ToArray(),
                    Y = encoded.Skip(1 + keySizeInBytes).ToArray(),
                };
            }
            else
            {
                throw new NotImplementedException(
                    "Point compression is not supported");
            }
        }
    }
}
