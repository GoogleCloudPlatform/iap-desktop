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

            Assert.That(normalized.UserName, Is.EqualTo("user@example.org"));
            Assert.That(normalized.Domain, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Normalize_WhenUserInNetBiosFormat_ThenNormalizeReturnsCredential()
        {
            var normalized = new NetworkCredential(
                "EXAMPLE\\user",
                "pwd",
                string.Empty).Normalize();

            Assert.That(normalized.UserName, Is.EqualTo("EXAMPLE\\user"));
            Assert.That(normalized.Domain, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Normalize_WhenDomainSpecifiedSeparately_ThenNormalizeReturnsCredential()
        {
            var normalized = new NetworkCredential(
                "user",
                "pwd",
                "EXAMPLE").Normalize();

            Assert.That(normalized.UserName, Is.EqualTo("EXAMPLE\\user"));
            Assert.That(normalized.Domain, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Normalize_WhenDomainMissing_ThenNormalizeReturnsCredential()
        {
            var normalized = new NetworkCredential(
                "user",
                "pwd",
                null).Normalize();

            Assert.That(normalized.UserName, Is.EqualTo("localhost\\user"));
            Assert.That(normalized.Domain, Is.EqualTo(string.Empty));
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

            Assert.That(cred1.IsUpnFormat(), Is.True);
            Assert.That(cred1.IsNetBiosFormat(), Is.False);
        }

        //---------------------------------------------------------------------
        // IsNetBiosFormat.
        //---------------------------------------------------------------------

        [Test]
        public void IsNetBiosFormat()
        {
            //
            // NetBios format.
            //
            var cred1 = new NetworkCredential()
            {
                UserName = "domain\\user",
            };

            Assert.That(cred1.IsNetBiosFormat(), Is.True);
            Assert.That(cred1.IsUpnFormat(), Is.False);

            //
            // Separate username, domain.
            //
            var cred2 = new NetworkCredential()
            {
                UserName = "user",
                Domain = "domain"
            };

            Assert.That(cred2.IsNetBiosFormat(), Is.True);
            Assert.That(cred2.IsUpnFormat(), Is.False);

            //
            // Unqualified format.
            //
            var cred3 = new NetworkCredential()
            {
                UserName = "user",
            };

            Assert.That(cred3.IsNetBiosFormat(), Is.True);
            Assert.That(cred3.IsUpnFormat(), Is.False);
        }

        //---------------------------------------------------------------------
        // IsDomainOrHostQualified.
        //---------------------------------------------------------------------

        [Test]
        public void IsDomainOrHostQualified()
        {
            //
            // Unqualified format.
            //
            Assert.That(new NetworkCredential().IsDomainOrHostQualified(), Is.False);
            Assert.That(new NetworkCredential("", "").IsDomainOrHostQualified(), Is.False);
            Assert.That(new NetworkCredential(" ", "").IsDomainOrHostQualified(), Is.False);
            Assert.That(new NetworkCredential("user", "").IsDomainOrHostQualified(), Is.False);
            Assert.That(new NetworkCredential(".\\user", "").IsDomainOrHostQualified(), Is.False);
            Assert.That(new NetworkCredential(" .\\user", "").IsDomainOrHostQualified(), Is.False);
            Assert.That(new NetworkCredential("user", "", ".").IsDomainOrHostQualified(), Is.False);
            Assert.That(new NetworkCredential("user", "", " ").IsDomainOrHostQualified(), Is.False);
            Assert.That(new NetworkCredential("user", "", " .").IsDomainOrHostQualified(), Is.False);

            //
            // UPN format.
            //
            Assert.That(new NetworkCredential("user@domain", "").IsDomainOrHostQualified(), Is.True);
            Assert.That(new NetworkCredential("user@domain", "", "domain").IsDomainOrHostQualified(), Is.True);
            Assert.That(new NetworkCredential("user", "", "domain").IsDomainOrHostQualified(), Is.True);
            Assert.That(new NetworkCredential("domain\\user", "", "domain").IsDomainOrHostQualified(), Is.True);
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

            Assert.That(
                new NetworkCredential()
                {
                    UserName = "user@domain",
                }.GetDomainComponent(), Is.EqualTo("domain"));
            Assert.That(
                new NetworkCredential()
                {
                    UserName = "user@",
                }.GetDomainComponent(), Is.EqualTo(""));

            //
            // NetBIOS format: domain\username.
            //
            Assert.That(
                new NetworkCredential()
                {
                    UserName = "domain\\user",
                }.GetDomainComponent(), Is.EqualTo("domain"));
            Assert.That(
                new NetworkCredential()
                {
                    UserName = "\\user",
                }.GetDomainComponent(), Is.EqualTo(""));

            //
            // Decomposed format.
            //
            Assert.That(
                new NetworkCredential()
                {
                    UserName = "user",
                    Domain = "domain"
                }.GetDomainComponent(), Is.EqualTo("domain"));

            Assert.That(
                new NetworkCredential()
                {
                    UserName = "user",
                }.GetDomainComponent(), Is.EqualTo(""));
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

            Assert.That(
                new NetworkCredential()
                {
                    UserName = "user@domain",
                }.GetUserComponent(), Is.EqualTo("user"));
            Assert.That(
                new NetworkCredential()
                {
                    UserName = "user@",
                }.GetUserComponent(), Is.EqualTo("user"));

            //
            // NetBIOS format: domain\username.
            //
            Assert.That(
                new NetworkCredential()
                {
                    UserName = "domain\\user",
                }.GetUserComponent(), Is.EqualTo("user"));
            Assert.That(
                new NetworkCredential()
                {
                    UserName = "user\\",
                }.GetUserComponent(), Is.EqualTo(""));

            //
            // Decomposed format.
            //
            Assert.That(
                new NetworkCredential()
                {
                    UserName = "user",
                    Domain = "domain"
                }.GetUserComponent(), Is.EqualTo("user"));

            Assert.That(
                new NetworkCredential()
                {
                    UserName = "user",
                }.GetUserComponent(), Is.EqualTo("user"));
        }
    }
}
