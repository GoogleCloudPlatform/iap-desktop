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


using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Rdp
{
    [TestFixture]
    public class TestRdpParameters
    {
        [Test]
        public void ParametersUseDefaults()
        {
            var parameters = new RdpParameters();

            Assert.That(parameters.TransportType, Is.EqualTo(SessionTransportType._Default));
            Assert.That(parameters.Port, Is.EqualTo(RdpParameters.DefaultPort));
            Assert.That(parameters.ConnectionTimeout, Is.EqualTo(RdpParameters.DefaultConnectionTimeout));
            Assert.That(parameters.ConnectionBar, Is.EqualTo(RdpConnectionBarState._Default));
            Assert.That(parameters.AuthenticationLevel, Is.EqualTo(RdpAuthenticationLevel._Default));
            Assert.That(parameters.ColorDepth, Is.EqualTo(RdpColorDepth._Default));
            Assert.That(parameters.AudioPlayback, Is.EqualTo(RdpAudioPlayback._Default));
            Assert.That(parameters.NetworkLevelAuthentication, Is.EqualTo(RdpNetworkLevelAuthentication._Default));
            Assert.That(parameters.UserAuthenticationBehavior, Is.EqualTo(RdpAutomaticLogon._Default));
            Assert.That(parameters.RedirectClipboard, Is.EqualTo(RdpRedirectClipboard._Default));
            Assert.That(parameters.RedirectPrinter, Is.EqualTo(RdpRedirectPrinter._Default));
            Assert.That(parameters.RedirectSmartCard, Is.EqualTo(RdpRedirectSmartCard._Default));
            Assert.That(parameters.RedirectPort, Is.EqualTo(RdpRedirectPort._Default));
            Assert.That(parameters.RedirectDrive, Is.EqualTo(RdpRedirectDrive._Default));
            Assert.That(parameters.RedirectDevice, Is.EqualTo(RdpRedirectDevice._Default));
            Assert.That(parameters.RedirectWebAuthn, Is.EqualTo(RdpRedirectWebAuthn._Default));
            Assert.That(parameters.HookWindowsKeys, Is.EqualTo(RdpHookWindowsKeys._Default));
            Assert.That(parameters.RestrictedAdminMode, Is.EqualTo(RdpRestrictedAdminMode._Default));
            Assert.That(parameters.SessionType, Is.EqualTo(RdpSessionType._Default));
        }
    }
}
