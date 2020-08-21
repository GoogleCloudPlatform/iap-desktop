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
using System;

namespace Google.Solutions.IapDesktop.Extensions.Os.Test.Inventory
{
    [TestFixture]
    public class TestWuaPackageType
    {
        [Test]
        public void WhenCategoryIdUnknown_ThenFromCategoryIdReturnsNull()
        {
            var type = WuaPackageType.FromCategoryId(
                "145233b6-2d99-4f56-ba70-3748c1b6fdbd");

            Assert.IsNull(type);
        }

        [Test]
        public void WhenCategoryIdInvalid_ThenFromCategoryIdReturnsNull()
        {
            var type = WuaPackageType.FromCategoryId("145233b");

            Assert.IsNull(type);
        }

        [Test]
        public void WhenCategoryIdKnown_ThenFromCategoryIdReturnsType()
        {
            var type = WuaPackageType.FromCategoryId(
                "cd5ffd1e-e932-4e3a-bf74-18bf0b1bbd83");

            Assert.AreEqual("Updates", type.Name);
            Assert.AreEqual(PackageCriticality.NonCritical, type.Criticality);
        }

        [Test]
        public void WhenAllCategoryIdsUnknown_ThenMaxCriticalityIsNonCritical()
        {
            var criticality = WuaPackageType.MaxCriticality(new []
                {
                    "145233b6-2d99-4f56-ba70-3748c1b6fdbd",
                    "145233b6-2d99-4f56-ba70-3748c1b6fdba"
                });

            Assert.AreEqual(PackageCriticality.NonCritical, criticality);
        }

        [Test]
        public void WhenSomeCategoryIdsCritical_ThenMaxCriticalityIsCritical()
        {
            var criticality = WuaPackageType.MaxCriticality(new []
                {
                    "145233b6-2d99-4f56-ba70-3748c1b6fdbd",
                    "145233b6-2d99-4f56-ba70-3748c1b6fdba",
                    "thisisnotaguid",
                    "E6CF1350-C01B-414D-A61F-263D14D133B4"
                });

            Assert.AreEqual(PackageCriticality.Critical, criticality);
        }
    }
}
