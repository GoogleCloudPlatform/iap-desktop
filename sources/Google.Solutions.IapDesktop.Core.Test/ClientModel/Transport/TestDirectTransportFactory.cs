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
// Profileific language governing permissions and limitations
// under the License.
//

using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Transport
{
    [TestFixture]
    public class TestDirectTransportFactory
    {
        //---------------------------------------------------------------------
        // CreateTransport.
        //---------------------------------------------------------------------

        [Test]
        public async Task CreateTransport()
        {
            var protocol = new Mock<IProtocol>();
            protocol.SetupGet(p => p.Id).Returns("mock");

            var endpoint = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 22);

            var factory = new DirectTransportFactory();
            using (var transport = await factory
                .CreateTransportAsync(protocol.Object, endpoint, CancellationToken.None)
                .ConfigureAwait(false))
            {
                Assert.AreSame(protocol.Object, transport.Protocol);
                Assert.AreSame(endpoint, transport.Endpoint);
            }
        }
    }
}
