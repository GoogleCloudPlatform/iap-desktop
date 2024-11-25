﻿//
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
using Google.Solutions.Testing.Apis.Platform;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Profile
{
    [TestFixture]
    public class TestUserProfile : ApplicationFixtureBase
    {
        private const string TestProfileName = "__Test";

        //---------------------------------------------------------------------
        // IsValidProfileName.
        //---------------------------------------------------------------------

        [Test]
        public void IsValidProfileName_WhenNameIsNullOrEmpty_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(UserProfile.IsValidProfileName(null));
            Assert.IsFalse(UserProfile.IsValidProfileName(string.Empty));
            Assert.IsFalse(UserProfile.IsValidProfileName(" "));
        }

        [Test]
        public void IsValidProfileName_WhenNameHasLeadingOrTrailingSpaces_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(UserProfile.IsValidProfileName(" foo"));
            Assert.IsFalse(UserProfile.IsValidProfileName("foo\t"));
        }

        [Test]
        public void IsValidProfileName_WhenNameContainsUmlauts_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(UserProfile.IsValidProfileName("Föö"));
        }

        [Test]
        public void IsValidProfileName_WhenNameIsTooLong_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(UserProfile.IsValidProfileName("This profile name is way too long"));
        }

        [Test]
        public void IsValidProfileName_WhenNameIsAlphanumeric_ThenIsValidProfileNameReturnsTrue()
        {
            Assert.IsTrue(UserProfile.IsValidProfileName("This is a valid name"));
        }

        [Test]
        public void IsValidProfileName_WhenNameIsDefault_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(UserProfile.IsValidProfileName(UserProfile.DefaultName.ToLower()));
        }

        //---------------------------------------------------------------------
        // SchemaVersion.
        //---------------------------------------------------------------------

        [Test]
        public void SchemaVersion_WhenDefaultProfileDoesNotExist_ThenSchemaVersionIsCurrent()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                using (var profile = install.OpenProfile(null))
                {
                    Assert.AreNotEqual(UserProfile.SchemaVersion.Initial, profile.Version);
                    Assert.AreEqual(UserProfile.SchemaVersion.Current, profile.Version);
                }
            }
        }

        [Test]
        public void SchemaVersion_WhenDefaultProfileExistsWithoutVersion_ThenSchemaVersionIsInitial()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                keyPath.CreateKey().CreateSubKey("1.0");

                using (var profile = install.OpenProfile(null))
                {
                    Assert.AreEqual(UserProfile.SchemaVersion.Initial, profile.Version);
                }
            }
        }

        [Test]
        public void SchemaVersion_WhenNewProfileCreated_ThenSchemaVersionIsCurrent()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                using (var profile = install.CreateProfile(TestProfileName))
                {
                    Assert.AreNotEqual(UserProfile.SchemaVersion.Initial, profile.Version);
                    Assert.AreEqual(UserProfile.SchemaVersion.Current, profile.Version);
                }
            }
        }

        [Test]
        public void SchemaVersion_WhenProfileLacksVersionValue_ThenSchemaVersionIsOne()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);
                using (var profile = install.CreateProfile(TestProfileName))
                {
                    profile.SettingsKey.DeleteValue("SchemaVersion");
                    Assert.AreEqual(UserProfile.SchemaVersion.Initial, profile.Version);
                }
            }
        }

        [Test]
        public void SchemaVersion_WhenProfileVersionInvalid_ThenSchemaVersionIsOne()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                using (var profile = install.CreateProfile(TestProfileName))
                {
                    profile.SettingsKey.DeleteValue("SchemaVersion");
                    profile.SettingsKey.SetValue("SchemaVersion", "junk");
                    Assert.AreEqual(UserProfile.SchemaVersion.Initial, profile.Version);
                }
            }
        }
    }
}
