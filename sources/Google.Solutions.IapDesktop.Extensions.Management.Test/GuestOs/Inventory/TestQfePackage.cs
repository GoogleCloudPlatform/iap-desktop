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
using System;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.GuestOs.Inventory
{
    [TestFixture]
    public class TestQfePackage
    {
        [Test]
        public void Properties_WhenFullyInitialized()
        {
            var package = (IPackage)new QfePackage(
                "http://uri",
                "description",
                "kb123",
                new DateTime(2020, 1, 3, 4, 5, 6, DateTimeKind.Utc));

            Assert.That(package.PackageType, Is.EqualTo("Hotfix"));
            Assert.That(package.PackageId, Is.EqualTo("kb123"));
            Assert.That(package.Description, Is.EqualTo("description"));
            Assert.That(package.Version, Is.Null);
            Assert.That(package.Weblink?.ToString(), Is.EqualTo("http://uri/"));
            Assert.That(package.InstalledOn, Is.EqualTo(new DateTime(2020, 1, 3, 4, 5, 6, DateTimeKind.Utc)));
            Assert.That(package.Criticality, Is.EqualTo(PackageCriticality.NonCritical));
        }

        [Test]
        public void Properties_WhenBarelyInitialized()
        {
            var package = (IPackage)new QfePackage(
                "not.a.url",
                null,
                "kb123",
                null);

            Assert.That(package.PackageType, Is.EqualTo("Hotfix"));
            Assert.That(package.PackageId, Is.EqualTo("kb123"));
            Assert.That(package.Description, Is.Null);
            Assert.That(package.Version, Is.Null);
            Assert.That(package.Weblink, Is.Null);
            Assert.That(package.InstalledOn, Is.Null);
            Assert.That(package.Criticality, Is.EqualTo(PackageCriticality.NonCritical));
        }
    }
}
