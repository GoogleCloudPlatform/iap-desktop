//
// Copyright 2022 Google LLC
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

using Google.Solutions.Mvvm.Shell;
using NUnit.Framework;
using System.Runtime.InteropServices;
using System.Threading;

namespace Google.Solutions.Mvvm.Test.Shell
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestStockIcons
    {
        [Test]
        public void GetIcon_WhenIdInvalid_ThenGetIconThrowsException()
        {
            Assert.Throws<COMException>(
                () => StockIcons.GetIcon(
                    (StockIcons.IconId)int.MaxValue,
                    StockIcons.IconSize.Small));
        }

        [Test]
        public void GetIcon_WhenIdValid_ThenGetLargeIconSucceeds()
        {
            using (var icon = StockIcons.GetIcon(
                StockIcons.IconId.Server,
                StockIcons.IconSize.Large))
            {
                Assert.IsNotNull(icon);
                Assert.That(icon.Width, Is.EqualTo(32));
            }
        }

        [Test]
        public void GetIcon_WhenIdValid_ThenGetSmallIconSucceeds()
        {
            using (var icon = StockIcons.GetIcon(
                StockIcons.IconId.Server,
                StockIcons.IconSize.Small))
            {
                Assert.IsNotNull(icon);
                Assert.That(icon.Width, Is.EqualTo(16));
            }
        }
    }
}
