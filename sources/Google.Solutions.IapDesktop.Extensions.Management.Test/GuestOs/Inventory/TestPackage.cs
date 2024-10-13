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
    public class TestPackage
    {
        [Test]
        public void Properties_WhenFullyInitialized()
        {
            var package = (IPackage)new Package(
                "name",
                "architecture",
                "version");

            Assert.AreEqual("Package", package.PackageType);
            Assert.AreEqual("name", package.PackageId);
            Assert.IsNull(package.Description);
            Assert.AreEqual("version", package.Version);
            Assert.IsNull(package.Weblink);
            Assert.IsNull(package.InstalledOn);
            Assert.AreEqual(PackageCriticality.NonCritical, package.Criticality);
        }

        [Test]
        public void Properties_WhenBarelyInitialized()
        {
            var package = (IPackage)new Package(
                null,
                null,
                null);

            Assert.AreEqual("Package", package.PackageType);
            Assert.IsNull(package.PackageId);
            Assert.IsNull(package.Description);
            Assert.IsNull(package.Version);
            Assert.IsNull(package.Weblink);
            Assert.IsNull(package.InstalledOn);
            Assert.AreEqual(PackageCriticality.NonCritical, package.Criticality);
        }
    }
}
