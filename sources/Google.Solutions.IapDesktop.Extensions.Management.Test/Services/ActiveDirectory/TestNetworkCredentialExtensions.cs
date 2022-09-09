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

using Google.Solutions.IapDesktop.Extensions.Management.Services.ActiveDirectory;
using NUnit.Framework;
using System.Net;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.Services.ActiveDirectory
{
    [TestFixture]
    public class TestNetworkCredentialExtensions
    {
        [Test]
        public void WhenUserInUpnFormat_ThenNormalizeReturnsCredential()
        {
            var normalized = new NetworkCredential(
                "user@example.org",
                "pwd",
                string.Empty).Normalize();

            Assert.AreEqual("user@example.org", normalized.UserName);
            Assert.AreEqual(string.Empty, normalized.Domain);
        }

        [Test]
        public void WhenUserInNetBiosFormat_ThenNormalizeReturnsCredential()
        {
            var normalized = new NetworkCredential(
                "EXAMPLE\\user",
                "pwd",
                string.Empty).Normalize();

            Assert.AreEqual("EXAMPLE\\user", normalized.UserName);
            Assert.AreEqual(string.Empty, normalized.Domain);
        }

        [Test]
        public void WhenDomainSpecifiedSeparately_ThenNormalizeReturnsCredential()
        {
            var normalized = new NetworkCredential(
                "user",
                "pwd",
                "EXAMPLE").Normalize();

            Assert.AreEqual("EXAMPLE\\user", normalized.UserName);
            Assert.AreEqual(string.Empty, normalized.Domain);
        }

        [Test]
        public void WhenDomainMissing_ThenNormalizeReturnsCredential()
        {
            var normalized = new NetworkCredential(
                "user",
                "pwd",
                null).Normalize();

            Assert.AreEqual("localhost\\user", normalized.UserName);
            Assert.AreEqual(string.Empty, normalized.Domain);
        }
    }
}
