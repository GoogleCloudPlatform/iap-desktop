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
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Test.Host
{
    [TestFixture]
    public class TestProfile : ApplicationFixtureBase
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
            Assert.IsFalse(Profile.IsValidProfileName(null));
            Assert.IsFalse(Profile.IsValidProfileName(string.Empty));
            Assert.IsFalse(Profile.IsValidProfileName(" "));
        }

        [Test]
        public void WhenNameHasLeadingOrTrailingSpaces_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(Profile.IsValidProfileName(" foo"));
            Assert.IsFalse(Profile.IsValidProfileName("foo\t"));
        }

        [Test]
        public void WhenNameContainsUmlauts_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(Profile.IsValidProfileName("Föö"));
        }

        [Test]
        public void WhenNameIsTooLong_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(Profile.IsValidProfileName("This profile name is way too long"));
        }

        [Test]
        public void WhenNameIsAlphanumeric_ThenIsValidProfileNameReturnsTrue()
        {
            Assert.IsTrue(Profile.IsValidProfileName("This is a valid name"));
        }

        [Test]
        public void WhenNameIsDefault_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(Profile.IsValidProfileName(Profile.DefaultProfileName.ToLower()));
        }

        //---------------------------------------------------------------------
        // CreateProfile.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileNameIsNotValid_ThenCreateProfileThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => Profile.CreateProfile(CreateInstall(), "Föö"));
        }

        [Test]
        public void WhenProfileNameIsNull_ThenCreateProfileThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => Profile.CreateProfile(CreateInstall(), null));
        }

        [Test]
        public void WhenProfileExists_ThenCreateProfileOpensProfile()
        {
            Profile.CreateProfile(CreateInstall(), TestProfileName);
            using (var profile = Profile.CreateProfile(CreateInstall(), TestProfileName))
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
                () => Profile.OpenProfile(CreateInstall(), "Föö"));
        }

        [Test]
        public void WhenProfileDoesNotExist_ThenOpenProfileThrowsException()
        {
            Assert.Throws<ProfileNotFoundException>(
                () => Profile.OpenProfile(CreateInstall(), "This does not exist"));
        }

        [Test]
        public void WhenProfileExists_ThenOpenProfileOpensProfile()
        {
            using (Profile.CreateProfile(CreateInstall(), TestProfileName))
            { }

            using (var profile = Profile.OpenProfile(CreateInstall(), TestProfileName))
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
            var profile = Profile.OpenProfile(CreateInstall(), null);
            Assert.AreEqual("Default", profile.Name);
            Assert.IsTrue(profile.IsDefault);
        }

        //---------------------------------------------------------------------
        // DeleteProfile.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileDoesNotExist_ThenDeleteProfileDoesNothing()
        {
            Profile.DeleteProfile(CreateInstall(), "This does not exist");
        }

        [Test]
        public void WhenProfileExists_ThenDeleteProfileDeletesProfile()
        {
            using (Profile.CreateProfile(CreateInstall(), TestProfileName))
            { }

            Profile.DeleteProfile(CreateInstall(), TestProfileName);

            Assert.Throws<ProfileNotFoundException>(
                () => Profile.OpenProfile(CreateInstall(), TestProfileName));
        }

        //---------------------------------------------------------------------
        // ListProfiles.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileCreated_ThenListProfilesIncludesProfile()
        {
            using (Profile.CreateProfile(CreateInstall(), TestProfileName))
            { }

            var list = Profile.ListProfiles(CreateInstall());

            Assert.IsNotNull(list);
            CollectionAssert.Contains(list, TestProfileName);
        }

        [Test]
        public void WhenDefaultProfileCreated_ThenListProfilesIncludesDefaultProfile()
        {
            using (Profile.OpenProfile(CreateInstall(), null))
            { }

            var list = Profile.ListProfiles(CreateInstall());

            Assert.IsNotNull(list);
            CollectionAssert.Contains(list, "Default");
        }

        [Test]
        public void WhenNonProfileKeysPresent_ThenListProfilesIgnoresKeys()
        {
            var install = CreateInstall();

            this.hkcu.CreateSubKey($"{install.BaseKeyPath}\\________Notaprofile", true);

            using (Profile.OpenProfile(install, null))
            { }

            var list = Profile.ListProfiles(CreateInstall());

            Assert.IsNotNull(list);
            Assert.IsFalse(list.Any(p => p.EndsWith("Notaprofile", StringComparison.OrdinalIgnoreCase)));
        }

        //---------------------------------------------------------------------
        // SchemaVersion.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDefaultProfileDoesNotExist_ThenSchemaVersionIsCurrent()
        {
            using (var profile = Profile.OpenProfile(CreateInstall(), null))
            {
                Assert.AreNotEqual(Profile.SchemaVersion.Initial, profile.Version);
                Assert.AreEqual(Profile.SchemaVersion.Current, profile.Version);
            }
        }

        [Test]
        public void WhenDefaultProfileExistsWithoutVersion_ThenSchemaVersionIsInitial()
        {
            var install = CreateInstall();

            this.hkcu.CreateSubKey($@"{install.BaseKeyPath}\1.0");
            using (var profile = Profile.OpenProfile(install, null))
            {
                Assert.AreEqual(Profile.SchemaVersion.Initial, profile.Version);
            }
        }

        [Test]
        public void WhenNewProfileCreated_ThenSchemaVersionIsCurrent()
        {
            using (var profile = Profile.CreateProfile(CreateInstall(), TestProfileName))
            {
                Assert.AreNotEqual(Profile.SchemaVersion.Initial, profile.Version);
                Assert.AreEqual(Profile.SchemaVersion.Current, profile.Version);
            }
        }

        [Test]
        public void WhenProfileLacksVersionValue_ThenSchemaVersionIsOne()
        {
            using (var profile = Profile.CreateProfile(CreateInstall(), TestProfileName))
            {
                profile.SettingsKey.DeleteValue("SchemaVersion");
                Assert.AreEqual(Profile.SchemaVersion.Initial, profile.Version);
            }
        }

        [Test]
        public void WhenProfileVersionInvalid_ThenSchemaVersionIsOne()
        {
            using (var profile = Profile.CreateProfile(CreateInstall(), TestProfileName))
            {
                profile.SettingsKey.DeleteValue("SchemaVersion");
                profile.SettingsKey.SetValue("SchemaVersion", "junk");
                Assert.AreEqual(Profile.SchemaVersion.Initial, profile.Version);
            }
        }
    }
}
