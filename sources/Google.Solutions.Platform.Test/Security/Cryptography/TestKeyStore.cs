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

using Google.Solutions.Platform.Security.Cryptography;
using NUnit.Framework;
using System;
using System.Security.Cryptography;

namespace Google.Solutions.Platform.Test.Security.Cryptography
{
    [TestFixture]
    public class TestKeyStore
    {
        private static readonly string KeyName = "test-" + typeof(TestKeyStore).Name;
        private readonly KeyStore Store =
            new KeyStore(CngProvider.MicrosoftSoftwareKeyStorageProvider);

        public enum CommonKeyType
        {
            Rsa1024,
            Rsa2048,
            Rsa3072,
            EcdsaP256,
            EcdsaP384,
            EcdsaP521
        }

        private static KeyType LookupCommonKeyType(CommonKeyType commonKeyType)
        {
            return commonKeyType switch
            {
                CommonKeyType.Rsa1024 => new KeyType(CngAlgorithm.Rsa, 1024),
                CommonKeyType.Rsa2048 => new KeyType(CngAlgorithm.Rsa, 2048),
                CommonKeyType.Rsa3072 => new KeyType(CngAlgorithm.Rsa, 3072),
                CommonKeyType.EcdsaP256 => new KeyType(CngAlgorithm.ECDsaP256, 256),
                CommonKeyType.EcdsaP384 => new KeyType(CngAlgorithm.ECDsaP384, 384),
                CommonKeyType.EcdsaP521 => new KeyType(CngAlgorithm.ECDsaP521, 521),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        [SetUp]
        public void SetUp()
        {
            try
            {
                CngKey.Open(KeyName).Delete();
            }
            catch (CryptographicException)
            { }
        }

        //---------------------------------------------------------------------
        // OpenKey.
        //---------------------------------------------------------------------

        [Test]
        public void OpenKey_WhenTypeIsValid_ThenOpenPersistentKeyReturnsKey(
            [Values(
                CommonKeyType.Rsa1024,
                CommonKeyType.Rsa2048,
                CommonKeyType.Rsa3072,
                CommonKeyType.EcdsaP256,
                CommonKeyType.EcdsaP384,
                CommonKeyType.EcdsaP521)] CommonKeyType commonType)
        {
            var keyType = LookupCommonKeyType(commonType);
            using (var key = this.Store.OpenKey(
                IntPtr.Zero,
                KeyName,
                keyType,
                CngKeyUsages.Signing,
                false))
            {
                Assert.IsNotNull(key);
                Assert.That(key.KeySize, Is.EqualTo(keyType.Size));
            }
        }

        [Test]
        public void OpenKey_WhenTypeIsInvalid_ThenOpenPersistentKeyThrowsException()
        {
            var keyType = new KeyType(CngAlgorithm.Rsa, 1);

            Assert.Throws<CryptographicException>(
                () => this.Store.OpenKey(
                    IntPtr.Zero,
                    KeyName,
                    keyType,
                    CngKeyUsages.Signing,
                    false));
        }

        [Test]
        public void OpenKey_WhenKeyFoundAndForceCreateIsFalse_ThenOpenPersistentKeyReturnsExistingKey(
            [Values(
                CommonKeyType.Rsa1024,
                CommonKeyType.Rsa2048,
                CommonKeyType.Rsa3072,
                CommonKeyType.EcdsaP256,
                CommonKeyType.EcdsaP384,
                CommonKeyType.EcdsaP521)] CommonKeyType commonType)
        {
            var keyType = LookupCommonKeyType(commonType);
            using (var createdKey = this.Store.OpenKey(
                IntPtr.Zero,
                KeyName,
                keyType,
                CngKeyUsages.AllUsages,
                false))
            using (var openedKey = this.Store.OpenKey(
                IntPtr.Zero,
                KeyName,
                keyType,
                CngKeyUsages.AllUsages,
                false))
            {
                Assert.IsNotNull(createdKey);
                Assert.IsNotNull(openedKey);
                CngAssert.AssertEqual(keyType.Algorithm, createdKey, openedKey);
            }
        }

        [Test]
        public void OpenKey_WhenKeyFoundAndForceCreateIsTrue_ThenOpenPersistentKeyReturnsNewKey(
            [Values(
                CommonKeyType.Rsa1024,
                CommonKeyType.Rsa2048,
                CommonKeyType.Rsa3072,
                CommonKeyType.EcdsaP256,
                CommonKeyType.EcdsaP384,
                CommonKeyType.EcdsaP521)] CommonKeyType commonType)
        {
            var keyType = LookupCommonKeyType(commonType);
            using (var createdKey = this.Store.OpenKey(
                IntPtr.Zero,
                KeyName,
                keyType,
                CngKeyUsages.AllUsages,
                false))
            using (var openedKey = this.Store.OpenKey(
                IntPtr.Zero,
                KeyName,
                keyType,
                CngKeyUsages.AllUsages,
                true))
            {
                Assert.IsNotNull(createdKey);
                Assert.IsNotNull(openedKey);
                CngAssert.AssertNotEqual(keyType.Algorithm, createdKey, openedKey);
            }
        }

        [Test]
        public void OpenKey_WhenKeyFoundButAlgorithmMismatches_ThenOpenPersistentKeyThrowsException()
        {
            using (this.Store.OpenKey(
                IntPtr.Zero,
                KeyName,
                new KeyType(CngAlgorithm.Rsa, 1024),
                CngKeyUsages.Signing,
                false))
            { }

            Assert.Throws<KeyConflictException>(
                () => this.Store.OpenKey(
                    IntPtr.Zero,
                    KeyName,
                    new KeyType(CngAlgorithm.ECDsaP256, 256),
                    CngKeyUsages.Signing,
                    false));
        }

        [Test]
        public void OpenKey_WhenKeyFoundButSizeMismatches_ThenOpenPersistentKeyThrowsException()
        {
            using (this.Store.OpenKey(
                IntPtr.Zero,
                KeyName,
                new KeyType(CngAlgorithm.Rsa, 1024),
                CngKeyUsages.Signing,
                false))
            { }

            Assert.Throws<KeyConflictException>(
                () => this.Store.OpenKey(
                    IntPtr.Zero,
                    KeyName,
                    new KeyType(CngAlgorithm.Rsa, 2048),
                    CngKeyUsages.Signing,
                    false));
        }

        [Test]
        public void OpenKey_WhenKeyFoundButUsageMismatches_ThenOpenPersistentKeyThrowsException()
        {
            using (this.Store.OpenKey(
                IntPtr.Zero,
                KeyName,
                new KeyType(CngAlgorithm.Rsa, 1024),
                CngKeyUsages.Signing,
                false))
            { }

            Assert.Throws<KeyConflictException>(
                () => this.Store.OpenKey(
                    IntPtr.Zero,
                    KeyName,
                    new KeyType(CngAlgorithm.Rsa, 1024),
                    CngKeyUsages.KeyAgreement,
                    false));
        }

        //---------------------------------------------------------------------
        // DeletePersistentKey.
        //---------------------------------------------------------------------

        [Test]
        public void DeletePersistentKey_WhenKeyExists()
        {
            using (this.Store.OpenKey(
                IntPtr.Zero,
                KeyName,
                new KeyType(CngAlgorithm.Rsa, 1024),
                CngKeyUsages.Signing,
                false))
            { }

            this.Store.DeleteKey(KeyName);
            this.Store.DeleteKey(KeyName);

            Assert.That(CngKey.Exists(KeyName), Is.False);
        }
    }
}
