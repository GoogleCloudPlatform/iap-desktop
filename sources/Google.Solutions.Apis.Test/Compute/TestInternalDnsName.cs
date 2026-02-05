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

using Google.Solutions.Apis.Compute;
using NUnit.Framework;

namespace Google.Solutions.Apis.Test.Compute
{
    [TestFixture]
    public class TestInternalDnsName
    {
        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse_WhenNameIsZonal_ThenReturnsZonalName()
        {
            var name = "instance-1.zone-1.c.project-1.internal";
            Assert.IsTrue(InternalDnsName.TryParse(name, out var parsed));
            Assert.IsNotNull(parsed);
            Assert.IsInstanceOf<InternalDnsName.ZonalName>(parsed);
            Assert.That(parsed!.Name, Is.EqualTo(name));
            Assert.That(parsed.ToString(), Is.EqualTo(name));
        }

        [Test]
        public void TryParse_WhenNameIsGlobal_ThenReturnsGlobalName()
        {
            var name = "instance-1.c.project-1.internal";
            Assert.IsTrue(InternalDnsName.TryParse(name, out var parsed));
            Assert.IsNotNull(parsed);
            Assert.IsInstanceOf<InternalDnsName.GlobalName>(parsed);
            Assert.That(parsed!.Name, Is.EqualTo(name));
            Assert.That(parsed.ToString(), Is.EqualTo(name));
        }

        [Test]
        public void TryParse_WhenNameIsInvalid_ThenReturnsFalse(
            [Values(null, "", " ", ".internal", "example.com")] string? name)
        {
            Assert.That(InternalDnsName.TryParse(name, out var _), Is.False);
        }
    }
}
