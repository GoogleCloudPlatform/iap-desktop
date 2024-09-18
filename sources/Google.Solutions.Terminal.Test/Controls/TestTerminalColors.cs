//
// Copyright 2024 Google LLC
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


using Google.Solutions.Terminal.Controls;
using NUnit.Framework;

namespace Google.Solutions.Terminal.Test.Controls
{
    [TestFixture]
    public class TestTerminalColors
    {
        [Test]
        public void Default_ToNative()
        {
            var colorTable = new uint[] { 
                0x0C0C0C, 
                0x1F0FC5, 
                0x0EA113,
                0x009CC1, 
                0xDA3700, 
                0x981788, 
                0xDD963A,
                0xCCCCCC, 
                0x767676,
                0x5648E7,
                0x0CC616,
                0xA5F1F9,
                0xFF783B, 
                0x9E00B4, 
                0xD6D661, 
                0xF2F2F2 };

            CollectionAssert.AreEqual(colorTable, TerminalColors.Default.ToNative());
        }
    }
}
