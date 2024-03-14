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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using NUnit.Framework;
using System.Collections.Specialized;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Settings
{
    [TestFixture]
    public class TestIapRdpUrlSettingsStore
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        [Test]
        public void QueryStringMissing()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance");
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.IsNull(settings.RdpUsername.Value);
            Assert.IsNull(settings.RdpPassword.Value);
            Assert.IsNull(settings.RdpDomain.Value);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.RdpConnectionBar.Value);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.RdpAuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth._Default, settings.RdpColorDepth.Value);
            Assert.AreEqual(RdpAudioMode._Default, settings.RdpAudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RdpRedirectClipboard.Value);
        }

        [Test]
        public void QueryStringContainsNonsense()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?a=b&user=wrongcase&_");
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.IsNull(settings.RdpUsername.Value);
            Assert.IsNull(settings.RdpPassword.Value);
            Assert.IsNull(settings.RdpDomain.Value);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.RdpConnectionBar.Value);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.RdpAuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth._Default, settings.RdpColorDepth.Value);
            Assert.AreEqual(RdpAudioMode._Default, settings.RdpAudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RdpRedirectClipboard.Value);
        }

        [Test]
        public void QueryStringContainsOutOfRangeValues()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "ConnectionBar=-1&DesktopSize=a&AuthenticationLevel=null&ColorDepth=&" +
                "AudioMode=9999&RedirectClipboard=b&RedirectClipboard=c&" +
                "CredentialGenerationBehavior=-11");
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.IsNull(settings.RdpUsername.Value);
            Assert.IsNull(settings.RdpPassword.Value);
            Assert.IsNull(settings.RdpDomain.Value);
            Assert.AreEqual(RdpConnectionBarState._Default, settings.RdpConnectionBar.Value);
            Assert.AreEqual(RdpAuthenticationLevel._Default, settings.RdpAuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth._Default, settings.RdpColorDepth.Value);
            Assert.AreEqual(RdpAudioMode._Default, settings.RdpAudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard._Default, settings.RdpRedirectClipboard.Value);
        }

        [Test]
        public void QueryStringContainsValidUserOrDomain()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "userNAME=John%20Doe&PassworD=ignore&Domain=%20%20mydomain&");
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.AreEqual("John Doe", settings.RdpUsername.Value);
            Assert.IsNull(settings.RdpPassword.Value);
            Assert.AreEqual("  mydomain", settings.RdpDomain.Value);
        }

        [Test]
        public void QueryStringContainsValidSettings()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "ConnectionBar=1&DesktopSize=1&AuthenticationLevel=0&ColorDepth=2&" +
                "AudioMode=2&RedirectClipboard=0&CredentialGenerationBehavior=0&Rdpport=13389");
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.AreEqual(RdpConnectionBarState.Pinned, settings.RdpConnectionBar.Value);
            Assert.AreEqual(RdpAuthenticationLevel.AttemptServerAuthentication, settings.RdpAuthenticationLevel.Value);
            Assert.AreEqual(RdpColorDepth.DeepColor, settings.RdpColorDepth.Value);
            Assert.AreEqual(RdpAudioMode.DoNotPlay, settings.RdpAudioMode.Value);
            Assert.AreEqual(RdpRedirectClipboard.Disabled, settings.RdpRedirectClipboard.Value);
            Assert.AreEqual(13389, settings.RdpPort.Value);
        }

        [Test]
        public void UrlParameterIsNullOrEmpty(
            [Values(null, "", " ")] string emptyValue)
        {
            var queryParameters = new NameValueCollection
            {
                { "AudioMode", emptyValue }
            };

            var url = new IapRdpUrl(SampleLocator, queryParameters);
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.AreEqual(RdpAudioMode._Default, settings.RdpAudioMode.Value);
        }

        [Test]
        public void UrlParameterOutOfRange(
            [Values("-1", "invalid", "999999999")] string invalidValue)
        {
            var queryParameters = new NameValueCollection
            {
                { "AudioMode", invalidValue }
            };

            var url = new IapRdpUrl(SampleLocator, queryParameters);
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.AreEqual(RdpAudioMode._Default, settings.RdpAudioMode.Value);
        }

        [Test]
        public void UrlParameterValid()
        {
            var queryParameters = new NameValueCollection
            {
                { "AudioMode", "2" }
            };

            var url = new IapRdpUrl(SampleLocator, queryParameters);
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.AreEqual(RdpAudioMode.DoNotPlay, settings.RdpAudioMode.Value);
        }
    }
}
