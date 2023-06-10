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
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Cryptography
{
    internal static class ECDsaExtensions
    {
        /// <summary>
        /// Encode public key point with point compression.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] EncodePublicKey(
            this ECDsaCng key)
        {
            //
            // Key exporting is only supported on 4.7+.
            //

#if NET47_OR_GREATER
            var point = key.ExportParameters(false).Q;
            var qX = point.X;
            var qY = point.Y;
#else
            var exportMethod = key.GetType().GetMethod(
                "ExportParameters",
                BindingFlags.Instance | BindingFlags.Public);
            if (exportMethod == null)
            {
                throw new PlatformNotSupportedException(
                    "Using ECDSA requires .NET framework 4.7 or newer");
            }

            var ecParameters = exportMethod.Invoke(
                key,
                new object[] { false });

            var qPoint = ecParameters.GetType().GetField(
                    "Q",
                    BindingFlags.Instance | BindingFlags.Public)
                .GetValue(ecParameters);

            var qX = (byte[])qPoint.GetType().GetField(
                    "X",
                    BindingFlags.Instance | BindingFlags.Public)
                .GetValue(qPoint);

            var qY = (byte[])qPoint.GetType().GetField(
                    "Y",
                    BindingFlags.Instance | BindingFlags.Public)
                .GetValue(qPoint);
#endif
            //
            // Get size in bytes (rounding up).
            //
            var keySizeInBytes = (key.KeySize + 7) / 8;
            if (qX.Length > keySizeInBytes || qY.Length > keySizeInBytes)
            {
                throw new ArgumentException("Point coordinates do not match field size");
            }

            //
            // Encode point, see sun/security/util/ECUtil.java or
            // java/org/bouncycastle/math/ec/ECPoint.java for reference.
            //
            var buffer = new byte[keySizeInBytes * 2 + 1];
            buffer[0] = 4; // Tag for 'uncompressed'.
            Array.Copy(qX, 0, buffer, keySizeInBytes - qX.Length + 1, qX.Length);
            Array.Copy(qY, 0, buffer, buffer.Length - qY.Length, qY.Length);
            return buffer;
        }

    }

    internal struct ECDsaSignature
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
