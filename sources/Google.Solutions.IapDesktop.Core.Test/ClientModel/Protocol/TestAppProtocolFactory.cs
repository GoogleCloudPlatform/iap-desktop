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
using Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies;
using NUnit.Framework;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestAppProtocolFactory
    {
        //---------------------------------------------------------------------
        // FromJson.
        //---------------------------------------------------------------------

        [Test]
        public void WhenJsonIsNullOrEmptyOrMalformed_ThenFromJsonThrowsException(
            [Values(null, " ", "{,", "{}")] string json)
        {
            Assert.Throws<InvalidAppProtocolException>(
                () => new AppProtocolFactory().FromJson(json));
        }

        [Test]
        public void WhenJsonContainsNoVersion_ThenFromJsonThrowsException()
        {
            var json = @"
                {
                    'name': 'protocol-1',
                    'condition': 'isWindows()',
                    'accessPolicy': 'AllowAll',
                    'remotePort': 8080
                }";

            Assert.Throws<InvalidAppProtocolException>(
                () => new AppProtocolFactory().FromJson(json));
        }

        [Test]
        public void WhenJsonContainsUnsupportedVersion_ThenFromJsonThrowsException()
        {
            var json = @"
                {
                    'version': 0,
                    'name': 'protocol-1',
                    'condition': 'isWindows()',
                    'accessPolicy': 'AllowAll',
                    'remotePort': 8080
                }";

            Assert.Throws<InvalidAppProtocolException>(
                () => new AppProtocolFactory().FromJson(json));
        }

        [Test]
        public void WhenJsonIsValid_ThenFromJsonReturnsProtocol()
        {
            var json = @"
                {
                    'version': 1,
                    'name': 'protocol-1',
                    'condition': 'isWindows()',
                    'accessPolicy': 'AllowAll',
                    'remotePort': 8080,
                    'client': {
                        'executable': 'cmd'
                    }
                }";

            var protocol = new AppProtocolFactory().FromJson(json);
            Assert.AreEqual("protocol-1", protocol.Name);
            Assert.IsInstanceOf<WindowsTrait>(protocol.RequiredTraits.First());
            Assert.IsInstanceOf<AllowAllPolicy>(protocol.Policy);
            Assert.AreEqual(8080, protocol.RemotePort);

            var client = (AppProtocolClient)protocol.Client;
            Assert.AreEqual("cmd", client.Executable);
        }
    }
}
