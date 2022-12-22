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
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Mvvm.Test.Shell
{
    [TestFixture]
    public class TestMarkOfTheWeb
    {
        //---------------------------------------------------------------------
        // ScanAndApplyZoneAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFileOriginatesFromInternet_ThenScanAndApplyZoneAppliesMotw()
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, "test");

            await MarkOfTheWeb.ScanAndApplyZoneAsync(
                    IntPtr.Zero,
                    filePath,
                    new Uri("https://example.com/"),
                    Guid.Empty)
                .ConfigureAwait(true);

            var zone = await MarkOfTheWeb
                .GetZoneAsync(filePath)
                .ConfigureAwait(true);

            Assert.AreEqual(MarkOfTheWeb.Zone.Internet, zone);
        }

        [Test]
        public async Task WhenFileOriginatesFromLocalMachine_ThenScanAndApplyZoneAppliesMotw()
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, "test");

            await MarkOfTheWeb.ScanAndApplyZoneAsync(
                    IntPtr.Zero,
                    filePath,
                    new Uri(@"c:\some\local\file\path.txt"),
                    Guid.Empty)
                .ConfigureAwait(true);

            var zone = await MarkOfTheWeb
                .GetZoneAsync(filePath)
                .ConfigureAwait(true);

            Assert.AreEqual(MarkOfTheWeb.Zone.LocalMachine, zone);
        }

        //---------------------------------------------------------------------
        // GetZoneAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFileIsLocal_ThenGetZoneReturnsLocalMachine()
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, "test");

            var zone = await MarkOfTheWeb
                .GetZoneAsync(filePath)
                .ConfigureAwait(true);

            Assert.AreEqual(MarkOfTheWeb.Zone.LocalMachine, zone);
        }
    }
}
