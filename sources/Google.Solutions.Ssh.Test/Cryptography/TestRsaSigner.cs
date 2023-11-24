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
using System.Security.Cryptography;

namespace Google.Solutions.Ssh.Test.Cryptography
{
    [TestFixture]
    public class TestRsaSigner
    {
        //---------------------------------------------------------------------
        // Sign.
        //---------------------------------------------------------------------

        [Test]
        public void WhenChallengeRequiresSha512_ThenSignReturnsSignature()
        {
            var challengeBlob = Convert.FromBase64String(
                "AAAAIEVr/Hy4lWvHE87XI+c+jchQ4kkz/gCEpWzdIYU+PLvjMgAAAAh0ZX" +
                "N0dXNlcgAAAA5zc2gtY29ubmVjdGlvbgAAAAlwdWJsaWNrZXkBAAAADHJz" +
                "YS1zaGEyLTUxMgAAAZcAAAAHc3NoLXJzYQAAAAMBAAEAAAGBAN1l+hknPO" +
                "InaZOAYM6Y6dDf9fZFE1nZntCT53HF8zSNhVv3cDaIgtODzyvd3IsUnCZQ" +
                "VSVQJ0tlcmlKodKpo5Xu0MzQA1y+XjIfnSU8udjY6wwSSp4mGRQ7aeYAQH" +
                "e5zo4kSDrAgcRLgV/teHpHy4l00qMWUFbFRfcnu0gaIXPMr00p1xk/1GJH" +
                "Rd+1Hucd+RPTTFq2AVNqtT2+UcVYi1EzeXsjbJ+Pv6oovF2yScRuJFfK5C" +
                "+OSEJKCPg1QcZHHUq62Mu3A5hxsomk02ZW42xmgIACUpE317mvuu5DkS/V" +
                "isVy63M236lF5vyMbFSpJH1ze00sZh3l027qGD0VdjpV/V/5C6dTNzAYiF" +
                "vghcxmuAJ/VszYQ6fTPnPpkan+aMWm2w+bWo/q3nRNmgjtQjUQoVM0/TNN" +
                "W+r+/Us0iPG+jgWQO27TUROaFkeiX/epRXYAmT68w6uTU4CCv9oXY93mKN" +
                "xn839ZTP+RzKaVZytKQLuCkh3u0Re8xZl0JM+pAQ==");

            var challenge = new AuthenticationChallenge(challengeBlob);
            Assert.AreEqual("rsa-sha2-512", challenge.Algorithm);

            using (var cngKey = new RSACng())
            using (var signer = new RsaSigner(cngKey, true))
            {
                CollectionAssert.AreEqual(
                    cngKey.SignData(
                        challengeBlob, 
                        HashAlgorithmName.SHA512, 
                        RSASignaturePadding.Pkcs1),
                    signer.Sign(challenge));
            }
        }

        [Test]
        public void WhenChallengeRequiresSha256_ThenSignReturnsSignature()
        {
            var challengeBlob = Convert.FromBase64String(
                "AAAAIEVr/Hy4lWvHE87XI+c+jchQ4kkz/gCEpWzdIYU+PLvjMgAAAAh0ZX" +
                "N0dXNlcgAAAA5zc2gtY29ubmVjdGlvbgAAAAlwdWJsaWNrZXkBAAAADHJz" +
                "YS1zaGEyLTI1NgAAAZcAAAAHc3NoLXJzYQAAAAMBAAEAAAGBAN1l+hknPO" +
                "InaZOAYM6Y6dDf9fZFE1nZntCT53HF8zSNhVv3cDaIgtODzyvd3IsUnCZQ" +
                "VSVQJ0tlcmlKodKpo5Xu0MzQA1y+XjIfnSU8udjY6wwSSp4mGRQ7aeYAQH" +
                "e5zo4kSDrAgcRLgV/teHpHy4l00qMWUFbFRfcnu0gaIXPMr00p1xk/1GJH" +
                "Rd+1Hucd+RPTTFq2AVNqtT2+UcVYi1EzeXsjbJ+Pv6oovF2yScRuJFfK5C" +
                "+OSEJKCPg1QcZHHUq62Mu3A5hxsomk02ZW42xmgIACUpE317mvuu5DkS/V" +
                "isVy63M236lF5vyMbFSpJH1ze00sZh3l027qGD0VdjpV/V/5C6dTNzAYiF" +
                "vghcxmuAJ/VszYQ6fTPnPpkan+aMWm2w+bWo/q3nRNmgjtQjUQoVM0/TNN" +
                "W+r+/Us0iPG+jgWQO27TUROaFkeiX/epRXYAmT68w6uTU4CCv9oXY93mKN" +
                "xn839ZTP+RzKaVZytKQLuCkh3u0Re8xZl0JM+pAQ==");

            var challenge = new AuthenticationChallenge(challengeBlob);
            Assert.AreEqual("rsa-sha2-256", challenge.Algorithm);

            using (var cngKey = new RSACng())
            using (var signer = new RsaSigner(cngKey, true))
            {
                CollectionAssert.AreEqual(
                    cngKey.SignData(
                        challengeBlob, 
                        HashAlgorithmName.SHA256, 
                        RSASignaturePadding.Pkcs1),
                    signer.Sign(challenge));
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOwnsKeyIsTrue_ThenDisposeClosesKey()
        {
            var key = new RSACng();
            using (var signer = new RsaSigner(key, true))
            {
                Assert.IsFalse(key.IsDisposed());
            }

            using (var signer = new RsaSigner(key, true))
            {
                Assert.IsTrue(key.IsDisposed());
            }
        }

        [Test]
        public void WhenOwnsKeyIsFalse_ThenDisposeClosesKey()
        {
            using (var key = new RSACng())
            {
                using (var signer = new RsaSigner(key, false))
                {
                    Assert.IsFalse(key.IsDisposed());
                }

                using (var signer = new RsaSigner(key, false))
                {
                    Assert.IsFalse(key.IsDisposed());
                }
            }
        }
    }
}
