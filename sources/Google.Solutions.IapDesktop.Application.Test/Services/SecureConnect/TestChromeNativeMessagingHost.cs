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

using Google.Solutions.IapDesktop.Application.Services.SecureConnect;
using Microsoft.Win32;
using NUnit.Framework;
using System.IO;

namespace Google.Solutions.IapDesktop.Application.Test.Services.SecureConnect
{
    [TestFixture]
    public class TestChromeNativeMessagingHost
    {
        [Test]
        public void WhenManifestPathIsNull_ThenFindNativeHelperLocationFromManifestReturnsNull()
        {
            Assert.IsNull(ChromeNativeMessagingHost.FindNativeHelperLocationFromManifest(
                "some.extension",
                null));
        }

        [Test]
        public void WhenManifestPathDoesNotExist_ThenFindNativeHelperLocationFromManifestReturnsNull()
        {
            Assert.IsNull(ChromeNativeMessagingHost.FindNativeHelperLocationFromManifest(
                "some.extension", 
                "doesnotexist.json"));
        }

        [Test]
        public void WhenManifestIsEmpty_ThenFindNativeHelperLocationFromManifestReturnsNull()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "{}");

            Assert.IsNull(ChromeNativeMessagingHost.FindNativeHelperLocationFromManifest(
                "some.extension", 
                tempFile));
        }

        [Test]
        public void WhenManifestContainsInvalidName_ThenFindNativeHelperLocationFromManifestReturnsNull()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(
                tempFile, @"{
                    ""name"": ""other.extension"",
                    ""path"": ""NativeHelperWin.exe""
                }");

            Assert.IsNull(ChromeNativeMessagingHost.FindNativeHelperLocationFromManifest(
                "some.extension", 
                tempFile));
        }

        [Test]
        public void WhenManifestIsValid_ThenFindNativeHelperReturnsAbsolutePath()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(
                tempFile, @"{
                    ""name"": ""some.extension"",
                    ""path"": ""extension.exe""
                }");

            var path = ChromeNativeMessagingHost.FindNativeHelperLocationFromManifest(
                "some.extension",
                tempFile);


            Assert.IsNotNull(path);
            StringAssert.EndsWith("\\extension.exe", path);
        }

        [Test]
        public void WhenHostNotFound_ThenStartRaisesException()
        {
            Assert.Throws<ChromeNativeMessagingHostNotAvailableException>(
                () => ChromeNativeMessagingHost.Start("unknown.host", RegistryHive.LocalMachine));
        }
    }
}
