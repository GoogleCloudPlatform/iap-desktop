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
        public void TryGetParameter_WhenUshortQueryParameterMissing()
        {
            var url = new IapRdpUrl(SampleLocator, new NameValueCollection());

            Assert.IsFalse(url.TryGetParameter("RdpPort", out ushort _));
        }

        [Test]
        public void TryGetParameter_WhenUshortQueryParameterIsNullOrEmpty(
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
        public void TryGetParameter_WhenUshortQueryParameterOutOfRange(
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
        public void TryGetParameter_WhenUshortQueryParameterValid()
        {
            var queryParameters = new NameValueCollection
            {
                { "RdpPort", "3389" }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsTrue(url.TryGetParameter("RdpPort", out ushort value));
            Assert.That(value, Is.EqualTo(3389));
        }

        //---------------------------------------------------------------------
        // TryGetParameter<string>.
        //---------------------------------------------------------------------

        [Test]
        public void TryGetParameter_WhenStringQueryParameterMissing()
        {
            var url = new IapRdpUrl(SampleLocator, new NameValueCollection());

            Assert.IsFalse(url.TryGetParameter("username", out string _));
        }

        [Test]
        public void TryGetParameter_WhenStringQueryParameterIsNullOrEmpty(
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
        public void TryGetParameter_WhenStringQueryParameterValid()
        {
            var queryParameters = new NameValueCollection
            {
                { "username", "bob" }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsTrue(url.TryGetParameter("username", out string value));
            Assert.That(value, Is.EqualTo("bob"));
        }

        //---------------------------------------------------------------------
        // TryGetParameter<TEnum>.
        //---------------------------------------------------------------------

        [Test]
        public void TryGetParameter_WhenEnumQueryParameterMissing()
        {
            var url = new IapRdpUrl(SampleLocator, new NameValueCollection());

            Assert.IsFalse(url.TryGetParameter<RdpAudioPlayback>("AudioMode", out var _));
        }

        [Test]
        public void TryGetParameter_WhenEnumQueryParameterIsNullOrEmpty(
            [Values(null, "", " ")] string emptyValue)
        {
            var queryParameters = new NameValueCollection
            {
                { "AudioMode", emptyValue }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsFalse(url.TryGetParameter<RdpAudioPlayback>("AudioMode", out var _));
        }

        [Test]
        public void TryGetParameter_WhenEnumQueryParameterOutOfRange(
            [Values("-1", "999999999")] string wrongValue)
        {
            var queryParameters = new NameValueCollection
            {
                { "AudioMode", wrongValue }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsFalse(url.TryGetParameter<RdpAudioPlayback>("AudioMode", out var _));
        }

        [Test]
        public void TryGetParameter_WhenEnumQueryParameterValid()
        {
            var queryParameters = new NameValueCollection
            {
                { "AudioMode", "2" }
            };
            var url = new IapRdpUrl(SampleLocator, queryParameters);

            Assert.IsTrue(url.TryGetParameter<RdpAudioPlayback>("AudioMode", out var value));
            Assert.That(value, Is.EqualTo(RdpAudioPlayback.DoNotPlay));
        }

        //---------------------------------------------------------------------
        // ApplyUrlParameterIfSet<TEnum>.
        //---------------------------------------------------------------------

        [Test]
        public void ApplyUrlParameterIfSet_WhenQueryParameterMissing()
        {
            var parameters = new RdpParameters
            {
                AudioPlayback = RdpAudioPlayback.PlayOnServer
            };
            Assert.That(parameters.AudioPlayback, Is.Not.EqualTo(RdpAudioPlayback._Default));

            parameters.ApplyUrlParameterIfSet<RdpAudioPlayback>(
                new IapRdpUrl(SampleLocator, new NameValueCollection()),
                "AudioMode",
                (p, v) => p.AudioPlayback = v);

            Assert.That(parameters.AudioPlayback, Is.EqualTo(RdpAudioPlayback.PlayOnServer));
        }

        [Test]
        public void ApplyUrlParameterIfSet_WhenQueryParameterIsNullOrEmpty(
            [Values(null, "", " ")] string emptyValue)
        {
            var parameters = new RdpParameters
            {
                AudioPlayback = RdpAudioPlayback.PlayOnServer
            };
            Assert.That(parameters.AudioPlayback, Is.Not.EqualTo(RdpAudioPlayback._Default));

            var queryParameters = new NameValueCollection
            {
                { "AudioMode", emptyValue }
            };

            parameters.ApplyUrlParameterIfSet<RdpAudioPlayback>(
                new IapRdpUrl(SampleLocator, queryParameters),
                "AudioMode",
                (p, v) => p.AudioPlayback = v);

            Assert.That(parameters.AudioPlayback, Is.EqualTo(RdpAudioPlayback.PlayOnServer));
        }

        [Test]
        public void ApplyUrlParameterIfSet_WhenQueryParameterOutOfRange(
            [Values("-1", "999999999")] string wrongValue)
        {
            var parameters = new RdpParameters
            {
                AudioPlayback = RdpAudioPlayback.PlayOnServer
            };
            Assert.That(parameters.AudioPlayback, Is.Not.EqualTo(RdpAudioPlayback._Default));

            var queryParameters = new NameValueCollection
            {
                { "AudioMode", wrongValue }
            };

            parameters.ApplyUrlParameterIfSet<RdpAudioPlayback>(
                new IapRdpUrl(SampleLocator, queryParameters),
                "AudioMode",
                (p, v) => p.AudioPlayback = v);

            Assert.That(parameters.AudioPlayback, Is.EqualTo(RdpAudioPlayback.PlayOnServer));
        }

        [Test]
        public void ApplyUrlParameterIfSet_WhenQueryParameterValid()
        {
            var parameters = new RdpParameters
            {
                AudioPlayback = RdpAudioPlayback.PlayOnServer
            };
            Assert.That(parameters.AudioPlayback, Is.Not.EqualTo(RdpAudioPlayback._Default));

            var queryParameters = new NameValueCollection
            {
                { "AudioMode", "2" }
            };

            parameters.ApplyUrlParameterIfSet<RdpAudioPlayback>(
                new IapRdpUrl(SampleLocator, queryParameters),
                "AudioMode",
                (p, v) => p.AudioPlayback = v);

            Assert.That(parameters.AudioPlayback, Is.EqualTo(RdpAudioPlayback.DoNotPlay));
        }
    }
}