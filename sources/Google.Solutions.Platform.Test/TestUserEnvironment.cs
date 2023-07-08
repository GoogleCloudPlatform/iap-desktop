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

using NUnit.Framework;
using System;
using System.IO;

namespace Google.Solutions.Platform.Test
{
    [TestFixture]
    public class TestUserEnvironment
    {
        //---------------------------------------------------------------------
        // ExpandEnvironmentStrings.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSourceIsNullOrEmpty_ThenExpandEnvironmentStringsReturnsSource(
            [Values("", " ", null)] string source)
        {
            Assert.AreEqual(
                source,
                UserEnvironment.ExpandEnvironmentStrings(source));
        }

        [Test]
        public void WhenSourceIncludesVariable_ThenExpandEnvironmentStringsReturnsExpandedSource()
        {
            var source = "%ProgramFiles(x86)%\\foo";
            var expanded = UserEnvironment.ExpandEnvironmentStrings(source);

            StringAssert.Contains(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                expanded);

            StringAssert.Contains("\\foo", expanded);
        }

        [Test]
        public void WhenSourceIncludesUnknownVariable_ThenExpandEnvironmentStringsReturnsSource()
        {
            var source = "%THISVARIABLEDOESNOTEXIST%\\foo";
            Assert.AreEqual(
                source,
                UserEnvironment.ExpandEnvironmentStrings(source));
        }

        //---------------------------------------------------------------------
        // TryResolveAppPath.
        //---------------------------------------------------------------------

        [Test]
        public void WhenAppUnknown_ThenTryResolveAppPathReturnsFalse()
        {
            Assert.IsFalse(UserEnvironment.TryResolveAppPath("doesnotexist.exe", out var _));
        }

        [Test]
        public void WhenAppRegistered_ThenTryResolveAppPathReturnsPath()
        {
            Assert.IsTrue(UserEnvironment.TryResolveAppPath("IExPlOrE.EXE", out var iexplore));
            Assert.IsNotNull(iexplore);
            Assert.IsTrue(File.Exists(iexplore));
        }
    }
}
