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

using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.Ssh.Cryptography;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Protocol.Ssh
{
    [TestFixture]
    public class TestOsLoginCertificateSigner
    {
        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyMalformed_ThenConstructorThrowsException()
        {
            Assert.Throws<FormatException>(
                () => new OsLoginCertificateSigner(
                    new Mock<IAsymmetricKeySigner>().Object,
                    "invalid"));
        }

        [Test]
        public void WhenKeyValid_ThenConstructorSucceeds()
        {
            using (var signer = new OsLoginCertificateSigner(
                new Mock<IAsymmetricKeySigner>().Object,
                "ssh-rsa-cert-v01@openssh.com AAAA user"))
            {
                Assert.AreEqual(
                    "ssh-rsa-cert-v01@openssh.com",
                    signer.PublicKey.Type);

                Assert.AreEqual(
                    Convert.FromBase64String("AAAA"),
                    signer.PublicKey.WireFormatValue);

                Assert.AreEqual("user", signer.Username);
            }
        }
    }
}
