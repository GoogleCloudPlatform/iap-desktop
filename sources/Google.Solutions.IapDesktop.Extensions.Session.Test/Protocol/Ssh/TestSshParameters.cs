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
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
{
    [TestFixture]
    public class TestSshParameters
    {
        [Test]
        public void ParametersUseDefaults()
        {
            var parameters = new SshParameters();

            Assert.That(parameters.Language, Is.Null);

            Assert.That(parameters.TransportType, Is.EqualTo(SessionTransportType._Default));
            Assert.That(parameters.Port, Is.EqualTo(SshParameters.DefaultPort));
            Assert.That(parameters.ConnectionTimeout, Is.EqualTo(SshParameters.DefaultConnectionTimeout));
            Assert.That(parameters.PreferredUsername, Is.Null);
            Assert.That(parameters.PublicKeyValidity, Is.EqualTo(SshParameters.DefaultPublicKeyValidity));
        }
    }
}
