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
using System.Linq;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Test.Cryptography
{
    [TestFixture]
    public class TestEcdsaPublicKey
    {
        //---------------------------------------------------------------------
        // Type.
        //---------------------------------------------------------------------

        [Test]
        public void Type_Nistp256()
        {
            using (var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP256)))
            using (var publicKey = new ECDsaPublicKey(key, true))
            {
                Assert.That(publicKey.Type, Is.EqualTo("ecdsa-sha2-nistp256"));
            }
        }

        [Test]
        public void Type_Nistp384()
        {
            using (var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP384)))
            using (var publicKey = new ECDsaPublicKey(key, true))
            {
                Assert.That(publicKey.Type, Is.EqualTo("ecdsa-sha2-nistp384"));
            }
        }

        [Test]
        public void Type_Nistp521()
        {
            using (var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP521)))
            using (var publicKey = new ECDsaPublicKey(key, true))
            {
                Assert.That(publicKey.Type, Is.EqualTo("ecdsa-sha2-nistp521"));
            }
        }

        //---------------------------------------------------------------------
        // FromWireFormat.
        //---------------------------------------------------------------------

        [Test]
        public void FromWireFormat_WhenDecodedAndEncoded_ThenFromWireFormatReturnsSameKey(
            [Values(
                SampleKeys.Nistp256,
                SampleKeys.Nistp384,
                SampleKeys.Nistp512)] string encodedKey)
        {
            using (var key = ECDsaPublicKey.FromWireFormat(Convert.FromBase64String(encodedKey)))
            {
                Assert.That(
                    Convert.ToBase64String(key.WireFormatValue), Is.EqualTo(encodedKey));
            }
        }

        [Test]
        public void FromWireFormat_WhenEncodedKeyIsUnsupported_ThenFromWireFormatThrowsException(
            [Values(
                SampleKeys.Ed25519,
                SampleKeys.Rsa2048)] string encodedKey)
        {
            Assert.Throws<SshFormatException>(
                () => ECDsaPublicKey.FromWireFormat(Convert.FromBase64String(encodedKey)));
        }

        [Test]
        public void FromWireFormat_WhenKeyTruncated_ThenFromWireFormatThrowsException(
            [Values(1, 20, 40)] int take)
        {
            Assert.Throws<SshFormatException>(
                () => ECDsaPublicKey.FromWireFormat(
                    Convert.FromBase64String(SampleKeys.Nistp256).Take(take).ToArray()));
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose_WhenOwnsKeyIsTrue_ThenDisposeClosesKey()
        {
            var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP256));
            using (var publicKey = new ECDsaPublicKey(key, true))
            {
                Assert.That(key.IsDisposed(), Is.False);
            }

            using (var publicKey = new ECDsaPublicKey(key, true))
            {
                Assert.IsTrue(key.IsDisposed());
            }
        }

        [Test]
        public void Dispose_WhenOwnsKeyIsFalse_ThenDisposeClosesKey()
        {
            using (var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP256)))
            {
                using (var publicKey = new ECDsaPublicKey(key, false))
                {
                    Assert.That(key.IsDisposed(), Is.False);
                }

                using (var publicKey = new ECDsaPublicKey(key, false))
                {
                    Assert.That(key.IsDisposed(), Is.False);
                }
            }
        }
    }
}
