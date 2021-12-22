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

using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.Ssh.Auth;
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

        [Test]
        public void WhenKeyNotFoundAndCreateNewIsFalse_ThenGetRsaKeyAsyncReturnsNull()
        {
            var adapter = new KeyStoreAdapter();

            Assert.IsNull(adapter.OpenSshKey(
                KeyName,
                CngKeyUsages.Signing,
                false,
                null));
        }

        [Test]
        public void WhenKeyNotFoundAndCreateNewIsTrue_ThenGetRsaKeyAsyncReturnsNewKey()
        {
            var adapter = new KeyStoreAdapter();

            var key = adapter.OpenSshKey(
                KeyName,
                CngKeyUsages.Signing,
                true,
                null);

            Assert.IsNotNull(key);
            Assert.IsInstanceOf<RsaSshKey>(key);
        }

        [Test]
        public void WhenKeyExists_ThenGetRsaKeyAsyncReturnsKey()
        {
            var adapter = new KeyStoreAdapter();

            var key1 = adapter.OpenSshKey(
                KeyName,
                CngKeyUsages.Signing,
                true,
                null);

            var key2 = adapter.OpenSshKey(
                KeyName,
                CngKeyUsages.Signing,
                false,
                null);

            Assert.IsNotNull(key2);
            Assert.AreEqual(key1.PublicKeyString, key2.PublicKeyString);
        }

        [Test]
        public void WhenKeyExistsButUsageDoesNotMatch_ThenGetRsaKeyAsyncReturnsKey()
        {
            var adapter = new KeyStoreAdapter();

            var key1 = adapter.OpenSshKey(
                KeyName,
                CngKeyUsages.Signing,
                true,
                null);

            Assert.Throws<CryptographicException>(
                () => adapter.OpenSshKey(
                    KeyName,
                    CngKeyUsages.Decryption,
                    false,
                    null));
        }
    }
}
