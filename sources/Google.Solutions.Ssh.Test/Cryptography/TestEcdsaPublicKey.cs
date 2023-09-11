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
using System.IO;
using System;
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
            using (var publicKey = new EcdsaPublicKey(key))
            {
                Assert.AreEqual("ecdsa-sha2-nistp256", publicKey.Type);
            }
        }

        [Test]
        public void TypeNistp384()
        {

            using (var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP384)))
            using (var publicKey = new EcdsaPublicKey(key))
            {
                Assert.AreEqual("ecdsa-sha2-nistp384", publicKey.Type);
            }
        }

        [Test]
        public void TypeNistp521()
        {

            using (var key = new ECDsaCng(CngKey.Create(CngAlgorithm.ECDsaP521)))
            using (var publicKey = new EcdsaPublicKey(key))
            {
                Assert.AreEqual("ecdsa-sha2-nistp521", publicKey.Type);
            }
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDecodedAndEncoded_ThenValueIsEqual()
        {
            Assert.Fail("NIY");
        }

        [Test]
        public void WhenKeyValid_ThenCtorSucceeds()
        {
            Assert.Fail("NIY");
        }

        [Test]
        public void WhenKeyTruncated_ThenConstructorThrowsException()
        {
            Assert.Fail("NIY");
        }
    }
}
