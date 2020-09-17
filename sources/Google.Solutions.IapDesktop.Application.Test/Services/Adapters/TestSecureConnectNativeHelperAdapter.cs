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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Adapters
{
    [TestFixture]
    public class TestSecureConnectNativeHelperAdapter
    {
        [Test]
        public void WhenManifestPathIsNull_ThenFindNativeHelperLocationFromManifestRaisesException()
        {
            Assert.Throws<SecureConnectNotInstalledException>(
                () => SecureConnectNativeHelperAdapter.FindNativeHelperLocationFromManifest(null));
        }

        [Test]
        public void WhenManifestPathDoesNotExist_ThenFindNativeHelperLocationFromManifestRaisesException()
        {
            Assert.Throws<SecureConnectNotInstalledException>(
                () => SecureConnectNativeHelperAdapter.FindNativeHelperLocationFromManifest("doesnotexist.json"));
        }

        [Test]
        public void WhenManifestIsEmpty_ThenFindNativeHelperLocationFromManifestRaisesException()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "{}");

            Assert.Throws<SecureConnectNotInstalledException>(
                () => SecureConnectNativeHelperAdapter.FindNativeHelperLocationFromManifest(tempFile));
        }

        [Test]
        public void WhenManifestContainsInvalidName_ThenFindNativeHelperLocationFromManifestRaisesException()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(
                tempFile, @"{
                    ""name"": ""com.google.secure_connect.xxx"",
                    ""path"": ""NativeHelperWin.exe""
                }");

            Assert.Throws<SecureConnectNotInstalledException>(
                () => SecureConnectNativeHelperAdapter.FindNativeHelperLocationFromManifest(tempFile));
        }

        [Test]
        public void WhenManifestIsValid_ThenFindNativeHelperReturnsAbsolutePath()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(
                tempFile, @"{
                    ""name"": ""com.google.secure_connect.native_helper"",
                    ""path"": ""NativeHelperWin.exe""
                }");

            var path = SecureConnectNativeHelperAdapter.FindNativeHelperLocationFromManifest(tempFile);
            
            Assert.IsNotNull(path);
            StringAssert.EndsWith("\\NativeHelperWin.exe", path);
        }
    }
}
