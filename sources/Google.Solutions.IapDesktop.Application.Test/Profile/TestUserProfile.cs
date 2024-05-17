//
// Copyright 2022 Google LLC
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

using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Test.Profile
{
    [TestFixture]
    public class TestUserProfile : ApplicationFixtureBase
    {
        private const string TestProfilesKeyPath = @"Software\Google\__Test";
        private const string TestProfileName = "__Test";

        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestProfilesKeyPath, false);
            using (this.hkcu.CreateSubKey(TestProfilesKeyPath, false))
            { }
        }

        private Install CreateInstall()
        {
            return new Install(TestProfilesKeyPath);
        }

        //---------------------------------------------------------------------
        // IsValidProfileName.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNameIsNullOrEmpty_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(UserProfile.IsValidProfileName(null));
            Assert.IsFalse(UserProfile.IsValidProfileName(string.Empty));
            Assert.IsFalse(UserProfile.IsValidProfileName(" "));
        }

        [Test]
        public void WhenNameHasLeadingOrTrailingSpaces_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(UserProfile.IsValidProfileName(" foo"));
            Assert.IsFalse(UserProfile.IsValidProfileName("foo\t"));
        }

        [Test]
        public void WhenNameContainsUmlauts_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(UserProfile.IsValidProfileName("Föö"));
        }

        [Test]
        public void WhenNameIsTooLong_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(UserProfile.IsValidProfileName("This profile name is way too long"));
        }

        [Test]
        public void WhenNameIsAlphanumeric_ThenIsValidProfileNameReturnsTrue()
        {
            Assert.IsTrue(UserProfile.IsValidProfileName("This is a valid name"));
        }

        [Test]
        public void WhenNameIsDefault_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(UserProfile.IsValidProfileName(UserProfile.DefaultName.ToLower()));
        }

        //---------------------------------------------------------------------
        // CreateProfile.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileNameIsNotValid_ThenCreateProfileThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => UserProfile.CreateProfile(CreateInstall(), "Föö"));
        }

        [Test]
        public void WhenProfileNameIsNull_ThenCreateProfileThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => UserProfile.CreateProfile(CreateInstall(), null!));
        }

        [Test]
        public void WhenProfileExists_ThenCreateProfileOpensProfile()
        {
            UserProfile.CreateProfile(CreateInstall(), TestProfileName);
            using (var profile = UserProfile.CreateProfile(CreateInstall(), TestProfileName))
            {
                Assert.IsNotNull(profile);
                Assert.AreEqual(TestProfileName, profile.Name);
            }
        }

        //---------------------------------------------------------------------
        // OpenProfile.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileNameIsNotValid_ThenOpenProfileThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => UserProfile.OpenProfile(CreateInstall(), "Föö"));
        }

        [Test]
        public void WhenProfileDoesNotExist_ThenOpenProfileThrowsException()
        {
            Assert.Throws<ProfileNotFoundException>(
                () => UserProfile.OpenProfile(CreateInstall(), "This does not exist"));
        }

        [Test]
        public void WhenProfileExists_ThenOpenProfileOpensProfile()
        {
            using (UserProfile.CreateProfile(CreateInstall(), TestProfileName))
            { }

            using (var profile = UserProfile.OpenProfile(CreateInstall(), TestProfileName))
            {
                Assert.IsNotNull(profile);
                Assert.AreEqual(TestProfileName, profile.Name);
                Assert.IsFalse(profile.IsDefault);
                Assert.IsNotNull(profile.SettingsKey);
            }
        }

        [Test]
        public void WhenProfileNameIsNullThenOpenProfileReturnsDefaultProfile()
        {
            var profile = UserProfile.OpenProfile(CreateInstall(), null);
            Assert.AreEqual("Default", profile.Name);
            Assert.IsTrue(profile.IsDefault);
        }

        //---------------------------------------------------------------------
        // DeleteProfile.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileDoesNotExist_ThenDeleteProfileDoesNothing()
        {
            UserProfile.DeleteProfile(CreateInstall(), "This does not exist");
        }

        [Test]
        public void WhenProfileExists_ThenDeleteProfileDeletesProfile()
        {
            using (UserProfile.CreateProfile(CreateInstall(), TestProfileName))
            { }

            UserProfile.DeleteProfile(CreateInstall(), TestProfileName);

            Assert.Throws<ProfileNotFoundException>(
                () => UserProfile.OpenProfile(CreateInstall(), TestProfileName));
        }

        //---------------------------------------------------------------------
        // ListProfiles.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileCreated_ThenListProfilesIncludesProfile()
        {
            using (UserProfile.CreateProfile(CreateInstall(), TestProfileName))
            { }

            var list = UserProfile.ListProfiles(CreateInstall());

            Assert.IsNotNull(list);
            CollectionAssert.Contains(list, TestProfileName);
        }

        [Test]
        public void WhenDefaultProfileCreated_ThenListProfilesIncludesDefaultProfile()
        {
            using (UserProfile.OpenProfile(CreateInstall(), null))
            { }

            var list = UserProfile.ListProfiles(CreateInstall());

            Assert.IsNotNull(list);
            CollectionAssert.Contains(list, "Default");
        }

        [Test]
        public void WhenNonProfileKeysPresent_ThenListProfilesIgnoresKeys()
        {
            var install = CreateInstall();

            this.hkcu.CreateSubKey($"{install.BaseKeyPath}\\________Notaprofile", true);

            using (UserProfile.OpenProfile(install, null))
            { }

            var list = UserProfile.ListProfiles(CreateInstall());

            Assert.IsNotNull(list);
            Assert.IsFalse(list.Any(p => p.EndsWith("Notaprofile", StringComparison.OrdinalIgnoreCase)));
        }

        //---------------------------------------------------------------------
        // SchemaVersion.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDefaultProfileDoesNotExist_ThenSchemaVersionIsCurrent()
        {
            using (var profile = UserProfile.OpenProfile(CreateInstall(), null))
            {
                Assert.AreNotEqual(UserProfile.SchemaVersion.Initial, profile.Version);
                Assert.AreEqual(UserProfile.SchemaVersion.Current, profile.Version);
            }
        }

        [Test]
        public void WhenDefaultProfileExistsWithoutVersion_ThenSchemaVersionIsInitial()
        {
            var install = CreateInstall();

            this.hkcu.CreateSubKey($@"{install.BaseKeyPath}\1.0");
            using (var profile = UserProfile.OpenProfile(install, null))
            {
                Assert.AreEqual(UserProfile.SchemaVersion.Initial, profile.Version);
            }
        }

        [Test]
        public void WhenNewProfileCreated_ThenSchemaVersionIsCurrent()
        {
            using (var profile = UserProfile.CreateProfile(CreateInstall(), TestProfileName))
            {
                Assert.AreNotEqual(UserProfile.SchemaVersion.Initial, profile.Version);
                Assert.AreEqual(UserProfile.SchemaVersion.Current, profile.Version);
            }
        }

        [Test]
        public void WhenProfileLacksVersionValue_ThenSchemaVersionIsOne()
        {
            using (var profile = UserProfile.CreateProfile(CreateInstall(), TestProfileName))
            {
                profile.SettingsKey.DeleteValue("SchemaVersion");
                Assert.AreEqual(UserProfile.SchemaVersion.Initial, profile.Version);
            }
        }

        [Test]
        public void WhenProfileVersionInvalid_ThenSchemaVersionIsOne()
        {
            using (var profile = UserProfile.CreateProfile(CreateInstall(), TestProfileName))
            {
                profile.SettingsKey.DeleteValue("SchemaVersion");
                profile.SettingsKey.SetValue("SchemaVersion", "junk");
                Assert.AreEqual(UserProfile.SchemaVersion.Initial, profile.Version);
            }
        }
    }
}
