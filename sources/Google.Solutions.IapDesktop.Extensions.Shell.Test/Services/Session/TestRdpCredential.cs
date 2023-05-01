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


using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using NUnit.Framework;
using System.Security;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Session
{
    [TestFixture]
    public class TestRdpCredential : ShellFixtureBase
    {
        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDomainSet_ThenToStringReturnsBackslashNotation()
        {
            var credential = new RdpCredential("user", "domain", new SecureString());
            Assert.AreEqual("domain\\user", credential.ToString());
        }

        [Test]
        public void WhenUserNullOrEmpty_ThenToStringReturnsEmpty()
        {
            Assert.AreEqual(
                "(empty)",
                new RdpCredential("", null, new SecureString()).ToString());
            Assert.AreEqual(
                "(empty)",
                new RdpCredential("", "", new SecureString()).ToString());
        }

        [Test]
        public void WhenDomainNullOrEmpty_ThenToStringReturnsUser()
        {
            Assert.AreEqual(
                "user",
                new RdpCredential("user", null, new SecureString()).ToString());
            Assert.AreEqual(
                "user",
                new RdpCredential("user", "", new SecureString()).ToString());
        }
    }
}
