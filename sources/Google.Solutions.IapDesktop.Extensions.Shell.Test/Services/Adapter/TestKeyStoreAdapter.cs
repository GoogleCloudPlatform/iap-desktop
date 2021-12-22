//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Auth;
using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.Ssh.Auth;
using Moq;
using NUnit.Framework;
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Adapter
{
    [TestFixture]
    public class TestKeyStoreAdapter : ApplicationFixtureBase
    {
        // This is for testing only, so use a weak key size.
        private static readonly string KeyName = "test-" + typeof(TestKeyStoreAdapter).Name;

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
        // CreateKeyName
        //---------------------------------------------------------------------

        [Test]
        public void WhenUsingRsa3072AndMicrosoftSoftwareKsp_ThenCreateKeyNameReturnsLegacyName()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("bob@example.com");

            var keyName = KeyStoreAdapter.CreateKeyName(
                authorization.Object,
                SshKeyType.Rsa3072,
                CngProvider.MicrosoftSoftwareKeyStorageProvider);

            Assert.AreEqual("IAPDESKTOP_bob@example.com", keyName);
        }

        [Test]
        public void WhenUsingRsa3072AndMicrosoftSmartCardKsp_ThenCreateKeyNameReturnsLegacyName()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("bob@example.com");

            var keyName = KeyStoreAdapter.CreateKeyName(
                authorization.Object,
                SshKeyType.Rsa3072,
                CngProvider.MicrosoftSmartCardKeyStorageProvider);

            Assert.AreEqual("IAPDESKTOP_bob@example.com_0001_E7909B75", keyName);
        }

        [Test]
        public void WhenUsingEcdsaNistp256AndMicrosoftSoftwareKsp_ThenCreateKeyNameReturnsLegacyName()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("bob@example.com");

            var keyName = KeyStoreAdapter.CreateKeyName(
                authorization.Object,
                SshKeyType.EcdsaNistp256,
                CngProvider.MicrosoftSoftwareKeyStorageProvider);

            Assert.AreEqual("IAPDESKTOP_bob@example.com_0011_094FE673", keyName);
        }

        [Test]
        public void WhenUsingEcdsaNistp256AndMicrosoftSmartCardKsp_ThenCreateKeyNameReturnsLegacyName()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("bob@example.com");

            var keyName = KeyStoreAdapter.CreateKeyName(
                authorization.Object,
                SshKeyType.EcdsaNistp256,
                CngProvider.MicrosoftSmartCardKeyStorageProvider);

            Assert.AreEqual("IAPDESKTOP_bob@example.com_0011_E7909B75", keyName);
        }

        [Test]
        public void WhenUsingEcdsaNistp521AndMicrosoftSoftwareKsp_ThenCreateKeyNameReturnsLegacyName()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("bob@example.com");

            var keyName = KeyStoreAdapter.CreateKeyName(
                authorization.Object,
                SshKeyType.EcdsaNistp521,
                CngProvider.MicrosoftSoftwareKeyStorageProvider);

            Assert.AreEqual("IAPDESKTOP_bob@example.com_0013_094FE673", keyName);
        }

        [Test]
        public void WhenUsingEcdsaNistp521AndMicrosoftSmartCardKsp_ThenCreateKeyNameReturnsLegacyName()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("bob@example.com");

            var keyName = KeyStoreAdapter.CreateKeyName(
                authorization.Object,
                SshKeyType.EcdsaNistp521,
                CngProvider.MicrosoftSmartCardKeyStorageProvider);

            Assert.AreEqual("IAPDESKTOP_bob@example.com_0013_E7909B75", keyName);
        }

        //---------------------------------------------------------------------
        // OpenSshKey
        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyNotFoundAndCreateNewIsFalse_ThenOpenSshKeyReturnsNull()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("notfound@example.com");

            var adapter = new KeyStoreAdapter();

            Assert.IsNull(adapter.OpenSshKey(
                SshKeyType.Rsa3072,
                authorization.Object,
                false,
                null));
        }

        [Test]
        public void WhenKeyNotFoundAndCreateNewIsTrue_ThenOpenSshKeyReturnsNewKey()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("bob@example.com");

            var adapter = new KeyStoreAdapter();
            adapter.DeleteSshKey(SshKeyType.Rsa3072, authorization.Object);

            var key = adapter.OpenSshKey(
                SshKeyType.Rsa3072,
                authorization.Object,
                true,
                null);

            Assert.IsNotNull(key);
            Assert.IsInstanceOf<RsaSshKey>(key);
        }

        [Test]
        public void WhenKeyExists_ThenOpenSshKeyReturnsKey()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("bob@example.com");

            var adapter = new KeyStoreAdapter();

            var key1 = adapter.OpenSshKey(
                SshKeyType.Rsa3072,
                authorization.Object,
                true,
                null);

            var key2 = adapter.OpenSshKey(
                SshKeyType.Rsa3072,
                authorization.Object,
                false,
                null);

            Assert.IsNotNull(key2);
            Assert.AreEqual(key1.PublicKeyString, key2.PublicKeyString);
        }

        [Test]
        public void WhenKeyExistsButKeyTypeDifferent_ThenOpenSshKeyReturnsNewKey()
        {
            var authorization = new Mock<IAuthorization>();
            authorization
                .SetupGet(a => a.Email)
                .Returns("bob@example.com");

            var adapter = new KeyStoreAdapter();

            var rsaKey = adapter.OpenSshKey(
                SshKeyType.Rsa3072,
                authorization.Object,
                true,
                null);
            Assert.IsNotNull(rsaKey);

            var ecdsaKey = adapter.OpenSshKey(
                SshKeyType.EcdsaNistp256,
                authorization.Object,
                true,
                null);
            Assert.IsNotNull(ecdsaKey);

            Assert.AreNotEqual(rsaKey.PublicKeyString, ecdsaKey.PublicKeyString);
        }
    }
}
