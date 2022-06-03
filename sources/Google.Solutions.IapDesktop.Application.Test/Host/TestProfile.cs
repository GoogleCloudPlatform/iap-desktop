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
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Host
{
    [TestFixture]
    public class TestProfile : ApplicationFixtureBase
    {
        private const string TestProfileName = "__Test";

        [SetUp]
        public void SetUp()
        {
            Profile.DeleteProfile(TestProfileName);
        }

        //---------------------------------------------------------------------
        // IsValidProfileName.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNameIsNullOrEmpty_ThenIsValidProfileNameReturnsFalse()
        {
            Assert.IsFalse(Profile.IsValidProfileName(null));
            Assert.IsFalse(Profile.IsValidProfileName(String.Empty));
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
            Assert.IsFalse(Profile.IsValidProfileName("This is a valid name"));
        }

        //---------------------------------------------------------------------
        // CreateProfile.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileNameIsNotValid_ThenCreateProfileThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => Profile.CreateProfile("Föö"));
        }

        [Test]
        public void WhenProfileExists_ThenCreateProfileOpensProfile()
        {
            Profile.CreateProfile(TestProfileName);
            using (var profile = Profile.CreateProfile(TestProfileName))
            {
                Assert.IsNotNull(profile);
            }
        }

        //---------------------------------------------------------------------
        // OpenProfile.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileNameIsNotValid_ThenOpenProfileThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => Profile.OpenProfile("Föö"));
        }

        [Test]
        public void WhenProfileDoesNotExist_ThenOpenProfileThrowsException()
        {
            Assert.Throws<ArgumentException>(
                () => Profile.OpenProfile("This does not exist"));
        }

        [Test]
        public void WhenProfileExists_ThenOpenProfileOpensProfile()
        {
            using (Profile.CreateProfile(TestProfileName))
            { }

            using (var profile = Profile.OpenProfile(TestProfileName))
            {
                Assert.IsNotNull(profile);
            }
        }

        //---------------------------------------------------------------------
        // DeleteProfile.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileDoesNotExist_ThenDeleteProfileDoesNothing()
        {
            Profile.DeleteProfile("This does not exist");
        }

        [Test]
        public void WhenProfileExists_ThenDeleteProfileDeletesProfile()
        {
            using (Profile.CreateProfile(TestProfileName))
            { }
            
            Profile.DeleteProfile(TestProfileName);

            Assert.Throws<ArgumentException>(
                () => Profile.OpenProfile(TestProfileName));
        }

        //---------------------------------------------------------------------
        // ListProfiles.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProfileCreated_ThenListProfilesIncludesProfile()
        {
            Profile.CreateProfile(TestProfileName);
            var list = Profile.ListProfiles();

            Assert.IsNotNull(list);
            CollectionAssert.Contains(list, TestProfileName);
        }
    }
}
