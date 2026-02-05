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

using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using NUnit.Framework;
using System.Security;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Rdp
{
    [TestFixture]
    public class TestRdpCredential
    {
        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_WhenDomainSet()
        {
            var credential = new RdpCredential("user", "domain", new SecureString());
            Assert.That(credential.ToString(), Is.EqualTo("domain\\user"));
        }

        [Test]
        public void ToString_WhenUserNullOrEmpty()
        {
            Assert.That(
                new RdpCredential("", null, new SecureString()).ToString(), Is.EqualTo("(empty)"));
            Assert.That(
                new RdpCredential("", "", new SecureString()).ToString(), Is.EqualTo("(empty)"));
        }

        [Test]
        public void ToString_WhenDomainNullOrEmpty()
        {
            Assert.That(
                new RdpCredential("user", null, new SecureString()).ToString(), Is.EqualTo("user"));
            Assert.That(
                new RdpCredential("user", "", new SecureString()).ToString(), Is.EqualTo("user"));
        }
    }
}
