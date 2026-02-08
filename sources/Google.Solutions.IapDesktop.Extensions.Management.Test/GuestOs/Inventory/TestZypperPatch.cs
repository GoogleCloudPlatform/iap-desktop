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

using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.GuestOs.Inventory
{
    [TestFixture]
    public class TestZypperPatch
    {
        [Test]
        public void Properties_WhenFullyInitialized()
        {
            var package = (IPackage)new ZypperPatch(
                "title",
                "category",
                "severity",
                "summary");

            Assert.That(package.PackageType, Is.EqualTo("Patch"));
            Assert.That(package.PackageId, Is.EqualTo("title"));
            Assert.That(package.Description, Is.EqualTo("summary (category)"));
            Assert.That(package.Version, Is.Null);
            Assert.That(package.Weblink, Is.Null);
            Assert.That(package.InstalledOn, Is.Null);
            Assert.That(package.Criticality, Is.EqualTo(PackageCriticality.NonCritical));
        }

        [Test]
        public void Properties_WhenBarelyInitialized()
        {
            var package = (IPackage)new ZypperPatch(
                "title",
                null,
                null,
                null);

            Assert.That(package.PackageType, Is.EqualTo("Patch"));
            Assert.That(package.PackageId, Is.EqualTo("title"));
            Assert.That(package.Description, Is.EqualTo(""));
            Assert.That(package.Version, Is.Null);
            Assert.That(package.Weblink, Is.Null);
            Assert.That(package.InstalledOn, Is.Null);
            Assert.That(package.Criticality, Is.EqualTo(PackageCriticality.NonCritical));
        }
    }
}
