﻿//
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

using Google.Solutions.IapDesktop.Extensions.Os.Inventory;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Os.Test.Inventory
{
    [TestFixture]
    public class TestZypperPatch
    {
        [Test]
        public void WhenFullyInitialized_ThenIPackagePropertiesAreSet()
        {
            var package = (IPackage)new ZypperPatch(
                "title",
                "category",
                "severity",
                "summary");

            Assert.AreEqual("Patch", package.PackageType);
            Assert.AreEqual("title", package.PackageId);
            Assert.AreEqual("summary (category)", package.Description);
            Assert.IsNull(package.Version);
            Assert.IsNull(package.Weblink);
            Assert.IsNull(package.InstalledOn);
            Assert.AreEqual(PackageCriticality.NonCritical, package.Criticality);
        }

        [Test]
        public void WhenBarelyInitialized_ThenIPackagePropertiesAreSet()
        {
            var package = (IPackage)new ZypperPatch(
                "title",
                null,
                null,
                null);

            Assert.AreEqual("Patch", package.PackageType);
            Assert.AreEqual("title", package.PackageId);
            Assert.AreEqual("", package.Description);
            Assert.IsNull(package.Version);
            Assert.IsNull(package.Weblink);
            Assert.IsNull(package.InstalledOn);
            Assert.AreEqual(PackageCriticality.NonCritical, package.Criticality);
        }
    }
}
