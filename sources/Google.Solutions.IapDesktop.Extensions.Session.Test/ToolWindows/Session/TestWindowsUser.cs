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

using Google.Solutions.Apis.Auth;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Session
{
    [TestFixture]
    public class TestWindowsUser
    {
        private static IOidcSession CreateSession(string email)
        {
            var session = new Mock<IOidcSession>();
            session
                .SetupGet(a => a.Username)
                .Returns(email);

            return session.Object;
        }

        //---------------------------------------------------------------------
        // IsUserPrincipalName.
        //---------------------------------------------------------------------

        public void IsUserPrincipalName_WhenNameIsValid(
            [Values("user@domain.com", "a@b.c")] string upn)
        {
            Assert.That(WindowsUser.IsUserPrincipalName(upn), Is.True);
        }

        public void IsUserPrincipalNameWhenNameIsInvalid(
            [Values("user@", "@user", "a.b@c")] string upn)
        {
            Assert.That(WindowsUser.IsUserPrincipalName(upn), Is.False);
        }

        //---------------------------------------------------------------------
        // IsLocalUsername.
        //---------------------------------------------------------------------

        public void IsLocalUsername_WhenNameIsValid(
            [Values("user", "u")] string name)
        {
            Assert.That(WindowsUser.IsLocalUsername(name), Is.True);
        }

        public void IsLocalUsername_WhenNameIsInvalid(
            [Values("DOMAIN\\user", "u12345678901234567890", "user@domain.tld")] string name)
        {
            Assert.That(WindowsUser.IsLocalUsername(name), Is.False);
        }

        //---------------------------------------------------------------------
        // SuggestUsername.
        //---------------------------------------------------------------------

        [Test]
        public void SuggestUsername_WhenEmailCompliant()
        {
            Assert.That(
                WindowsUser.SuggestUsername(
                    CreateSession("bob.b@example.com")), Is.EqualTo("bob.b"));
        }

        [Test]
        public void SuggestUsername_WhenEmailTooLong()
        {
            Assert.That(
                WindowsUser.SuggestUsername(
                    CreateSession("bob01234567890123456789@example.com")), Is.EqualTo("bob01234567890123456"));
        }

        [Test]
        public void SuggestUsername_WhenEmailNull()
        {
            Assert.That(
                WindowsUser.SuggestUsername(
                    CreateSession(null!)), Is.EqualTo(Environment.UserName));
        }

        [Test]
        public void SuggestUsername_WhenEmailInvalid()
        {
            Assert.That(
                WindowsUser.SuggestUsername(
                    CreateSession("bob")), Is.EqualTo(Environment.UserName));
        }
    }
}
