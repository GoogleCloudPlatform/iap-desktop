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
    public class TestRsaPublicKey
    {
        //---------------------------------------------------------------------
        // Type.
        //---------------------------------------------------------------------

        [Test]
        public void Type()
        {
            using (var key = new RSACng())
            using (var publicKey = new RsaPublicKey(key, true))
            {
                Assert.AreEqual("ssh-rsa", publicKey.Type);
            }
        }

        //---------------------------------------------------------------------
        // FromWireFormat.
        //---------------------------------------------------------------------

        [Test]
        public void FromWireFormat_WhenDecodedAndEncoded_ThenFromWireFormatReturnsSameKey(
            [Values(
                SampleKeys.Rsa1024,
                SampleKeys.Rsa2048,
                SampleKeys.Rsa4096)] string encodedKey)
        {
            using (var key = RsaPublicKey.FromWireFormat(Convert.FromBase64String(encodedKey)))
            {
                Assert.AreEqual(
                    encodedKey,
                    Convert.ToBase64String(key.WireFormatValue));
            }
        }

        [Test]
        public void FromWireFormat_WhenEncodedKeyIsUnsupported_ThenFromWireFormatThrowsException(
            [Values(
                SampleKeys.Ed25519,
                SampleKeys.Nistp256)] string encodedKey)
        {
            Assert.Throws<SshFormatException>(
                () => RsaPublicKey.FromWireFormat(Convert.FromBase64String(encodedKey)));
        }

        [Test]
        public void FromWireFormat_WhenKeyTruncated_ThenFromWireFormatThrowsException(
            [Values(1, 20, 40)] int take)
        {
            Assert.Throws<SshFormatException>(
                () => RsaPublicKey.FromWireFormat(
                    Convert.FromBase64String(SampleKeys.Nistp256).Take(take).ToArray()));
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose_WhenOwnsKeyIsTrue_ThenDisposeClosesKey()
        {
            var key = new RSACng();
            using (var publicKey = new RsaPublicKey(key, true))
            {
                Assert.IsFalse(key.IsDisposed());
            }

            using (var publicKey = new RsaPublicKey(key, true))
            {
                Assert.IsTrue(key.IsDisposed());
            }
        }

        [Test]
        public void Dispose_WhenOwnsKeyIsFalse_ThenDisposeClosesKey()
        {
            using (var key = new RSACng())
            {
                using (var publicKey = new RsaPublicKey(key, false))
                {
                    Assert.IsFalse(key.IsDisposed());
                }

                using (var publicKey = new RsaPublicKey(key, false))
                {
                    Assert.IsFalse(key.IsDisposed());
                }
            }
        }
    }
}
