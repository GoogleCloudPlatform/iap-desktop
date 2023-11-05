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

using Google.Solutions.Ssh.Cryptography;
using NUnit.Framework;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Test.Cryptography
{
    [TestFixture]
    public class TestEcdsaKeyCredential
    {
        //---------------------------------------------------------------------
        // HashAlgorithm.
        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyIsNistP256_ThenHashAlgorithmIsSha256()
        {
            using (var key = new ECDsaCng(256))
            using (var credential = new EcdsaKeyCredential(key))
            {
                Assert.AreEqual(HashAlgorithmName.SHA256, credential.HashAlgorithm);
            }
        }

        [Test]
        public void WhenKeyIsNistP384_ThenHashAlgorithmIsSha384()
        {
            using (var key = new ECDsaCng(384))
            using (var credential = new EcdsaKeyCredential(key))
            {
                Assert.AreEqual(HashAlgorithmName.SHA384, credential.HashAlgorithm);
            }
        }

        [Test]
        public void WhenKeyIsNistP521_ThenHashAlgorithmIsSha512()
        {
            using (var key = new ECDsaCng(521))
            using (var credential = new EcdsaKeyCredential(key))
            {
                Assert.AreEqual(HashAlgorithmName.SHA512, credential.HashAlgorithm);
            }
        }

        //---------------------------------------------------------------------
        // Sign.
        //---------------------------------------------------------------------

        [Test]
        public void Sign()
        {
            Assert.Fail("NIY");
        }
    }
}
