﻿//
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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Views.RemoteDesktop;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Views
{
    [TestFixture]
    public class TestRemoteDesktop : WindowTestFixtureBase
    {
        private readonly InstanceLocator instanceReference =
            new InstanceLocator("project", "zone", "instance");

        [Test]
        public void WhenServerInvalid_ThenErrorIsShownAndWindowIsClosed()
        {
            var rdpService = new RemoteDesktopService(this.serviceProvider);
            rdpService.Connect(
                this.instanceReference,
                "invalid.corp",
                3389,
                new VmInstanceConnectionSettings());

            AwaitEvent<RemoteDesktopConnectionFailedEvent>();
            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(260, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }

        [Test]
        public void WhenPortNotListening_ThenErrorIsShownAndWindowIsClosed()
        {
            var rdpService = new RemoteDesktopService(this.serviceProvider);
            rdpService.Connect(
                this.instanceReference,
                "localhost",
                1,
                new VmInstanceConnectionSettings()
                {
                    ConnectionTimeout = 5
                });

            AwaitEvent<RemoteDesktopConnectionFailedEvent>();
            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(516, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }

        [Test]
        [Ignore("")]
        public void WhenWrongPort_ThenErrorIsShownAndWindowIsClosed()
        {
            var rdpService = new RemoteDesktopService(this.serviceProvider);
            rdpService.Connect(
                this.instanceReference,
                "localhost",
                135,    // That one will be listening, but it is RPC, not RDP.
                new VmInstanceConnectionSettings());

            AwaitEvent<RemoteDesktopConnectionFailedEvent>();
            Assert.IsInstanceOf(typeof(RdpDisconnectedException), this.ExceptionShown);
            Assert.AreEqual(2308, ((RdpDisconnectedException)this.ExceptionShown).DisconnectReason);
        }
    }
}
