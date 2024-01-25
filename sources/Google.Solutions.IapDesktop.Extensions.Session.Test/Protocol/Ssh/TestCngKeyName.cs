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

            Assert.AreEqual("IAPDESKTOP_user", name.Value);
            Assert.AreEqual("IAPDESKTOP_user", name.ToString());

            Assert.AreEqual(CngAlgorithm.Rsa, name.Type.Algorithm);
            Assert.AreEqual(3072, name.Type.Size);
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

            Assert.AreEqual("IAPDESKTOP_user_00000011_094FE673", name.Value);
            Assert.AreEqual("IAPDESKTOP_user_00000011_094FE673", name.ToString());

            Assert.AreEqual(CngAlgorithm.ECDsaP256, name.Type.Algorithm);
            Assert.AreEqual(256, name.Type.Size);
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

            Assert.AreEqual("IAPDESKTOP_user_00000012_094FE673", name.Value);
            Assert.AreEqual("IAPDESKTOP_user_00000012_094FE673", name.ToString());

            Assert.AreEqual(CngAlgorithm.ECDsaP384, name.Type.Algorithm);
            Assert.AreEqual(384, name.Type.Size);
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

            Assert.AreEqual("IAPDESKTOP_user_00000013_094FE673", name.Value);
            Assert.AreEqual("IAPDESKTOP_user_00000013_094FE673", name.ToString());

            Assert.AreEqual(CngAlgorithm.ECDsaP521, name.Type.Algorithm);
            Assert.AreEqual(521, name.Type.Size);
        }
    }
}
