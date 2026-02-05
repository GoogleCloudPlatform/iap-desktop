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
        public void ExpandEnvironmentStrings_WhenSourceIsNullOrEmpty(
            [Values("", " ", null)] string? source)
        {
            Assert.That(
                UserEnvironment.ExpandEnvironmentStrings(source), Is.EqualTo(source));
        }

        [Test]
        public void ExpandEnvironmentStrings__WhenSourceIncludesVariable()
        {
            var source = "%ProgramFiles(x86)%\\foo";
            var expanded = UserEnvironment.ExpandEnvironmentStrings(source);

            Assert.That(
                expanded, Does.Contain(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)));

            Assert.That(expanded, Does.Contain("\\foo"));
        }

        [Test]
        public void ExpandEnvironmentStrings_WhenSourceIncludesUnknownVariable()
        {
            var source = "%THISVARIABLEDOESNOTEXIST%\\foo";
            Assert.That(
                UserEnvironment.ExpandEnvironmentStrings(source), Is.EqualTo(source));
        }

        //---------------------------------------------------------------------
        // TryResolveAppPath.
        //---------------------------------------------------------------------

        [Test]
        public void TryResolveAppPath_WhenAppUnknown_ThenTryResolveAppPathReturnsFalse()
        {
            Assert.That(UserEnvironment.TryResolveAppPath("doesnotexist.exe", out var _), Is.False);
        }

        [Test]
        public void TryResolveAppPath_WhenAppRegistered_ThenTryResolveAppPathReturnsPath()
        {
            Assert.That(UserEnvironment.TryResolveAppPath("Powershell.EXE", out var powershell), Is.True);
            Assert.That(powershell, Is.Not.Null);
            Assert.That(File.Exists(powershell), Is.True);
        }

        [Test]
        public void TryResolveAppPath_WhenAppNameIsPath_ThenTryResolveAppPathReturnsFalse(
            [Values("../app.exe", "c:\\app.exe")] string exeName)
        {
            Assert.That(UserEnvironment.TryResolveAppPath(exeName, out var _), Is.False);
        }
    }
}
