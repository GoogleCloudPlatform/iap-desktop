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
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Settings;
using Google.Solutions.Testing.Apis.Platform;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Profile.Settings
{
    [TestFixture]
    public class TestAuthSettingsRepository : ApplicationFixtureBase
    {
        [Test]
        public void GetSettings_WhenBaseKeyIsEmpty_SettingsAreEmpty()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new AuthSettingsRepository(settingsPath.CreateKey());
                var settings = repository.GetSettings();

                Assert.IsNull(settings.Credentials.Value);
            }
        }

        [Test]
        public void GetSettings_WhenSettingsSaved_GetSettingsReturnsData()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new AuthSettingsRepository(settingsPath.CreateKey());

                var originalSettings = repository.GetSettings();
                originalSettings.Credentials.Value = SecureStringExtensions.FromClearText("secure");
                repository.SetSettings(originalSettings);

                var settings = repository.GetSettings();

                Assert.That(
                    settings.Credentials.GetClearTextValue(), Is.EqualTo("secure"));
            }
        }

        //---------------------------------------------------------------------
        // TryRead.
        //---------------------------------------------------------------------

        [Test]
        public void TryRead_WhenBlobNullOrEmpty_ThenTryReadReturnsFalse(
            [Values(null, "", "{")] string value)
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new AuthSettingsRepository(settingsPath.CreateKey());

                // Store value.
                var originalSettings = repository.GetSettings();
                originalSettings.Credentials.Value = SecureStringExtensions.FromClearText(value);
                repository.SetSettings(originalSettings);

                // Read.
                Assert.IsFalse(repository.TryRead(out var _));
            }
        }

        [Test]
        public void TryRead_WhenBlobContainsFullTokenResponse_ThenTryReadReturnsTrue()
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

            using (var settingsPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new AuthSettingsRepository(settingsPath.CreateKey());

                // Store value.
                var originalSettings = repository.GetSettings();
                originalSettings.Credentials.Value = SecureStringExtensions.FromClearText(value);
                repository.SetSettings(originalSettings);

                // Read.
                Assert.IsTrue(repository.TryRead(out var offlineCredential));

                Assert.IsNotNull(offlineCredential);
                Assert.That(offlineCredential!.RefreshToken, Is.EqualTo("rt"));
                Assert.That(offlineCredential.IdToken, Is.EqualTo("idt"));
                Assert.That(
                    offlineCredential.Scope, Is.EqualTo("https://www.googleapis.com/auth/cloud-platform https://www.googleapis.com/auth/userinfo.email openid"));
            }
        }

        [Test]
        public void TryRead_WhenBlobOnlyContainsGaiaRefreshToken_ThenTryReadReturnsTrue()
        {
            var value = @"{
                'refresh_token':'rt',
                'scope': 'openid'
                }";

            using (var settingsPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new AuthSettingsRepository(settingsPath.CreateKey());

                // Store value.
                var originalSettings = repository.GetSettings();
                originalSettings.Credentials.Value = SecureStringExtensions.FromClearText(value);
                repository.SetSettings(originalSettings);

                // Read.
                Assert.IsTrue(repository.TryRead(out var offlineCredential));

                Assert.IsNotNull(offlineCredential);
                Assert.That(offlineCredential!.Issuer, Is.EqualTo(OidcIssuer.Gaia));
                Assert.That(offlineCredential.RefreshToken, Is.EqualTo("rt"));
                Assert.IsNull(offlineCredential.IdToken);
                Assert.That(offlineCredential.Scope, Is.EqualTo("openid"));
            }
        }

        [Test]
        public void TryRead_WhenBlobOnlyContainsStsRefreshToken_ThenTryReadReturnsTrue()
        {
            var value = @"{
                'refresh_token':'rt',
                'issuer': 'sts',
                'scope': 'openid'
                }";

            using (var settingsPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new AuthSettingsRepository(settingsPath.CreateKey());

                // Store value.
                var originalSettings = repository.GetSettings();
                originalSettings.Credentials.Value = SecureStringExtensions.FromClearText(value);
                repository.SetSettings(originalSettings);

                // Read.
                Assert.IsTrue(repository.TryRead(out var offlineCredential));

                Assert.IsNotNull(offlineCredential);
                Assert.That(offlineCredential!.Issuer, Is.EqualTo(OidcIssuer.Sts));
                Assert.That(offlineCredential.RefreshToken, Is.EqualTo("rt"));
                Assert.IsNull(offlineCredential.IdToken);
                Assert.That(offlineCredential.Scope, Is.EqualTo("openid"));
            }
        }

        //---------------------------------------------------------------------
        // Write.
        //---------------------------------------------------------------------

        [Test]
        public void Write_GaiaOfflineCredential()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new AuthSettingsRepository(settingsPath.CreateKey());

                // Write.
                repository.Write(new OidcOfflineCredential(
                    OidcIssuer.Gaia,
                    "openid",
                    "rt",
                    "idt"));

                // Read again.
                Assert.IsTrue(repository.TryRead(out var offlineCredential));

                Assert.IsNotNull(offlineCredential);
                Assert.That(offlineCredential!.Issuer, Is.EqualTo(OidcIssuer.Gaia));
                Assert.That(offlineCredential.RefreshToken, Is.EqualTo("rt"));
                Assert.That(offlineCredential.IdToken, Is.EqualTo("idt"));
                Assert.That(offlineCredential.Scope, Is.EqualTo("openid"));
            }
        }

        [Test]
        public void Write_StsOfflineCredential()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new AuthSettingsRepository(settingsPath.CreateKey());

                // Write.
                repository.Write(new OidcOfflineCredential(
                    OidcIssuer.Sts,
                    "openid",
                    "rt",
                    null));

                // Read again.
                Assert.IsTrue(repository.TryRead(out var offlineCredential));

                Assert.IsNotNull(offlineCredential);
                Assert.That(offlineCredential!.Issuer, Is.EqualTo(OidcIssuer.Sts));
                Assert.That(offlineCredential.RefreshToken, Is.EqualTo("rt"));
                Assert.IsNull(offlineCredential.IdToken);
                Assert.That(offlineCredential.Scope, Is.EqualTo("openid"));
            }
        }

        //---------------------------------------------------------------------
        // Clear.
        //---------------------------------------------------------------------

        [Test]
        public void Clear()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest())
            {
                var repository = new AuthSettingsRepository(settingsPath.CreateKey());

                // Write & clear.
                repository.Write(new OidcOfflineCredential(
                    OidcIssuer.Gaia, "openid", "rt", "idt"));
                repository.Clear();

                // Read again.
                Assert.IsFalse(repository.TryRead(out var _));
            }
        }
    }
}