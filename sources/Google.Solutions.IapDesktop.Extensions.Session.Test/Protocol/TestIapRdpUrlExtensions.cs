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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using NUnit.Framework;
using System.Collections.Specialized;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol
{
    [TestFixture]
    public class TestIapRdpUrlExtensions
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // TryGetParameter<ushort>.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUshortQueryParameterMissing_ThenTryGetParameterReturnsFalse()
        {
            var url = new IapRdpUrl(SampleLocator, new NameValueCollection());

            Assert.IsFalse(url.TryGetParameter("RdpPort", out ushort _));
        }

        [Test]
        public void WhenUshortQueryParameterIsNullOrEmpty_ThenTryGetParameterReturnsFalse(
            [Values(null, "", " ")] string emptyValue)
        {
            var queryParameters = new NameValueCollection
            {
                { "RdpPort", emptyValue }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsFalse(url.TryGetParameter("RdpPort", out ushort _));
        }

        [Test]
        public void WhenUshortQueryParameterOutOfRange_ThenTryGetParameterReturnsFalse(
            [Values("-1", "999999999")] string wrongValue)
        {
            var queryParameters = new NameValueCollection
            {
                { "RdpPort", wrongValue }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsFalse(url.TryGetParameter("RdpPort", out ushort _));
        }

        [Test]
        public void WhenUshortQueryParameterValid_ThenTryGetParameterReturnsTrue()
        {
            var queryParameters = new NameValueCollection
            {
                { "RdpPort", "3389" }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsTrue(url.TryGetParameter("RdpPort", out ushort value));
            Assert.AreEqual(3389, value);
        }

        //---------------------------------------------------------------------
        // TryGetParameter<string>.
        //---------------------------------------------------------------------

        [Test]
        public void WhenStringQueryParameterMissing_ThenTryGetParameterReturnsFalse()
        {
            var url = new IapRdpUrl(SampleLocator, new NameValueCollection());

            Assert.IsFalse(url.TryGetParameter("username", out string _));
        }

        [Test]
        public void WhenStringQueryParameterIsNullOrEmpty_ThenTryGetParameterReturnsFalse(
            [Values(null, "", " ")] string emptyValue)
        {
            var queryParameters = new NameValueCollection
            {
                { "Username", emptyValue }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsFalse(url.TryGetParameter("username", out string _));
        }

        [Test]
        public void WhenStringQueryParameterValid_ThenTryGetParameterReturnsTrue()
        {
            var queryParameters = new NameValueCollection
            {
                { "username", "bob" }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsTrue(url.TryGetParameter("username", out string value));
            Assert.AreEqual("bob", value);
        }

        //---------------------------------------------------------------------
        // TryGetParameter<TEnum>.
        //---------------------------------------------------------------------

        [Test]
        public void WhenEnumQueryParameterMissing_ThenTryGetParameterReturnsFalse()
        {
            var url = new IapRdpUrl(SampleLocator, new NameValueCollection());

            Assert.IsFalse(url.TryGetParameter<RdpAudioMode>("AudioMode", out var _));
        }

        [Test]
        public void WhenEnumQueryParameterIsNullOrEmpty_ThenTryGetParameterReturnsFalse(
            [Values(null, "", " ")] string emptyValue)
        {
            var queryParameters = new NameValueCollection
            {
                { "AudioMode", emptyValue }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsFalse(url.TryGetParameter<RdpAudioMode>("AudioMode", out var _));
        }

        [Test]
        public void WhenEnumQueryParameterOutOfRange_ThenTryGetParameterReturnsFalse(
            [Values("-1", "999999999")] string wrongValue)
        {
            var queryParameters = new NameValueCollection
            {
                { "AudioMode", wrongValue }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsFalse(url.TryGetParameter<RdpAudioMode>("AudioMode", out var _));
        }

        [Test]
        public void WhenEnumQueryParameterValid_ThenTryGetParameterReturnsTrue()
        {
            var queryParameters = new NameValueCollection
            {
                { "AudioMode", "2" }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsTrue(url.TryGetParameter<RdpAudioMode>("AudioMode", out var value));
            Assert.AreEqual(RdpAudioMode.DoNotPlay, value);
        }

        //---------------------------------------------------------------------
        // ApplyUrlParameterIfSet<TEnum>.
        //---------------------------------------------------------------------

        [Test]
        public void WhenQueryParameterMissing_ThenApplyLeavesOriginalValue()
        {
            var parameters = new RdpParameters
            {
                AudioMode = RdpAudioMode.PlayOnServer
            };
            Assert.AreNotEqual(RdpAudioMode._Default, parameters.AudioMode);

            parameters.ApplyUrlParameterIfSet<RdpAudioMode>(
                new IapRdpUrl(SampleLocator, new NameValueCollection()),
                "AudioMode",
                (p, v) => p.AudioMode = v);

            Assert.AreEqual(RdpAudioMode.PlayOnServer, parameters.AudioMode);
        }

        [Test]
        public void WhenQueryParameterIsNullOrEmpty_ThenApplyLeavesOriginalValue(
            [Values(null, "", " ")] string emptyValue)
        {
            var parameters = new RdpParameters
            {
                AudioMode = RdpAudioMode.PlayOnServer
            };
            Assert.AreNotEqual(RdpAudioMode._Default, parameters.AudioMode);

            var queryParameters = new NameValueCollection
            {
                { "AudioMode", emptyValue }
            };

            parameters.ApplyUrlParameterIfSet<RdpAudioMode>(
                new IapRdpUrl(SampleLocator, queryParameters),
                "AudioMode",
                (p, v) => p.AudioMode = v);

            Assert.AreEqual(RdpAudioMode.PlayOnServer, parameters.AudioMode);
        }

        [Test]
        public void WhenQueryParameterOutOfRange_ThenApplyLeavesOriginalValue(
            [Values("-1", "999999999")] string wrongValue)
        {
            var parameters = new RdpParameters
            {
                AudioMode = RdpAudioMode.PlayOnServer
            };
            Assert.AreNotEqual(RdpAudioMode._Default, parameters.AudioMode);

            var queryParameters = new NameValueCollection
            {
                { "AudioMode", wrongValue }
            };

            parameters.ApplyUrlParameterIfSet<RdpAudioMode>(
                new IapRdpUrl(SampleLocator, queryParameters),
                "AudioMode",
                (p, v) => p.AudioMode = v);

            Assert.AreEqual(RdpAudioMode.PlayOnServer, parameters.AudioMode);
        }

        [Test]
        public void WhenQueryParameterValid_ThenApplyReplacesOriginalValue()
        {
            var parameters = new RdpParameters
            {
                AudioMode = RdpAudioMode.PlayOnServer
            };
            Assert.AreNotEqual(RdpAudioMode._Default, parameters.AudioMode);

            var queryParameters = new NameValueCollection
            {
                { "AudioMode", "2" }
            };

            parameters.ApplyUrlParameterIfSet<RdpAudioMode>(
                new IapRdpUrl(SampleLocator, queryParameters),
                "AudioMode",
                (p, v) => p.AudioMode = v);

            Assert.AreEqual(RdpAudioMode.DoNotPlay, parameters.AudioMode);
        }
    }
}