//
// Copyright 2019 Google LLC
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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System;

namespace Google.Solutions.Apis.Test.Locator
{
    [TestFixture]
    public class TestLicenseLocator
        : EquatableFixtureBase<LicenseLocator, LicenseLocator>
    {
        protected override LicenseLocator CreateInstance()
        {
            return new LicenseLocator("project-1", "type-1");
        }

        [Test]
        public void Project()
        {
            var ref1 = new LicenseLocator("project-1", "type-1");
            Assert.AreEqual(ref1.ProjectId, ref1.Project.Name);
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPathIsValid_ParseReturnsObject()
        {
            var ref1 = LicenseLocator.Parse(
                "projects/project-1/global/licenses/windows-10-enterprise-byol");

            Assert.AreEqual("licenses", ref1.ResourceType);
            Assert.AreEqual("windows-10-enterprise-byol", ref1.Name);
            Assert.AreEqual("project-1", ref1.ProjectId);
        }

        [Test]
        public void WhenQualifiedByComputeGoogleapisHost_ParseReturnsObject()
        {
            var ref1 = LicenseLocator.Parse(
                "https://compute.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-10-enterprise-byol");

            Assert.AreEqual("licenses", ref1.ResourceType);
            Assert.AreEqual("windows-10-enterprise-byol", ref1.Name);
            Assert.AreEqual("windows-cloud", ref1.ProjectId);
        }

        [Test]
        public void WhenQualifiedByGoogleapisHost_ParseReturnsObject()
        {
            var ref1 = LicenseLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-10-enterprise-byol");

            Assert.AreEqual("licenses", ref1.ResourceType);
            Assert.AreEqual("windows-10-enterprise-byol", ref1.Name);
            Assert.AreEqual("windows-cloud", ref1.ProjectId);
        }

        [Test]
        public void WhenPathLacksProject_ParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => LicenseLocator.Parse(
                "/project-1/project-1/global/licenses/windows-10-enterprise-byol"));
        }

        [Test]
        public void WhenPathInvalid_ParseThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => LicenseLocator.Parse(
                "projects/project-1/notglobal/licenses/windows-10-enterprise-byol"));
            Assert.Throws<ArgumentException>(() => LicenseLocator.Parse(
                "/project-1/global/licenses/windows-10-enterprise-byol"));
            Assert.Throws<ArgumentException>(() => LicenseLocator.Parse(
                "/"));
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        [Test]
        public void WhenObjectsNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new LicenseLocator("proj-1", "windows-10-enterprise-byol");
            var ref2 = new LicenseLocator("proj-2", "windows-10-enterprise-byol");

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object?)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/global/licenses/windows-10-enterprise-byol";

            Assert.AreEqual(
                path,
                LicenseLocator.Parse(path).ToString());
        }

        [Test]
        public void WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/project-1/global/licenses/windows-10-enterprise-byol";

            Assert.AreEqual(
                path,
                LicenseLocator.Parse(
                    "https://www.googleapis.com/compute/v1/" + path).ToString());
        }

        //---------------------------------------------------------------------
        // IsWindowsLicense.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLicenseIsFromWindowsCloud_ThenIsWindowsLicenseReturnsTrue()
        {
            var locator = LicenseLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-10-enterprise-byol");
            Assert.IsTrue(locator.IsWindowsLicense());
        }

        [Test]
        public void WhenLicenseIsNotFromWindowsCloud_ThenIsWindowsLicenseReturnsFalse()
        {
            var locator = LicenseLocator.Parse(
                "projects/my-project/global/licenses/windows-10-enterprise-byol");
            Assert.IsFalse(locator.IsWindowsLicense());
        }

        [Test]
        public void WhenLicenseHasByolSuffix_ThenIsWindowsByolLicenseReturnsTrue()
        {
            var locator = LicenseLocator.Parse(
                "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-10-enterprise-byol");
            Assert.IsTrue(locator.IsWindowsByolLicense());
        }

        [Test]
        public void WhenLicenseHasNoByolSuffix_ThenIsWindowsByolLicenseReturnsFalse()
        {
            var locator = LicenseLocator.Parse(
                "projects/windows-cloud/global/licenses/windows-2016");
            Assert.IsFalse(locator.IsWindowsByolLicense());
        }
    }
}
