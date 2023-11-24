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
        public void TypeNistp256()
        {
            using (var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP256)))
            using (var publicKey = new EcdsaPublicKey(key, true))
            {
                Assert.AreEqual("ecdsa-sha2-nistp256", publicKey.Type);
            }
        }

        [Test]
        public void TypeNistp384()
        {
            using (var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP384)))
            using (var publicKey = new EcdsaPublicKey(key, true))
            {
                Assert.AreEqual("ecdsa-sha2-nistp384", publicKey.Type);
            }
        }

        [Test]
        public void TypeNistp521()
        {
            using (var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP521)))
            using (var publicKey = new EcdsaPublicKey(key, true))
            {
                Assert.AreEqual("ecdsa-sha2-nistp521", publicKey.Type);
            }
        }

        //---------------------------------------------------------------------
        // FromWireFormat.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDecodedAndEncoded_ThenFromWireFormatReturnsSameKey(
            [Values(
                SampleKeys.Nistp256,
                SampleKeys.Nistp384,
                SampleKeys.Nistp512)] string encodedKey)
        {
            using (var key = EcdsaPublicKey.FromWireFormat(Convert.FromBase64String(encodedKey)))
            {
                Assert.AreEqual(
                    encodedKey,
                    Convert.ToBase64String(key.WireFormatValue));
            }
        }

        [Test]
        public void WhenEncodedKeyIsUnsupported_ThenFromWireFormatThrowsException(
            [Values(
                SampleKeys.Ed25519,
                SampleKeys.Rsa2048)] string encodedKey)
        {
            Assert.Throws<SshFormatException>(
                () => EcdsaPublicKey.FromWireFormat(Convert.FromBase64String(encodedKey)));
        }

        [Test]
        public void WhenKeyTruncated_ThenFromWireFormatThrowsException(
            [Values(1, 20, 40)] int take)
        {
            Assert.Throws<SshFormatException>(
                () => EcdsaPublicKey.FromWireFormat(
                    Convert.FromBase64String(SampleKeys.Nistp256).Take(take).ToArray()));
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOwnsKeyIsTrue_ThenDisposeClosesKey()
        {
            var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP256));
            using (var publicKey = new EcdsaPublicKey(key, true))
            {
                Assert.IsFalse(key.IsDisposed());
            }

            using (var publicKey = new EcdsaPublicKey(key, true))
            {
                Assert.IsTrue(key.IsDisposed());
            }
        }

        [Test]
        public void WhenOwnsKeyIsFalse_ThenDisposeClosesKey()
        {
            using (var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP256)))
            {
                using (var publicKey = new EcdsaPublicKey(key, false))
                {
                    Assert.IsFalse(key.IsDisposed());
                }

                using (var publicKey = new EcdsaPublicKey(key, false))
                {
                    Assert.IsFalse(key.IsDisposed());
                }
            }
        }
    }
}
