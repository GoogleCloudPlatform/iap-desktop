//
// Copyright 2023 Google LLC
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
using Google.Solutions.Platform;
using Google.Solutions.Testing.Apis.Platform;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Test.Host
{
    [TestFixture]
    public class TestInstall
    {
        private const string TestProfileName = "__Test";

        //---------------------------------------------------------------------
        // UserAgent.
        //---------------------------------------------------------------------

        [Test]
        public void UserAgent()
        {
            StringAssert.Contains("IAP-Desktop/", Install.UserAgent.ToString());
        }

        //---------------------------------------------------------------------
        // IsExecutingTests.
        //---------------------------------------------------------------------

        [Test]
        public void IsExecutingTests()
        {
            Assert.IsTrue(Install.IsExecutingTests);
        }

        //---------------------------------------------------------------------
        // CurrentVersion.
        //---------------------------------------------------------------------

        [Test]
        public void CurrentVersion()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);
                Assert.AreNotEqual(0, install.CurrentVersion.Major);
            }
        }

        //---------------------------------------------------------------------
        // UniqueId.
        //---------------------------------------------------------------------

        [Test]
        public void UniqueId()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);
                Assert.IsNotNull(install.UniqueId);
                Assert.AreEqual(16, install.UniqueId.Length);

                Assert.AreEqual(
                    install.UniqueId,
                    new Install(keyPath.Path).UniqueId);
            }
        }

        //---------------------------------------------------------------------
        // BaseKeyPath.
        //---------------------------------------------------------------------

        [Test]
        public void BaseKeyPath()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);
                Assert.AreEqual(keyPath.Path, install.BaseKeyPath);
            }
        }

        //---------------------------------------------------------------------
        // BaseDirectory.
        //---------------------------------------------------------------------

        [Test]
        public void BaseDirectory()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                Assert.IsNotNull(install.BaseDirectory);
                Assert.IsTrue(Directory.Exists(install.BaseDirectory));
            }
        }

        //---------------------------------------------------------------------
        // InitialVersion.
        //---------------------------------------------------------------------

        [Test]
        public void InitialVersion_WhenFreshlyInstalled_ThenInitialVersionIsCurrentVersion()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);
                Assert.AreEqual(install.CurrentVersion, install.InitialVersion);
            }
        }

        [Test]
        public void InitialVersion_WhenKeyMissing_ThenInitialVersionIsCurrentVersion()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);
                keyPath.Delete();

                Assert.AreEqual(install.CurrentVersion, install.InitialVersion);
            }
        }

        [Test]
        public void InitialVersion_WhenUpgraded_ThenInitialVersionOldestVersionEverInstalled()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                using (var key = keyPath.CreateKey())
                {
                    key.SetValue(
                        "InstalledVersionHistory",
                        new string[] { "0.0.3.0", "0.0.1.0", "0.0.2.0" },
                        RegistryValueKind.MultiString);
                }

                Assert.AreNotEqual(install.CurrentVersion, install.InitialVersion);
                Assert.AreEqual(new Version(0, 0, 1, 0), install.InitialVersion);
            }
        }

        //---------------------------------------------------------------------
        // PreviousVersion.
        //---------------------------------------------------------------------

        [Test]
        public void PreviousVersion_WhenFreshlyInstalled_ThenPreviousVersionIsNull()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);
                Assert.IsNull(install.PreviousVersion);
            }
        }

        [Test]
        public void PreviousVersion_WhenKeyMissing_ThenPreviousVersionIsNull()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);
                keyPath.Delete();

                Assert.IsNull(install.PreviousVersion);
            }
        }

        [Test]
        public void PreviousVersion_WhenUpgraded_ThenPreviousVersionIsSet()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                using (var key = keyPath.CreateKey())
                {
                    key.SetValue(
                        "InstalledVersionHistory",
                        new string[] { install.CurrentVersion.ToString(), "0.0.3.0", "0.0.1.0", "0.0.2.0" },
                        RegistryValueKind.MultiString);
                }

                Assert.AreEqual(new Version(0, 0, 3, 0), install.PreviousVersion);
            }
        }

        //---------------------------------------------------------------------
        // UserAgent.
        //---------------------------------------------------------------------

        [Test]
        public void UserAgent_IncludesPlatform()
        {
            StringAssert.Contains(
                Environment.OSVersion.VersionString,
                Install.UserAgent.Platform);
            StringAssert.Contains(
                $"{ProcessEnvironment.ProcessArchitecture.ToString().ToLower()}/",
                Install.UserAgent.Platform);
            StringAssert.Contains(
                $"/{ProcessEnvironment.NativeArchitecture.ToString().ToLower()}",
                Install.UserAgent.Platform);
        }

        //---------------------------------------------------------------------
        // CreateProfile.
        //---------------------------------------------------------------------

        [Test]
        public void CreateProfile_WhenProfileNameIsNotValid_ThenCreateProfileThrowsException()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                Assert.Throws<ArgumentException>(() => install.CreateProfile("Föö"));
            }
        }

        [Test]
        public void CreateProfile_WhenProfileNameIsNull_ThenCreateProfileThrowsException()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                Assert.Throws<ArgumentException>(() => install.CreateProfile(null!));
            }
        }

        [Test]
        public void CreateProfile_WhenProfileExists_ThenCreateProfileOpensProfile()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                install.CreateProfile(TestProfileName);
                using (var profile = install.CreateProfile(TestProfileName))
                {
                    Assert.IsNotNull(profile);
                    Assert.AreEqual(TestProfileName, profile.Name);
                }
            }
        }

        //---------------------------------------------------------------------
        // OpenProfile.
        //---------------------------------------------------------------------

        [Test]
        public void OpenProfile_WhenProfileNameIsNotValid_ThenOpenProfileThrowsException()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                Assert.Throws<ArgumentException>(() => install.OpenProfile("Föö"));
            }
        }

        [Test]
        public void OpenProfile_WhenProfileDoesNotExist_ThenOpenProfileThrowsException()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                Assert.Throws<ProfileNotFoundException>(
                () => install.OpenProfile("This does not exist"));
            }
        }

        [Test]
        public void OpenProfile_WhenProfileExists_ThenOpenProfileOpensProfile()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                using (install.CreateProfile(TestProfileName))
                { }

                using (var profile = install.OpenProfile(TestProfileName))
                {
                    Assert.IsNotNull(profile);
                    Assert.AreEqual(TestProfileName, profile.Name);
                    Assert.IsFalse(profile.IsDefault);
                    Assert.IsNotNull(profile.SettingsKey);
                }
            }
        }

        [Test]
        public void OpenProfile_WhenProfileNameIsNullThenOpenProfileReturnsDefaultProfile()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                var profile = install.OpenProfile(null);
                Assert.AreEqual("Default", profile.Name);
                Assert.IsTrue(profile.IsDefault);
            }
        }

        //---------------------------------------------------------------------
        // DeleteProfile.
        //---------------------------------------------------------------------

        [Test]
        public void DeleteProfile_WhenProfileDoesNotExist_ThenDeleteProfileDoesNothing()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                install.DeleteProfile("This does not exist");
            }
        }

        [Test]
        public void DeleteProfile_WhenProfileExists_ThenDeleteProfileDeletesProfile()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                using (install.CreateProfile(TestProfileName))
                { }

                install.DeleteProfile(TestProfileName);

                Assert.Throws<ProfileNotFoundException>(
                    () => install.OpenProfile(TestProfileName));
            }
        }

        //---------------------------------------------------------------------
        // ListProfiles.
        //---------------------------------------------------------------------

        [Test]
        public void ListProfiles_WhenProfileCreated_ThenListProfilesIncludesProfile()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                using (install.CreateProfile(TestProfileName))
                { }

                var list = install.Profiles;

                Assert.IsNotNull(list);
                CollectionAssert.Contains(list, TestProfileName);
            }
        }

        [Test]
        public void ListProfiles_WhenDefaultProfileCreated_ThenListProfilesIncludesDefaultProfile()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                using (install.OpenProfile(null))
                { }

                var list = install.Profiles;

                Assert.IsNotNull(list);
                CollectionAssert.Contains(list, "Default");
            }
        }

        [Test]
        public void ListProfiles_WhenNonProfileKeysPresent_ThenListProfilesIgnoresKeys()
        {
            using (var keyPath = RegistryKeyPath.ForCurrentTest())
            {
                var install = new Install(keyPath.Path);

                keyPath
                    .CreateKey()
                    .CreateSubKey($"{install.BaseKeyPath}\\________Notaprofile", true);

                using (install.OpenProfile(null))
                { }

                var list = install.Profiles;

                Assert.IsNotNull(list);
                Assert.IsFalse(list.Any(p => p.EndsWith("Notaprofile", StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
