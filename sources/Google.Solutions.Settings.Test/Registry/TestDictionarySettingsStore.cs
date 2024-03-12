//
// Copyright 2024 Google LLC
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

using Google.Solutions.Settings.Registry;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Google.Solutions.Settings.Test.Registry
{
    [TestFixture]
    public class TestDictionarySettingsStore
    {
        [Test]
        public void Read()
        {
            var store = new DictionarySettingsStore(new Dictionary<string, string>()
            {
                { "string", "a string" },
                { "int", "-42" },
                { "long", "1000000000000001" },
                { "color", "3" }
            });

            Assert.AreEqual(
                "a string", 
                store.Read<string>("string", "desc", null, null, null).Value);
            Assert.AreEqual(
                -42,
                store.Read<int>("int", "desc", null, null, 0).Value);
            Assert.AreEqual(
                1000000000000001L,
                store.Read<long>("long", "desc", null, null, 0).Value);
            Assert.AreEqual(
                ConsoleColor.DarkCyan,
                store.Read<ConsoleColor>("color", "desc", null, null, ConsoleColor.Black).Value);
        }

        [Test]
        public void Write()
        {
            var dict = new Dictionary<string, string>();
            var store = new DictionarySettingsStore(dict);

            var v = store.Read<string>("string", "desc", null, null, null);
            v.Value = "a string";
            store.Write(v);

            CollectionAssert.AreEquivalent(
                new Dictionary<string, string>()
                {
                    { "string", "a string" },
                },
                dict);
        }
    }
}
