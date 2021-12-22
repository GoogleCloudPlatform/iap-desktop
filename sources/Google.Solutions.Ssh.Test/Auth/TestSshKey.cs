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

using Google.Solutions.Ssh.Auth;
using Google.Solutions.Ssh.Cryptography;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


namespace Google.Solutions.Ssh.Test.Auth
{
    [TestFixture]
    public class TestSshKey
    {
        private static readonly string KeyName = "test-" + typeof(TestSshKey).Name;
        private readonly CngProvider KeyStoragePovider 
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
            var key = SshKey.NewEphemeralKey(SshKeyType.Rsa3072);
            Assert.IsInstanceOf<RsaSshKey>(key);
            Assert.AreEqual(3072, key.KeySize);
        }

        [Test]
        public void WhenTypeIsEcdsaNistp256_ThenNewEphemeralKeyReturnsKey()
        {
            var key = SshKey.NewEphemeralKey(SshKeyType.EcdsaNistp256);
            Assert.IsInstanceOf<ECDsaSshKey>(key);
            Assert.AreEqual(256, key.KeySize);
        }

        [Test]
        public void WhenTypeIsEcdsaNistp384_ThenNewEphemeralKeyReturnsKey()
        {
            var key = SshKey.NewEphemeralKey(SshKeyType.EcdsaNistp384);
            Assert.IsInstanceOf<ECDsaSshKey>(key);
            Assert.AreEqual(384, key.KeySize);
        }

        [Test]
        public void WhenTypeIsEcdsaNistp521_ThenNewEphemeralKeyReturnsKey()
        {
            var key = SshKey.NewEphemeralKey(SshKeyType.EcdsaNistp521);
            Assert.IsInstanceOf<ECDsaSshKey>(key);
            Assert.AreEqual(521, key.KeySize);
        }

        [Test]
        public void WhenTypeIsInvalid_ThenNewEphemeralKeyReturnsThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => SshKey.NewEphemeralKey((SshKeyType)0xFF));
        }

        //---------------------------------------------------------------------
        // OpenPersistentKey.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTypeIsRsa3072_ThenOpenPersistentKeyReturnsKey()
        {
            var key = SshKey.OpenPersistentKey(
                KeyName,
                SshKeyType.Rsa3072,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero);
            Assert.IsInstanceOf<RsaSshKey>(key);
            Assert.AreEqual(3072, key.KeySize);
        }

        [Test]
        public void WhenTypeIsEcdsaNistp256_ThenOpenPersistentKeyReturnsKey()
        {
            var key = SshKey.OpenPersistentKey(
                KeyName,
                SshKeyType.EcdsaNistp256,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero);
            Assert.IsInstanceOf<ECDsaSshKey>(key);
            Assert.AreEqual(256, key.KeySize);
        }

        [Test]
        public void WhenTypeIsEcdsaNistp384_ThenOpenPersistentKeyReturnsKey()
        {
            var key = SshKey.OpenPersistentKey(
                KeyName,
                SshKeyType.EcdsaNistp384,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero);
            Assert.IsInstanceOf<ECDsaSshKey>(key);
            Assert.AreEqual(384, key.KeySize);
        }

        [Test]
        public void WhenTypeIsEcdsaNistp521_ThenOpenPersistentKeyReturnsKey()
        {
            var key = SshKey.OpenPersistentKey(
                KeyName,
                SshKeyType.EcdsaNistp521,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero);
            Assert.IsInstanceOf<ECDsaSshKey>(key);
            Assert.AreEqual(521, key.KeySize);
        }

        [Test]
        public void WhenKeyFoundAndCreateNewIsFalse_ThenOpenPersistentKeyReturnsKey(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType)
        {
            var createdKey = SshKey.OpenPersistentKey(
                KeyName,
                keyType,
                KeyStoragePovider,
                CngKeyUsages.AllUsages,
                true,
                IntPtr.Zero);

            var openedKey = SshKey.OpenPersistentKey(
                KeyName,
                keyType,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                false,
                IntPtr.Zero);

            Assert.IsNotNull(openedKey);
            Assert.AreEqual(createdKey.PublicKeyString, openedKey.PublicKeyString);
        }

        [Test]
        public void WhenKeyNotFoundAndCreateNewIsFalse_ThenOpenPersistentKeyReturnsNull(
            [Values(SshKeyType.Rsa3072, SshKeyType.EcdsaNistp256)] SshKeyType keyType)
        {
            var key = SshKey.OpenPersistentKey(
                KeyName,
                keyType,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                false,
                IntPtr.Zero);
            Assert.IsNull(key);
        }

        [Test]
        public void WhenKeyFoundButAlgorithmMismatches_ThenOpenPersistentKeyThrowsException()
        {
            SshKey.OpenPersistentKey(
                KeyName,
                SshKeyType.Rsa3072,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero);

            Assert.Throws<CryptographicException>(
                () => SshKey.OpenPersistentKey(
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
            SshKey.OpenPersistentKey(
                KeyName,
                SshKeyType.Rsa3072,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero);

            Assert.Throws<CryptographicException>(
                () => SshKey.OpenPersistentKey(
                    KeyName,
                    SshKeyType.Rsa3072,
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
            SshKey.OpenPersistentKey(
                KeyName,
                SshKeyType.Rsa3072,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                true,
                IntPtr.Zero);

            SshKey.DeletePersistentKey(KeyName);
            SshKey.DeletePersistentKey(KeyName);

            Assert.IsNull(SshKey.OpenPersistentKey(
                KeyName,
                SshKeyType.Rsa3072,
                KeyStoragePovider,
                CngKeyUsages.Signing,
                false,
                IntPtr.Zero));
        }
    }
}
