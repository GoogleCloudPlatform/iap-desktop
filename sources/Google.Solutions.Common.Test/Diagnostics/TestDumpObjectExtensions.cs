
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

using Google.Solutions.Common.Diagnostics;
using NUnit.Framework;

namespace Google.Solutions.Common.Test.Diagnostics
{
    [TestFixture]
    public class TestDumpObjectExtensions
    {
        [Test]
        public void DumpProperties_WhenObjectIsNull_ThenReturnsEmptyString()
        {
            Assert.That(((object?)null).DumpProperties(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void DumpProperties_WhenObjectIsNotNull_ThenReturnsString()
        {
            Assert.That(
                new {
                    Foo = 1,
                    Bar = "test",
                    Quux = new object()
                }.DumpProperties(), Is.EqualTo("Foo: 1\nBar: test\nQuux: System.Object\n"));
        }
    }
}
