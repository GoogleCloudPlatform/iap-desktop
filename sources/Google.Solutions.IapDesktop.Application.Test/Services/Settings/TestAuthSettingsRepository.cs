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

using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Settings
{
    [TestFixture]
    public class TestAuthSettingsRepository : ApplicationFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
        }

        [Test]
        public void WhenBaseKeyIsEmpty_SettingsAreEmpty()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            var settings = repository.GetSettings();

            Assert.IsNull(settings.Credentials.Value);
        }

        [Test]
        public void WhenSettingsSaved_GetSettingsReturnsData()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            var originalSettings = repository.GetSettings();
            originalSettings.Credentials.Value = SecureStringExtensions.FromClearText("secure");
            repository.SetSettings(originalSettings);

            var settings = repository.GetSettings();

            Assert.AreEqual(
                "secure",
                settings.Credentials.ClearTextValue);
        }

        //---------------------------------------------------------------------
        // IDataStore.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDeleteAsyncWithUnknownKey_KeyNotFoundExceptionThrown()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            ExceptionAssert.ThrowsAggregateException<KeyNotFoundException>(() =>
            {
                repository.DeleteAsync<string>("invalidkey");
            });
        }

        [Test]
        public void WhenGetAsyncWithUnknownKey_KeyNotFoundExceptionThrown()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            ExceptionAssert.ThrowsAggregateException<KeyNotFoundException>(() =>
            {
                repository.GetAsync<string>("invalidkey");
            });
        }

        [Test]
        public void WhenStoreAsyncWithUnknownKey_KeyNotFoundExceptionThrown()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            ExceptionAssert.ThrowsAggregateException<KeyNotFoundException>(() =>
            {
                repository.StoreAsync<string>("invalidkey", null);
            });
        }

        [Test]
        public async Task WhenStoreWithValidKey_GetReturnsSameData()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            await repository
                .StoreAsync<string>(repository.CredentialStoreKey, "test")
                .ConfigureAwait(false);

            Assert.AreEqual(
                "test",
                await repository
                    .GetAsync<string>(repository.CredentialStoreKey)
                    .ConfigureAwait(false));
        }

        [Test]
        public async Task WhenStoreWithValidKeyAndClear_GetReturnsNull()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            await repository
                .StoreAsync<string>(repository.CredentialStoreKey, "test")
                .ConfigureAwait(false);
            await repository
                .ClearAsync()
                .ConfigureAwait(false);

            Assert.IsNull(await repository
                .GetAsync<string>(repository.CredentialStoreKey)
                .ConfigureAwait(false));
        }
    }
}
