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


using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Platform.Dispatch;
using Moq;
using NUnit.Framework;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.App
{
    [TestFixture]
    public class TestAppContextFactory
    {
        //---------------------------------------------------------------------
        // CreateContext.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTargetUnsupported_ThenCreateContextThrowsException()
        {
            Assert.Fail();
        }

        [Test]
        public void WhenFlagsClear_ThenCreateContextUsesNoNetworkCredentials()
        {
            Assert.Fail();
        }

        [Test]
        public void WhenTryUseRdpNetworkCredentialsIsSetButNoCredentialsFound_ThenCreateContextUsesNoNetworkCredentials()
        {
            Assert.Fail();
        }

        [Test]
        public void WhenTryUseRdpNetworkCredentials_ThenCreateContextUsesRdpNetworkCredentials()
        {
            Assert.Fail();
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse()
        {
            var factory = new AppContextFactory(
                new AppProtocol(
                    "app-1",
                    Enumerable.Empty<ITrait>(),
                    new Mock<ITransportPolicy>().Object,
                    80,
                    null,
                    new Mock<IAppProtocolClient>().Object),
                new Mock<IIapTransportFactory>().Object,
                new Mock<IWin32ProcessFactory>().Object,
                new Mock<IConnectionSettingsService>().Object);

            Assert.IsFalse(factory.TryParse(new System.Uri("app-1:///test"), out var _));
        }
    }
}
