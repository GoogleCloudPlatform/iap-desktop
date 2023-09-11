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

using Google.Solutions.Ssh.Cryptography;
using NUnit.Framework;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Test.Cryptography
{
    [TestFixture]
    public class TestRsaPublicKey
    {
        private const string SampleKey =
            "AAAAB3NzaC1yc2EAAAADAQABAAAAgQCrt1m7xe5Rd8DlltVnwx17WTt" +
            "m2HCpvGsHCRo/BDlKso5YleTqoHPXYVz0Z8UXmUX14qgnRScySBYVSXqYYdh6j7" +
            "ZPlI3ehrZ2vn0WEJSvcB/gi+VrcNfVEvvn2EFOip1fXZxFcdVhvhZp5HRTUzzKx" +
            "spcMRM9OvY+qgJIveT7cw==";

        //---------------------------------------------------------------------
        // Type.
        //---------------------------------------------------------------------

        [Test]
        public void Type()
        {
            using (var key = new RSACng())
            using (var publicKey = new RsaPublicKey(key))
            {
                Assert.AreEqual("rsa-ssh", publicKey.Type);
            }
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDecodedAndEncoded_ThenValueIsEqual()
        {
            using (var key = new RSACng())
            using (var publicKey = new RsaPublicKey(key))
            using (var reencodedPublicKey = new RsaPublicKey(publicKey.Value))
            {
                CollectionAssert.AreEqual(publicKey.Value, reencodedPublicKey.Value);
            }
        }

        [Test]
        public void WhenKeyValid_ThenCtorSucceeds()
        {
            using (var publicKey = new RsaPublicKey(Convert.FromBase64String(SampleKey)))
            {
            }
        }

        [Test]
        public void WhenKeyTruncated_ThenConstructorThrowsException()
        {
            Assert.Throws<SshFormatException>(
                () => new RsaPublicKey(Convert.FromBase64String(SampleKey).Take(30).ToArray()));
        }
    }
}
