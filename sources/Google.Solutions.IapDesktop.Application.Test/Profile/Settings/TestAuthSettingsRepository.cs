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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Security;
using Google.Solutions.Settings;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings
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
                settings.Credentials.GetClearTextValue());
        }

        //---------------------------------------------------------------------
        // TryRead.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBlobNullOrEmpty_ThenTryReadReturnsFalse(
            [Values(null, "", "{")] string value)
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            // Store value.
            var originalSettings = repository.GetSettings();
            originalSettings.Credentials.Value = SecureStringExtensions.FromClearText(value);
            repository.SetSettings(originalSettings);

            // Read.
            Assert.IsFalse(repository.TryRead(out var _));
        }

        [Test]
        public void WhenBlobContainsFullTokenResponse_ThenTryReadReturnsTrue()
        {
            var value = @"{
                'access_token':'ya29.a0A...',
                'token_type':'Bearer',
                'expires_in':3599,
                'refresh_token':'rt',
                'scope':'https://www.googleapis.com/auth/cloud-platform https://www.googleapis.com/auth/userinfo.email openid',
                'id_token':'idt',
                'Issued':'2023-07-29T09:15:08.643+10:00',
                'IssuedUtc':'2023-07-28T23:15:08.643Z'
                }";
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            // Store value.
            var originalSettings = repository.GetSettings();
            originalSettings.Credentials.Value = SecureStringExtensions.FromClearText(value);
            repository.SetSettings(originalSettings);

            // Read.
            Assert.IsTrue(repository.TryRead(out var offlineCredential));

            Assert.AreEqual("rt", offlineCredential.RefreshToken);
            Assert.AreEqual("idt", offlineCredential.IdToken);
            Assert.AreEqual(
                "https://www.googleapis.com/auth/cloud-platform https://www.googleapis.com/auth/userinfo.email openid",
                offlineCredential.Scope);
        }

        [Test]
        public void WhenBlobOnlyContainsGaiaRefreshToken_ThenTryReadReturnsTrue()
        {
            var value = @"{
                'refresh_token':'rt',
                'scope': 'openid'
                }";
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            // Store value.
            var originalSettings = repository.GetSettings();
            originalSettings.Credentials.Value = SecureStringExtensions.FromClearText(value);
            repository.SetSettings(originalSettings);

            // Read.
            Assert.IsTrue(repository.TryRead(out var offlineCredential));

            Assert.AreEqual(OidcIssuer.Gaia, offlineCredential.Issuer);
            Assert.AreEqual("rt", offlineCredential.RefreshToken);
            Assert.IsNull(offlineCredential.IdToken);
            Assert.AreEqual("openid", offlineCredential.Scope);
        }

        [Test]
        public void WhenBlobOnlyContainsStsRefreshToken_ThenTryReadReturnsTrue()
        {
            var value = @"{
                'refresh_token':'rt',
                'issuer': 'sts',
                'scope': 'openid'
                }";
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            // Store value.
            var originalSettings = repository.GetSettings();
            originalSettings.Credentials.Value = SecureStringExtensions.FromClearText(value);
            repository.SetSettings(originalSettings);

            // Read.
            Assert.IsTrue(repository.TryRead(out var offlineCredential));

            Assert.AreEqual(OidcIssuer.Sts, offlineCredential.Issuer);
            Assert.AreEqual("rt", offlineCredential.RefreshToken);
            Assert.IsNull(offlineCredential.IdToken);
            Assert.AreEqual("openid", offlineCredential.Scope);
        }

        //---------------------------------------------------------------------
        // Write.
        //---------------------------------------------------------------------

        [Test]
        public void WriteGaiaOfflineCredential()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            // Write.
            repository.Write(new OidcOfflineCredential(
                OidcIssuer.Gaia,
                "openid",
                "rt",
                "idt"));

            // Read again.
            Assert.IsTrue(repository.TryRead(out var offlineCredential));

            Assert.AreEqual(OidcIssuer.Gaia, offlineCredential.Issuer);
            Assert.AreEqual("rt", offlineCredential.RefreshToken);
            Assert.AreEqual("idt", offlineCredential.IdToken);
            Assert.AreEqual("openid", offlineCredential.Scope);
        }

        [Test]
        public void WriteStsOfflineCredential()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            // Write.
            repository.Write(new OidcOfflineCredential(
                OidcIssuer.Sts,
                "openid",
                "rt",
                null));

            // Read again.
            Assert.IsTrue(repository.TryRead(out var offlineCredential));

            Assert.AreEqual(OidcIssuer.Sts, offlineCredential.Issuer);
            Assert.AreEqual("rt", offlineCredential.RefreshToken);
            Assert.IsNull(offlineCredential.IdToken);
            Assert.AreEqual("openid", offlineCredential.Scope);
        }

        //---------------------------------------------------------------------
        // Clear.
        //---------------------------------------------------------------------

        [Test]
        public void Clear()
        {
            var baseKey = this.hkcu.CreateSubKey(TestKeyPath);
            var repository = new AuthSettingsRepository(baseKey);

            // Write & clear.
            repository.Write(new OidcOfflineCredential(
                OidcIssuer.Gaia, "openid", "rt", "idt"));
            repository.Clear();

            // Read again.
            Assert.IsFalse(repository.TryRead(out var _));
        }
    }
}