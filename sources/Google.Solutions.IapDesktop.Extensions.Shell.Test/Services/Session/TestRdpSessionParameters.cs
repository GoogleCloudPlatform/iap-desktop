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
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Session;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
using Moq;
using NUnit.Framework;
using System.Security;
using System.Threading;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Services.Session
{
    [TestFixture]
    public class TestRdpSessionParameters
    {
        [Test]
        public void ParametersUseDefaults()
        {
            var parameters = new RdpSessionParameters();

            Assert.AreEqual(Transport.TransportType._Default, parameters.TransportType);
            Assert.AreEqual(RdpSessionParameters.DefaultPort, parameters.Port);
            Assert.AreEqual(RdpSessionParameters.DefaultConnectionTimeout, parameters.ConnectionTimeout);
            Assert.AreEqual(RdpConnectionBarState._Default, parameters.ConnectionBar);
            Assert.AreEqual(RdpDesktopSize._Default, parameters.DesktopSize);
            Assert.AreEqual(RdpAuthenticationLevel._Default, parameters.AuthenticationLevel);
            Assert.AreEqual(RdpColorDepth._Default, parameters.ColorDepth);
            Assert.AreEqual(RdpAudioMode._Default, parameters.AudioMode);
            Assert.AreEqual(RdpBitmapPersistence._Default, parameters.BitmapPersistence);
            Assert.AreEqual(RdpNetworkLevelAuthentication._Default, parameters.NetworkLevelAuthentication);
            Assert.AreEqual(RdpUserAuthenticationBehavior._Default, parameters.UserAuthenticationBehavior);
            Assert.AreEqual(RdpRedirectClipboard._Default, parameters.RedirectClipboard);
            Assert.AreEqual(RdpRedirectPrinter._Default, parameters.RedirectPrinter);
            Assert.AreEqual(RdpRedirectSmartCard._Default, parameters.RedirectSmartCard);
            Assert.AreEqual(RdpRedirectPort._Default, parameters.RedirectPort);
            Assert.AreEqual(RdpRedirectDrive._Default, parameters.RedirectDrive);
            Assert.AreEqual(RdpRedirectDevice._Default, parameters.RedirectDevice);
            Assert.AreEqual(RdpHookWindowsKeys._Default, parameters.HookWindowsKeys);
        }
    }
}
