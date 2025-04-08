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

            Assert.AreEqual(SessionTransportType._Default, parameters.TransportType);
            Assert.AreEqual(RdpParameters.DefaultPort, parameters.Port);
            Assert.AreEqual(RdpParameters.DefaultConnectionTimeout, parameters.ConnectionTimeout);
            Assert.AreEqual(RdpConnectionBarState._Default, parameters.ConnectionBar);
            Assert.AreEqual(RdpAuthenticationLevel._Default, parameters.AuthenticationLevel);
            Assert.AreEqual(RdpColorDepth._Default, parameters.ColorDepth);
            Assert.AreEqual(RdpAudioPlayback._Default, parameters.AudioPlayback);
            Assert.AreEqual(RdpNetworkLevelAuthentication._Default, parameters.NetworkLevelAuthentication);
            Assert.AreEqual(RdpAutomaticLogon._Default, parameters.UserAuthenticationBehavior);
            Assert.AreEqual(RdpRedirectClipboard._Default, parameters.RedirectClipboard);
            Assert.AreEqual(RdpRedirectPrinter._Default, parameters.RedirectPrinter);
            Assert.AreEqual(RdpRedirectSmartCard._Default, parameters.RedirectSmartCard);
            Assert.AreEqual(RdpRedirectPort._Default, parameters.RedirectPort);
            Assert.AreEqual(RdpRedirectDrive._Default, parameters.RedirectDrive);
            Assert.AreEqual(RdpRedirectDevice._Default, parameters.RedirectDevice);
            Assert.AreEqual(RdpRedirectWebAuthn._Default, parameters.RedirectWebAuthn);
            Assert.AreEqual(RdpHookWindowsKeys._Default, parameters.HookWindowsKeys);
            Assert.AreEqual(RdpRestrictedAdminMode._Default, parameters.RestrictedAdminMode);
            Assert.AreEqual(RdpSessionType._Default, parameters.SessionType);
        }
    }
}
