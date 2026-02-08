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

            Assert.That(settings.RdpUsername.Value, Is.Null);
            Assert.That(settings.RdpPassword.Value, Is.Null);
            Assert.That(settings.RdpDomain.Value, Is.Null);
            Assert.That(settings.RdpConnectionBar.Value, Is.EqualTo(RdpConnectionBarState._Default));
            Assert.That(settings.RdpAuthenticationLevel.Value, Is.EqualTo(RdpAuthenticationLevel._Default));
            Assert.That(settings.RdpColorDepth.Value, Is.EqualTo(RdpColorDepth._Default));
            Assert.That(settings.RdpAudioPlayback.Value, Is.EqualTo(RdpAudioPlayback._Default));
            Assert.That(settings.RdpRedirectClipboard.Value, Is.EqualTo(RdpRedirectClipboard._Default));
        }

        [Test]
        public void QueryStringContainsNonsense()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?a=b&user=wrongcase&_");
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.That(settings.RdpUsername.Value, Is.Null);
            Assert.That(settings.RdpPassword.Value, Is.Null);
            Assert.That(settings.RdpDomain.Value, Is.Null);
            Assert.That(settings.RdpConnectionBar.Value, Is.EqualTo(RdpConnectionBarState._Default));
            Assert.That(settings.RdpAuthenticationLevel.Value, Is.EqualTo(RdpAuthenticationLevel._Default));
            Assert.That(settings.RdpColorDepth.Value, Is.EqualTo(RdpColorDepth._Default));
            Assert.That(settings.RdpAudioPlayback.Value, Is.EqualTo(RdpAudioPlayback._Default));
            Assert.That(settings.RdpRedirectClipboard.Value, Is.EqualTo(RdpRedirectClipboard._Default));
        }

        [Test]
        public void QueryStringContainsOutOfRangeValues()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "ConnectionBar=-1&DesktopSize=a&AuthenticationLevel=null&ColorDepth=&" +
                "AudioMode=9999&RedirectClipboard=b&RedirectClipboard=c&" +
                "CredentialGenerationBehavior=-11");
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.That(settings.RdpUsername.Value, Is.Null);
            Assert.That(settings.RdpPassword.Value, Is.Null);
            Assert.That(settings.RdpDomain.Value, Is.Null);
            Assert.That(settings.RdpConnectionBar.Value, Is.EqualTo(RdpConnectionBarState._Default));
            Assert.That(settings.RdpAuthenticationLevel.Value, Is.EqualTo(RdpAuthenticationLevel._Default));
            Assert.That(settings.RdpColorDepth.Value, Is.EqualTo(RdpColorDepth._Default));
            Assert.That(settings.RdpAudioPlayback.Value, Is.EqualTo(RdpAudioPlayback._Default));
            Assert.That(settings.RdpRedirectClipboard.Value, Is.EqualTo(RdpRedirectClipboard._Default));
        }

        [Test]
        public void QueryStringContainsValidUserOrDomain()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "userNAME=John%20Doe&PassworD=ignore&Domain=%20%20mydomain&");
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.That(settings.RdpUsername.Value, Is.EqualTo("John Doe"));
            Assert.That(settings.RdpPassword.Value, Is.Null);
            Assert.That(settings.RdpDomain.Value, Is.EqualTo("  mydomain"));
        }

        [Test]
        public void QueryStringContainsValidSettings()
        {
            var url = IapRdpUrl.FromString("iap-rdp:///my-project/us-central1-a/my-instance?" +
                "ConnectionBar=1&DesktopSize=1&AuthenticationLevel=0&ColorDepth=2&" +
                "AudioMode=2&RedirectClipboard=0&CredentialGenerationBehavior=0&Rdpport=13389");
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.That(settings.RdpConnectionBar.Value, Is.EqualTo(RdpConnectionBarState.Pinned));
            Assert.That(settings.RdpAuthenticationLevel.Value, Is.EqualTo(RdpAuthenticationLevel.AttemptServerAuthentication));
            Assert.That(settings.RdpColorDepth.Value, Is.EqualTo(RdpColorDepth.DeepColor));
            Assert.That(settings.RdpAudioPlayback.Value, Is.EqualTo(RdpAudioPlayback.DoNotPlay));
            Assert.That(settings.RdpRedirectClipboard.Value, Is.EqualTo(RdpRedirectClipboard.Disabled));
            Assert.That(settings.RdpPort.Value, Is.EqualTo(13389));
        }

        [Test]
        public void UrlParameterIsNullOrEmpty(
            [Values(null, "", " ")] string? emptyValue)
        {
            var queryParameters = new NameValueCollection
            {
                { "AudioMode", emptyValue }
            };

            var url = new IapRdpUrl(SampleLocator, queryParameters);
            var settings = new ConnectionSettings(url.Instance, new IapRdpUrlSettingsStore(url));

            Assert.That(settings.RdpAudioPlayback.Value, Is.EqualTo(RdpAudioPlayback._Default));
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

            Assert.That(settings.RdpAudioPlayback.Value, Is.EqualTo(RdpAudioPlayback._Default));
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

            Assert.That(settings.RdpAudioPlayback.Value, Is.EqualTo(RdpAudioPlayback.DoNotPlay));
        }
    }
}
