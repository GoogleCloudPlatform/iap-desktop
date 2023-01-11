﻿//
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

using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Test.Host
{
    [TestFixture]
    public class TestInstall
    {
        private const string TestBaseKeyPath = @"Software\Google\__Test";

        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(TestBaseKeyPath, false);
        }

        //---------------------------------------------------------------------
        // CurrentVersion.
        //---------------------------------------------------------------------

        [Test]
        public void CurrentVersion()
        {
            var install = new Install(TestBaseKeyPath);
            Assert.AreNotEqual(0, install.CurrentVersion.Major);
        }

        //---------------------------------------------------------------------
        // InitialVersion.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFreshlyInstalled_ThenInitialVersionIsCurrentVersion()
        {
            var install = new Install(TestBaseKeyPath);
            Assert.AreEqual(install.CurrentVersion, install.InitialVersion);
        }

        [Test]
        public void WhenKeyMissing_ThenInitialVersionIsCurrentVersion()
        {
            var install = new Install(TestBaseKeyPath);
            this.hkcu.DeleteSubKeyTree(TestBaseKeyPath, false);
            Assert.AreEqual(install.CurrentVersion, install.InitialVersion);
        }

        [Test]
        public void WhenUpgraded_ThenInitialVersionIsCurrentVersion()
        {
            var install = new Install(TestBaseKeyPath);
            
            using (var key = this.hkcu.CreateSubKey(TestBaseKeyPath))
            {
                key.SetValue(
                    "InstalledVersionHistory",
                    new string[] { "0.0.3.0", "0.0.1.0", "0.0.2.0" },
                    RegistryValueKind.MultiString);
            }

            Assert.AreNotEqual(install.CurrentVersion, install.InitialVersion);
            Assert.AreEqual(new Version(0, 0, 1, 0), install.InitialVersion);
        }
    }
}
