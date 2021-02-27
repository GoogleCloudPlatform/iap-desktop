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

using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using NUnit.Framework;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Adapter
{
    [TestFixture]
    public class TestKeyStoreAdapter : ApplicationFixtureBase
    {
        // This is for testing only, so use a weak key size.
        private const int KeySize = 1024;
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
        public async Task WhenKeyNotFoundAndCreateNewIsFalse_ThenGetRsaKeyAsyncReturnsNull()
        {
            var adapter = new KeyStoreAdapter(
                CngProvider.MicrosoftSoftwareKeyStorageProvider,
                KeySize);

            Assert.IsNull(await adapter.CreateRsaKeyAsync(
                KeyName,
                CngKeyUsages.Signing,
                false,
                null));
        }

        [Test]
        public async Task WhenKeyNotFoundAndCreateNewIsTrue_ThenGetRsaKeyAsyncReturnsNewKey()
        {
            var adapter = new KeyStoreAdapter(
                CngProvider.MicrosoftSoftwareKeyStorageProvider,
                KeySize);

            var key = await adapter.CreateRsaKeyAsync(
                KeyName,
                CngKeyUsages.Signing,
                true,
                null);

            Assert.IsNotNull(key);
            Assert.AreEqual("RSA", key.SignatureAlgorithm);
        }

        [Test]
        public async Task WhenKeyExists_ThenGetRsaKeyAsyncReturnsKey()
        {
            var adapter = new KeyStoreAdapter(
                CngProvider.MicrosoftSoftwareKeyStorageProvider,
                KeySize);

            var key1 = await adapter.CreateRsaKeyAsync(
                KeyName,
                CngKeyUsages.Signing,
                true,
                null);

            var key2 = await adapter.CreateRsaKeyAsync(
                KeyName,
                CngKeyUsages.Signing,
                false,
                null);

            Assert.IsNotNull(key2);
            Assert.AreEqual(key1.ExportParameters(false).Modulus, key2.ExportParameters(false).Modulus);
            Assert.AreEqual(key1.ExportParameters(false).Exponent, key2.ExportParameters(false).Exponent);
        }

        [Test]
        public async Task WhenKeyExistsButUsageDoesNotMatch_ThenGetRsaKeyAsyncReturnsKey()
        {
            var adapter = new KeyStoreAdapter(
                CngProvider.MicrosoftSoftwareKeyStorageProvider,
                KeySize);

            var key1 = await adapter.CreateRsaKeyAsync(
                KeyName,
                CngKeyUsages.Signing,
                true,
                null);

            AssertEx.ThrowsAggregateException<CryptographicException>(
                () => adapter.CreateRsaKeyAsync(
                    KeyName,
                    CngKeyUsages.Decryption,
                    false,
                    null).Wait());
        }
    }
}
