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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Testbed;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using NUnit.Framework;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("IAP")]
    public class TestEchoOverIapDirectTunnel : TestEchoOverIapBase
    {
        protected override INetworkStream ConnectToEchoServer(
            InstanceLocator vmRef,
            ICredential credential)
        {
            return new FragmentingStream(new SshRelayStream(
                new IapTunnelingEndpoint(
                    credential,
                    vmRef,
                    7,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    Defaults.UserAgent)));
        }
    }
}
