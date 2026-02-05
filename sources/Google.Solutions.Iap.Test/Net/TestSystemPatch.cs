//
// Copyright 2019 Google LLC
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

using Google.Solutions.Iap.Net;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.WebSockets;

namespace Google.Solutions.Iap.Test.Net
{
    [TestFixture]
    public class TestSystemPatch : IapFixtureBase
    {
        //---------------------------------------------------------------------
        // UnrestrictUserAgentHeader.
        //---------------------------------------------------------------------

        [Test]
        public void UnrestrictUserAgentHeader()
        {
            var websocket = new ClientWebSocket();

            SystemPatch.UnrestrictUserAgentHeader.Install();
            Assert.IsTrue(SystemPatch.UnrestrictUserAgentHeader.IsInstalled);

            // Now the User-agent header can be set.
            websocket.Options.SetRequestHeader("User-Agent", "test");
        }

        //---------------------------------------------------------------------
        // SetUsernameAsHostHeaderPatch.
        //---------------------------------------------------------------------

        [Test]
        public void SetUsernameAsHostHeaderPatch_WhenPrefixNotRegistere()
        {
            var patch = new SystemPatch.SetUsernameAsHostHeaderPatch("unknown:");

            Assert.Throws<ArgumentException>(() => patch.Install());
        }

        [Test]
        public void SetUsernameAsHostHeaderPatch_WhenPrefixRegistered()
        {
            WebRequest.RegisterPrefix(
                TestHttpWebRequestCreate.Prefix,
                new TestHttpWebRequestCreate()); ;

            var patch = new SystemPatch.SetUsernameAsHostHeaderPatch(TestHttpWebRequestCreate.Prefix);
            patch.Install();
            Assert.IsTrue(patch.IsInstalled);

            var requestWithoutUserInfo = (HttpWebRequest)
                WebRequest.Create(new Uri("test+http://example.com"));
            Assert.That(requestWithoutUserInfo.RequestUri, Is.EqualTo(new Uri("http://example.com")));
            Assert.That(requestWithoutUserInfo.Host, Is.EqualTo("example.com"));

            var requestWithUserInfo = (HttpWebRequest)
                WebRequest.Create(new Uri("test+http://example.com@1.2.3.4/"));
            Assert.That(requestWithUserInfo.RequestUri, Is.EqualTo(new Uri("http://1.2.3.4")));
            Assert.That(requestWithUserInfo.Host, Is.EqualTo("example.com"));

            patch.Uninstall();
            Assert.That(patch.IsInstalled, Is.False);
        }

        private class TestHttpWebRequestCreate : IWebRequestCreate
        {
            public const string Prefix = "test+http:";

            public WebRequest Create(Uri uri)
            {
                return WebRequest.Create(new UriBuilder(uri)
                {
                    Scheme = "http"
                }.Uri);
            }
        }
    }
}
