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

using Google.Solutions.Apis.Auth;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.Ssh.Cryptography;
using Moq;
using NUnit.Framework;
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
{
    [TestFixture]
    public class TestCngKeyName
    {
        //---------------------------------------------------------------------
        // Value, Type.
        //---------------------------------------------------------------------

        [Test]
        public void RsaWithDefaultProvider()
        {
            var session = new Mock<IOidcSession>();
            session.SetupGet(s => s.Username).Returns("user");

            var name = new CngKeyName(
                session.Object,
                SshKeyType.Rsa3072,
                CngProvider.MicrosoftSoftwareKeyStorageProvider);

            Assert.That(name.Value, Is.EqualTo("IAPDESKTOP_user"));
            Assert.That(name.ToString(), Is.EqualTo("IAPDESKTOP_user"));

            Assert.That(name.Type.Algorithm, Is.EqualTo(CngAlgorithm.Rsa));
            Assert.That(name.Type.Size, Is.EqualTo(3072));
        }

        [Test]
        public void EcdsaP256WithDefaultProvider()
        {
            var session = new Mock<IOidcSession>();
            session.SetupGet(s => s.Username).Returns("user");

            var name = new CngKeyName(
                session.Object,
                SshKeyType.EcdsaNistp256,
                CngProvider.MicrosoftSoftwareKeyStorageProvider);

            Assert.That(name.Value, Is.EqualTo("IAPDESKTOP_user_00000011_094FE673"));
            Assert.That(name.ToString(), Is.EqualTo("IAPDESKTOP_user_00000011_094FE673"));

            Assert.That(name.Type.Algorithm, Is.EqualTo(CngAlgorithm.ECDsaP256));
            Assert.That(name.Type.Size, Is.EqualTo(256));
        }

        [Test]
        public void EcdsaP384WithDefaultProvider()
        {
            var session = new Mock<IOidcSession>();
            session.SetupGet(s => s.Username).Returns("user");

            var name = new CngKeyName(
                session.Object,
                SshKeyType.EcdsaNistp384,
                CngProvider.MicrosoftSoftwareKeyStorageProvider);

            Assert.That(name.Value, Is.EqualTo("IAPDESKTOP_user_00000012_094FE673"));
            Assert.That(name.ToString(), Is.EqualTo("IAPDESKTOP_user_00000012_094FE673"));

            Assert.That(name.Type.Algorithm, Is.EqualTo(CngAlgorithm.ECDsaP384));
            Assert.That(name.Type.Size, Is.EqualTo(384));
        }

        [Test]
        public void EcdsaP521WithDefaultProvider()
        {
            var session = new Mock<IOidcSession>();
            session.SetupGet(s => s.Username).Returns("user");

            var name = new CngKeyName(
                session.Object,
                SshKeyType.EcdsaNistp521,
                CngProvider.MicrosoftSoftwareKeyStorageProvider);

            Assert.That(name.Value, Is.EqualTo("IAPDESKTOP_user_00000013_094FE673"));
            Assert.That(name.ToString(), Is.EqualTo("IAPDESKTOP_user_00000013_094FE673"));

            Assert.That(name.Type.Algorithm, Is.EqualTo(CngAlgorithm.ECDsaP521));
            Assert.That(name.Type.Size, Is.EqualTo(521));
        }
    }
}
