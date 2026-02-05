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

using Google.Solutions.Common.Format;
using NUnit.Framework;
using System.Text;

namespace Google.Solutions.Common.Test.Format
{
    [TestFixture]
    public class TestBase64UrlEncoding
    {
        [Test]
        public void Encode()
        {
            Assert.That(
                Base64UrlEncoding.Encode(Encoding.ASCII.GetBytes("")), Is.EqualTo(""));
            Assert.That(
                Base64UrlEncoding.Encode(Encoding.ASCII.GetBytes("1")), Is.EqualTo("MQ"));
            Assert.That(
                Base64UrlEncoding.Encode(Encoding.ASCII.GetBytes("12")), Is.EqualTo("MTI"));
            Assert.That(
                Base64UrlEncoding.Encode(Encoding.ASCII.GetBytes("123")), Is.EqualTo("MTIz"));
            Assert.That(
                Base64UrlEncoding.Encode(Encoding.ASCII.GetBytes("1234")), Is.EqualTo("MTIzNA"));
        }

        [Test]
        public void Decode()
        {
            Assert.That(
                Encoding.ASCII.GetString(Base64UrlEncoding.Decode("")), Is.EqualTo(""));
            Assert.That(
                Encoding.ASCII.GetString(Base64UrlEncoding.Decode("MQ")), Is.EqualTo("1"));
            Assert.That(
                Encoding.ASCII.GetString(Base64UrlEncoding.Decode("MTI")), Is.EqualTo("12"));
            Assert.That(
                Encoding.ASCII.GetString(Base64UrlEncoding.Decode("MTIz")), Is.EqualTo("123"));
            Assert.That(
                Encoding.ASCII.GetString(Base64UrlEncoding.Decode("MTIzNA")), Is.EqualTo("1234"));
        }
    }
}
