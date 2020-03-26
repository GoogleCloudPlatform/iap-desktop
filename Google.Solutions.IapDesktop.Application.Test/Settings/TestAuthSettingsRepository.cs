//
// Copyright 2010 Google LLC
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

using Google.Solutions.IapDesktop.Application.Registry;
using Google.Solutions.IapDesktop.Application.Settings;
using Microsoft.Win32;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Settings
{
    [TestFixture]
    public class TestAuthSettingsRepository
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);


        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
        }

        [Test]
        public void WhenBaseKeyIsEmpty_SettingsAreEmpty()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            var settings = repository.GetSettings();

            Assert.IsNull(settings.Credentials);
        }


        [Test]
        public void WhenSettingsSaved_GetSettingsReturnsData()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            var originalSettings = new AuthSettings()
            {
                Credentials = SecureStringExtensions.FromClearText("secure")
            };

            repository.SetSettings(originalSettings);

            var settings = repository.GetSettings();

            Assert.AreEqual(
                "secure",
                settings.Credentials.AsClearText());
        }


        //---------------------------------------------------------------------
        // IDataStore.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDeleteAsyncWithUnknownKey_KeyNotFoundExceptionThrown()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            AssertEx.ThrowsAggregateException<KeyNotFoundException>(() =>
            {
                repository.DeleteAsync<string>("invalidkey");
            });
        }

        [Test]
        public void WhenGetAsyncWithUnknownKey_KeyNotFoundExceptionThrown()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            AssertEx.ThrowsAggregateException<KeyNotFoundException>(() =>
            {
                repository.GetAsync<string>("invalidkey");
            });
        }

        [Test]
        public void WhenStoreAsyncWithUnknownKey_KeyNotFoundExceptionThrown()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            AssertEx.ThrowsAggregateException<KeyNotFoundException>(() =>
            {
                repository.StoreAsync<string>("invalidkey", null);
            });
        }

        [Test]
        public async Task WhenStoreWithValidKey_GetReturnsSameData()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            await repository.StoreAsync<string>(repository.CredentialStoreKey, "test");

            Assert.AreEqual(
                "test",
                await repository.GetAsync<string>(repository.CredentialStoreKey));
        }

        [Test]
        public async Task WhenStoreWithValidKeyAndClear_GetReturnsNull()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            await repository.StoreAsync<string>(repository.CredentialStoreKey, "test");
            await repository.ClearAsync();

            Assert.IsNull(await repository.GetAsync<string>(repository.CredentialStoreKey));
        }
    }
}
