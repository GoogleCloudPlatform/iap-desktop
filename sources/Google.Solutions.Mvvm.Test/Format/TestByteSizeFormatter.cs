//
// Copyright 2020 Google LLC
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

using Google.Solutions.Mvvm.Format;
using NUnit.Framework;

namespace Google.Solutions.Mvvm.Test.Format
{
    [TestFixture]
    public class TestByteSizeFormatter
    {
        [Test]
        public void Format_WhenDataExceedsExabyte_ThenVolumeShownAsEB()
        {
            var volume = 2ul * 1024 * 1024 * 1024 * 1024 * 1024 * 1024;
            Assert.AreEqual("2 EB", ByteSizeFormatter.Format(volume));
        }

        [Test]
        public void Format_WhenDataExceedsPetabyte_ThenVolumeShownAsPB()
        {
            var volume = 2ul * 1024 * 1024 * 1024 * 1024 * 1024;
            Assert.AreEqual("2 PB", ByteSizeFormatter.Format(volume));
        }

        [Test]
        public void Format_WhenDataExceedsTerabyte_ThenVolumeShownAsTB()
        {
            var volume = 2ul * 1024 * 1024 * 1024 * 1024;
            Assert.AreEqual("2 TB", ByteSizeFormatter.Format(volume));
        }

        [Test]
        public void Format_WhenDataExceedsGigabyte_ThenVolumeShownAsGB()
        {
            var volume = 2ul * 1024 * 1024 * 1024;
            Assert.AreEqual("2 GB", ByteSizeFormatter.Format(volume));
        }

        [Test]
        public void Format_WhenDataExceedsMegabyte_ThenVolumeShownAsMB()
        {
            var volume = 2ul * 1024 * 1024;
            Assert.AreEqual("2 MB", ByteSizeFormatter.Format(volume));
        }

        [Test]
        public void Format_WhenDataExceedsKilobyte_ThenVolumeShownAsKB()
        {
            var volume = 2ul * 1024 + 200;
            Assert.AreEqual("2.2 KB", ByteSizeFormatter.Format(volume));
        }

        [Test]
        public void Format_WhenDataLessThanKilobyte_ThenVolumeShownAsB()
        {
            var volume = 765ul;
            Assert.AreEqual("765 B", ByteSizeFormatter.Format(volume));
        }
    }
}
