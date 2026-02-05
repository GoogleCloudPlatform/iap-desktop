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
using NUnit.Framework;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Test.Cryptography
{
    [TestFixture]
    public class TestEcdsaSigner
    {
        //---------------------------------------------------------------------
        // HashAlgorithm.
        //---------------------------------------------------------------------

        [Test]
        public void HashAlgorithm_WhenKeyIsNistP256_ThenHashAlgorithmIsSha256()
        {
            using (var key = new ECDsaCng(256))
            using (var credential = new ECDsaSigner(key, true))
            {
                Assert.That(credential.HashAlgorithm, Is.EqualTo(HashAlgorithmName.SHA256));
            }
        }

        [Test]
        public void HashAlgorithm_WhenKeyIsNistP384_ThenHashAlgorithmIsSha384()
        {
            using (var key = new ECDsaCng(384))
            using (var credential = new ECDsaSigner(key, true))
            {
                Assert.That(credential.HashAlgorithm, Is.EqualTo(HashAlgorithmName.SHA384));
            }
        }

        [Test]
        public void HashAlgorithm_WhenKeyIsNistP521_ThenHashAlgorithmIsSha512()
        {
            using (var key = new ECDsaCng(521))
            using (var credential = new ECDsaSigner(key, true))
            {
                Assert.That(credential.HashAlgorithm, Is.EqualTo(HashAlgorithmName.SHA512));
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose_WhenOwnsKeyIsTrue_ThenDisposeClosesKey()
        {
            var key = new ECDsaCng(521);
            using (var signer = new ECDsaSigner(key, true))
            {
                Assert.That(key.IsDisposed(), Is.False);
            }

            using (var signer = new ECDsaSigner(key, true))
            {
                Assert.That(key.IsDisposed(), Is.True);
            }
        }

        [Test]
        public void Dispose_WhenOwnsKeyIsFalse_ThenDisposeClosesKey()
        {
            using (var key = new ECDsaCng(521))
            {
                using (var signer = new ECDsaSigner(key, false))
                {
                    Assert.That(key.IsDisposed(), Is.False);
                }

                using (var signer = new ECDsaSigner(key, false))
                {
                    Assert.That(key.IsDisposed(), Is.False);
                }
            }
        }
    }
}
