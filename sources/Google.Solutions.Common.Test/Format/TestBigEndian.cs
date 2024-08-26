//
// Copyright 2019 Google LLC
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

namespace Google.Solutions.Common.Test.Format
{
    [TestFixture]
    public class TestBigEndian : CommonFixtureBase
    {
        [Test]
        public void EncodeDecode_Int16()
        {
            var buffer = new byte[2];
            BigEndian.EncodeUInt16(ushort.MaxValue, buffer, 0);
            Assert.AreEqual(ushort.MaxValue, BigEndian.DecodeUInt16(buffer, 0));

            BigEndian.EncodeUInt16(0xABCD, buffer, 0);
            Assert.AreEqual(0xABCD, BigEndian.DecodeUInt16(buffer, 0));
        }

        [Test]
        public void EncodeDecode_Int16WithOffset()
        {
            var buffer = new byte[4];
            BigEndian.EncodeUInt16(ushort.MaxValue, buffer, 2);
            Assert.AreEqual(0, buffer[0]);
            Assert.AreEqual(0, buffer[1]);
            Assert.AreEqual(ushort.MaxValue, BigEndian.DecodeUInt16(buffer, 2));
        }

        [Test]
        public void EncodeDecode_Int32()
        {
            var buffer = new byte[4];
            BigEndian.EncodeUInt32(uint.MaxValue, buffer, 0);
            Assert.AreEqual(uint.MaxValue, BigEndian.DecodeUInt32(buffer, 0));

            BigEndian.EncodeUInt32(0xABCDEF12, buffer, 0);
            Assert.AreEqual(0xABCDEF12, BigEndian.DecodeUInt32(buffer, 0));
        }

        [Test]
        public void EncodeDecode_Int64()
        {
            var buffer = new byte[8];
            BigEndian.EncodeUInt64(ulong.MaxValue, buffer, 0);
            Assert.AreEqual(ulong.MaxValue, BigEndian.DecodeUInt64(buffer, 0));


            BigEndian.EncodeUInt64(0xABCDEF12DEADBEEFL, buffer, 0);
            Assert.AreEqual(0xABCDEF12DEADBEEFL, BigEndian.DecodeUInt64(buffer, 0));
        }
    }
}
