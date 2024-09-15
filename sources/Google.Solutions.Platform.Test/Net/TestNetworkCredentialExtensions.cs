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

using Google.Solutions.Platform.Net;
using NUnit.Framework;
using System.Net;

namespace Google.Solutions.Platform.Test.Net
{
    [TestFixture]
    public class TestNetworkCredentialExtensions
    {
        //---------------------------------------------------------------------
        // Normalize.
        //---------------------------------------------------------------------

        [Test]
        public void Normalize_WhenUserInUpnFormat_ThenNormalizeReturnsCredential()
        {
            var normalized = new NetworkCredential(
                "user@example.org",
                "pwd",
                string.Empty).Normalize();

            Assert.AreEqual("user@example.org", normalized.UserName);
            Assert.AreEqual(string.Empty, normalized.Domain);
        }

        [Test]
        public void Normalize_WhenUserInNetBiosFormat_ThenNormalizeReturnsCredential()
        {
            var normalized = new NetworkCredential(
                "EXAMPLE\\user",
                "pwd",
                string.Empty).Normalize();

            Assert.AreEqual("EXAMPLE\\user", normalized.UserName);
            Assert.AreEqual(string.Empty, normalized.Domain);
        }

        [Test]
        public void Normalize_WhenDomainSpecifiedSeparately_ThenNormalizeReturnsCredential()
        {
            var normalized = new NetworkCredential(
                "user",
                "pwd",
                "EXAMPLE").Normalize();

            Assert.AreEqual("EXAMPLE\\user", normalized.UserName);
            Assert.AreEqual(string.Empty, normalized.Domain);
        }

        [Test]
        public void Normalize_WhenDomainMissing_ThenNormalizeReturnsCredential()
        {
            var normalized = new NetworkCredential(
                "user",
                "pwd",
                null).Normalize();

            Assert.AreEqual("localhost\\user", normalized.UserName);
            Assert.AreEqual(string.Empty, normalized.Domain);
        }

        //---------------------------------------------------------------------
        // IsUpnFormat.
        //---------------------------------------------------------------------

        [Test]
        public void IsUpnFormat()
        {
            var cred1 = new NetworkCredential()
            {
                UserName = "user@domain"
            };

            Assert.IsTrue(cred1.IsUpnFormat());
            Assert.IsFalse(cred1.IsNetBiosFormat());
        }

        //---------------------------------------------------------------------
        // IsNetBiosFormat.
        //---------------------------------------------------------------------

        [Test]
        public void IsNetBiosFormat()
        {
            var cred1 = new NetworkCredential()
            {
                UserName = "domain\\user",
            };
            var cred2 = new NetworkCredential()
            {
                UserName = "user",
                Domain = "domain"
            };
            var cred3 = new NetworkCredential()
            {
                UserName = "user",
            };

            Assert.IsTrue(cred1.IsNetBiosFormat());
            Assert.IsTrue(cred2.IsNetBiosFormat());
            Assert.IsTrue(cred3.IsNetBiosFormat());

            Assert.IsFalse(cred1.IsUpnFormat());
            Assert.IsFalse(cred2.IsUpnFormat());
            Assert.IsFalse(cred3.IsUpnFormat());
        }

        //---------------------------------------------------------------------
        // GetDomainComponent.
        //---------------------------------------------------------------------

        [Test]
        public void GetDomainComponent()
        {
            //
            // UPN format: username@domain.
            //

            Assert.AreEqual("domain", new NetworkCredential()
            {
                UserName = "user@domain",
            }.GetDomainComponent());
            Assert.AreEqual("", new NetworkCredential()
            {
                UserName = "user@",
            }.GetDomainComponent());

            //
            // NetBIOS format: domain\username.
            //
            Assert.AreEqual("domain", new NetworkCredential()
            {
                UserName = "domain\\user",
            }.GetDomainComponent());
            Assert.AreEqual("", new NetworkCredential()
            {
                UserName = "\\user",
            }.GetDomainComponent());

            //
            // Decomposed format.
            //
            Assert.AreEqual("domain", new NetworkCredential()
            {
                UserName = "user",
                Domain = "domain"
            }.GetDomainComponent());

            Assert.AreEqual("", new NetworkCredential()
            {
                UserName = "user",
            }.GetDomainComponent());
        }


        //---------------------------------------------------------------------
        // GetUserComponent.
        //---------------------------------------------------------------------

        [Test]
        public void GetUserComponent()
        {
            //
            // UPN format: username@domain.
            //

            Assert.AreEqual("user", new NetworkCredential()
            {
                UserName = "user@domain",
            }.GetUserComponent());
            Assert.AreEqual("user", new NetworkCredential()
            {
                UserName = "user@",
            }.GetUserComponent());

            //
            // NetBIOS format: domain\username.
            //
            Assert.AreEqual("user", new NetworkCredential()
            {
                UserName = "domain\\user",
            }.GetUserComponent());
            Assert.AreEqual("", new NetworkCredential()
            {
                UserName = "user\\",
            }.GetUserComponent());

            //
            // Decomposed format.
            //
            Assert.AreEqual("user", new NetworkCredential()
            {
                UserName = "user",
                Domain = "domain"
            }.GetUserComponent());

            Assert.AreEqual("user", new NetworkCredential()
            {
                UserName = "user",
            }.GetUserComponent());
        }
    }
}
