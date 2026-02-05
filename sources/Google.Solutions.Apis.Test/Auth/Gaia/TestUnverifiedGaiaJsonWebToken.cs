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


using Google.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using NUnit.Framework;

namespace Google.Solutions.Apis.Test.Auth.Gaia
{
    [TestFixture]
    public class TestUnverifiedGaiaJsonWebToken
    {
        private const string SampleJwtWithInvalidSignature
            = "eyJ0eXAiOiJqd3QifQ.eyJlbWFpbCI6InhAZXhhbXBsZS5jb20iLCJlbWFpbF92ZXJpZmllZCI6ZmFsc2V9.nosig";

        //---------------------------------------------------------------------
        // Decode.
        //---------------------------------------------------------------------

        [Test]
        public void Decode_WhenTokenMissesPart()
        {
            Assert.Throws<InvalidJwtException>(
                () => UnverifiedGaiaJsonWebToken.Decode("a"));
            Assert.Throws<InvalidJwtException>(
                () => UnverifiedGaiaJsonWebToken.Decode("a.b"));
        }

        [Test]
        public void Decode_WhenJsonIsMalformed()
        {
            Assert.Throws<InvalidJwtException>(
                () => UnverifiedGaiaJsonWebToken.Decode("YQ.YQ.YQ"));
        }

        [Test]
        public void Decode()
        {
            var jwt = UnverifiedGaiaJsonWebToken.Decode(SampleJwtWithInvalidSignature);
            Assert.That(jwt.Header.Type, Is.EqualTo("jwt"));
            Assert.That(jwt.Payload.Email, Is.EqualTo("x@example.com"));
        }

        //---------------------------------------------------------------------
        // TryDecode.
        //---------------------------------------------------------------------

        [Test]
        public void TryDecode_WhenJsonIsMalformed()
        {
            Assert.IsFalse(UnverifiedGaiaJsonWebToken.TryDecode("YQ.YQ.YQ", out var _));
        }

        [Test]
        public void TryDecode()
        {
            Assert.IsTrue(UnverifiedGaiaJsonWebToken.TryDecode(
                SampleJwtWithInvalidSignature,
                out var jwt));
            Assert.That(jwt!.Header.Type, Is.EqualTo("jwt"));
            Assert.That(jwt.Payload.Email, Is.EqualTo("x@example.com"));
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToString_ReturnsEncodedToken()
        {
            var jwt = UnverifiedGaiaJsonWebToken.Decode(SampleJwtWithInvalidSignature);
            Assert.That(jwt.ToString(), Is.EqualTo(SampleJwtWithInvalidSignature));
        }
    }
}
