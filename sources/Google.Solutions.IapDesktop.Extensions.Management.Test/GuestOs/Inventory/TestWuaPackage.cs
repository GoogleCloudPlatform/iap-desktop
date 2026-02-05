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
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.GuestOs.Inventory
{
    [TestFixture]
    public class TestWuaPackage
    {
        [Test]
        public void Properties_WhenFullyInitialized()
        {
            var package = (IPackage)new WuaPackage(
                "title",
                "description",
                new List<string>()
                {
                    "cat1",
                    "cat2"
                },
                new List<string>()
                {
                    "B54E7D24-7ADD-428F-8B75-90A396FA584F",
                    "9511D615-35B2-47BB-927F-F73D8E9260BB"
                },
                new List<string>()
                {
                    "kb0000001",
                    "kb0000002"
                },
                "http://microsoft.com/",
                "id",
                42,
                new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));

            Assert.That(package.PackageType, Is.EqualTo("FeaturePacks, Guidance"));
            Assert.That(package.PackageId, Is.EqualTo("title"));
            Assert.That(package.Description, Is.EqualTo("description"));
            Assert.That(package.Version, Is.EqualTo("42"));
            Assert.That(package.Weblink?.ToString(), Is.EqualTo("http://microsoft.com/"));
            Assert.That(package.PublishedOn, Is.EqualTo(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc)));
            Assert.IsNull(package.InstalledOn);
            Assert.That(package.Criticality, Is.EqualTo(PackageCriticality.NonCritical));
        }

        [Test]
        public void Properties_WhenBarelyInitialized()
        {
            var package = (IPackage)new WuaPackage(
                "title",
                null,
                null,
                null,
                null,
                null,
                null,
                0,
                null);

            Assert.That(package.PackageType, Is.EqualTo(""));
            Assert.That(package.PackageId, Is.EqualTo("title"));
            Assert.IsNull(package.Description);
            Assert.That(package.Version, Is.EqualTo("0"));
            Assert.IsNull(package.Weblink);
            Assert.IsNull(package.InstalledOn);
            Assert.That(package.Criticality, Is.EqualTo(PackageCriticality.NonCritical));
        }
    }
}
