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

using Google.Solutions.IapDesktop.Application.Util;
using NUnit.Framework;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Test.Util
{
    [TestFixture]
    public class TestPropertyMapper : FixtureBase
    {
        class ClassWithNoMappedProperties
        {
        }

        [Test]
        public void WhenClassHasNoMappedProperties_ThenGetMappingsReturnsEmptyList()
        {
            Assert.IsFalse(new PropertyMapper<ClassWithNoMappedProperties>().GetMappings().Any());
        }

        class ClassWithOneMappedProperty
        {
            [StringRegistryValue(null)]
            public string IgnoredNullValue { get; }
        }

        [Test]
        public void WhenClassHasMappedPropertyWithoutName_ThenMGetMappingsIgnoredProperty()
        {
            Assert.IsFalse(new PropertyMapper<ClassWithNoMappedProperties>().GetMappings().Any());
        }

        class ClassWithTwoMappedPropertyies
        {
            [StringRegistryValue("str")]
            public string String { get; } = "test";

            [DwordRegistryValue("dword")]
            public int Dword { get; } = 42;
        }

        [Test]
        public void WhenClassHasMappedProperties_ThenGetMappingsReturnsMappings()
        {
            CollectionAssert.AreEqual(
                new[] { "str", "dword" },
                new PropertyMapper<ClassWithTwoMappedPropertyies>()
                    .GetMappings()
                    .Select(m => m.Name));
        }
    }
}
