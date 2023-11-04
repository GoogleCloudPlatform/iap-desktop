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

using Google.Solutions.Ssh.Cryptography;
using NUnit.Framework;
using System;
using System.Security.Cryptography;


namespace Google.Solutions.Ssh.Test.Cryptography
{
    [TestFixture]
    public class TestSshKeyPair
    {
        private static readonly string KeyName = "test-" + typeof(TestSshKeyPair).Name;
        private static readonly CngProvider KeyStoragePovider
            = CngProvider.MicrosoftSoftwareKeyStorageProvider;

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
        // NewEphemeralKey.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTypeIsRsa3072_ThenNewEphemeralKeyReturnsKey()
        {
            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.Rsa3072))
            {
                Assert.IsInstanceOf<RsaSshKeyPair>(key);
                Assert.AreEqual(3072, key.KeySize);
            }
        }

        [Test]
        public void WhenTypeIsEcdsaNistp256_ThenNewEphemeralKeyReturnsKey()
        {
            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.EcdsaNistp256))
            {
                Assert.IsInstanceOf<ECDsaSshKeyPair>(key);
                Assert.AreEqual(256, key.KeySize);
            }
        }

        [Test]
        public void WhenTypeIsEcdsaNistp384_ThenNewEphemeralKeyReturnsKey()
        {
            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.EcdsaNistp384))
            {
                Assert.IsInstanceOf<ECDsaSshKeyPair>(key);
                Assert.AreEqual(384, key.KeySize);
            }
        }

        [Test]
        public void WhenTypeIsEcdsaNistp521_ThenNewEphemeralKeyReturnsKey()
        {
            using (var key = SshKeyPair.NewEphemeralKeyPair(SshKeyType.EcdsaNistp521))
            {
                Assert.IsInstanceOf<ECDsaSshKeyPair>(key);
                Assert.AreEqual(521, key.KeySize);
            }
        }

        [Test]
        public void WhenTypeIsInvalid_ThenNewEphemeralKeyReturnsThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => SshKeyPair.NewEphemeralKeyPair((SshKeyType)0xFF));
        }

        //---------------------------------------------------------------------
        // OpenPersistentKey.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTypeIsRsa3072_ThenOpenPersistentKeyReturnsKey()
        {
            using (var key = SshKeyPair.OpenPersistentKeyPair(
                KeyName,
                SshKeyType.Rsa3072,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero))
            {
                Assert.IsNotNull(key);
                Assert.IsInstanceOf<RsaSshKeyPair>(key);
                Assert.AreEqual(3072, key!.KeySize);
            }
        }

        [Test]
        public void WhenTypeIsEcdsaNistp256_ThenOpenPersistentKeyReturnsKey()
        {
            using (var key = SshKeyPair.OpenPersistentKeyPair(
                KeyName,
                SshKeyType.EcdsaNistp256,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero))
            {
                Assert.IsNotNull(key);
                Assert.IsInstanceOf<ECDsaSshKeyPair>(key);
                Assert.AreEqual(256, key!.KeySize);
            }
        }

        [Test]
        public void WhenTypeIsEcdsaNistp384_ThenOpenPersistentKeyReturnsKey()
        {
            using (var key = SshKeyPair.OpenPersistentKeyPair(
                KeyName,
                SshKeyType.EcdsaNistp384,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero))
            {
                Assert.IsNotNull(key);
                Assert.IsInstanceOf<ECDsaSshKeyPair>(key);
                Assert.AreEqual(384, key!.KeySize);
            }
        }

        [Test]
        public void WhenTypeIsEcdsaNistp521_ThenOpenPersistentKeyReturnsKey()
        {
            using (var key = SshKeyPair.OpenPersistentKeyPair(
                KeyName,
                SshKeyType.EcdsaNistp521,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero))
            {
                Assert.IsNotNull(key);
                Assert.IsInstanceOf<ECDsaSshKeyPair>(key);
                Assert.AreEqual(521, key!.KeySize);
            }
        }

        [Test]
        public void WhenKeyFoundAndCreateNewIsFalse_ThenOpenPersistentKeyReturnsKey(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType)
        {
            using (var createdKey = SshKeyPair.OpenPersistentKeyPair(
                KeyName,
                keyType,
                KeyStoragePovider,
                CngKeyUsages.AllUsages,
                true,
                IntPtr.Zero))
            using (var openedKey = SshKeyPair.OpenPersistentKeyPair(
                KeyName,
                keyType,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                false,
                IntPtr.Zero))
            {
                Assert.IsNotNull(createdKey);
                Assert.IsNotNull(openedKey);
                Assert.AreEqual(createdKey!.PublicKeyString, openedKey!.PublicKeyString);
            }
        }

        [Test]
        public void WhenKeyNotFoundAndCreateNewIsFalse_ThenOpenPersistentKeyReturnsNull(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType)
        {
            using (var key = SshKeyPair.OpenPersistentKeyPair(
                KeyName,
                keyType,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                false,
                IntPtr.Zero))
            {
                Assert.IsNull(key);
            }
        }

        [Test]
        public void WhenKeyFoundButAlgorithmMismatches_ThenOpenPersistentKeyThrowsException()
        {
            using (SshKeyPair.OpenPersistentKeyPair(
                KeyName,
                SshKeyType.Rsa3072,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero))
            { }

            Assert.Throws<CryptographicException>(
                () => SshKeyPair.OpenPersistentKeyPair(
                    KeyName,
                    SshKeyType.EcdsaNistp256,
                    KeyStoragePovider,
                    CngKeyUsages.Signing,
                    false,
                    IntPtr.Zero));
        }

        [Test]
        public void WhenKeyFoundButUsageMismatches_ThenOpenPersistentKeyThrowsException()
        {
            using (SshKeyPair.OpenPersistentKeyPair(
                KeyName,
                SshKeyType.Rsa3072,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero))
            { }

            Assert.Throws<CryptographicException>(
                () => SshKeyPair.OpenPersistentKeyPair(
                    KeyName,
                    SshKeyType.Rsa3072,
                    KeyStoragePovider,
                    CngKeyUsages.KeyAgreement,
                    false,
                    IntPtr.Zero));
        }

        [Test]
        public void WhenKeTypeIsInvalid_ThenOpenPersistentKeyThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => SshKeyPair.OpenPersistentKeyPair(
                    KeyName,
                    (SshKeyType)0xFF,
                    KeyStoragePovider,
                    CngKeyUsages.KeyAgreement,
                    false,
                    IntPtr.Zero));
        }

        //---------------------------------------------------------------------
        // DeletePersistentKey.
        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyExists_ThenDeletePersistentKeyDeletesKey()
        {
            using (SshKeyPair.OpenPersistentKeyPair(
                KeyName,
                SshKeyType.Rsa3072,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero))
            { }

            SshKeyPair.DeletePersistentKeyPair(KeyName);
            SshKeyPair.DeletePersistentKeyPair(KeyName);

            Assert.IsNull(SshKeyPair.OpenPersistentKeyPair(
                KeyName,
                SshKeyType.Rsa3072,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                false,
                IntPtr.Zero));
        }
    }
}
