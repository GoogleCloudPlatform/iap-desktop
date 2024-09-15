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

using Google.Solutions.Ssh.Cryptography;
using Google.Solutions.Ssh.Format;
using NUnit.Framework;
using System;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Test.Cryptography
{
    [TestFixture]
    public class TestECPointEncoding
    {
        //--------------------------------------------------------------------
        // Encode.
        //--------------------------------------------------------------------

        [Test]
        public void Encode_WhenKeySizeDoesNotMatchPoint_ThenEncodeThrowsException()
        {
            using (var key = new ECDsaCng(ECCurve.NamedCurves.nistP256))
            {
                Assert.Throws<SshFormatException>(
                    () => ECPointEncoding.Encode(
                        key.ExportParameters(false).Q,
                        128));
            }
        }

        //--------------------------------------------------------------------
        // Decode.
        //--------------------------------------------------------------------

        [Test]
        public void Decode_WhenEncodedAndDecoded_ThenPointIsEqual()
        {
            using (var key = new ECDsaCng(ECCurve.NamedCurves.nistP256))
            {
                var point = key.ExportParameters(false).Q;

                var encoded = ECPointEncoding.Encode(point, (ushort)key.KeySize);

                Assert.AreEqual(4, encoded[0]);
                Assert.AreEqual(65, encoded.Length);

                var restoredPoint = ECPointEncoding.Decode(
                    encoded,
                    (ushort)key.KeySize);

                CollectionAssert.AreEqual(
                    point.X,
                    restoredPoint.X);
                CollectionAssert.AreEqual(
                    point.Y,
                    restoredPoint.Y);
            }
        }

        [Test]
        public void Decode_WhenEncodedDataUncompressedButWithoutTag_ThenDecodeThrowsException()
        {
            var uncompressedSizeData = new byte[256 / 8 * 2 + 1];
            uncompressedSizeData[0] = 1; // Junk.

            Assert.Throws<SshFormatException>(
                () => ECPointEncoding.Decode(uncompressedSizeData, 256));
        }

        [Test]
        public void Decode_WhenEncodedDataNotUncompressed_ThenDecodeThrowsException()
        {
            Assert.Throws<NotImplementedException>(
                () => ECPointEncoding.Decode(
                    new byte[] { 0, 0, 0 },
                    246));
        }
    }
}
