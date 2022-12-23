﻿//
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
using Google.Solutions.Testing.Common;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Google.Solutions.Mvvm.Test.Shell
{
    [TestFixture]
    public class TestQuarantine
    {
        //
        // EICAR dummy-malware, see
        // https://www.eicar.org/download-anti-malware-testfile/
        //
        private const string Eicar = @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

        //---------------------------------------------------------------------
        // ScanAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFileOriginatesFromInternet_ThenScanAppliesMotw()
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, "test");

            await Quarantine.ScanAsync(
                    IntPtr.Zero,
                    new FileInfo(filePath),
                    new Uri("https://example.com/"),
                    Guid.Empty)
                .ConfigureAwait(true);

            var zone = await Quarantine
                .GetZoneAsync(filePath)
                .ConfigureAwait(true);

            Assert.AreEqual(Quarantine.Zone.Internet, zone);
        }

        [Test]
        public async Task WhenFileOriginatesFromLocalMachine_ThenScanAppliesMotw()
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, "test");

            await Quarantine.ScanAsync(
                    IntPtr.Zero,
                    new FileInfo(filePath),
                    new Uri(@"c:\some\local\file\path.txt"),
                    Guid.Empty)
                .ConfigureAwait(true);

            var zone = await Quarantine
                .GetZoneAsync(filePath)
                .ConfigureAwait(true);

            Assert.AreEqual(Quarantine.Zone.LocalMachine, zone);
        }

        [Test]
        public void WhenFileIsMalicious_ThenScanThrowsException()
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, Eicar);

            ExceptionAssert.ThrowsAggregateException<QuarantineException>(
                () => Quarantine.ScanAsync(
                    IntPtr.Zero,
                    new FileInfo(filePath),
                    new Uri(@"c:\some\local\file\path.txt"),
                    Guid.Empty).Wait());
        }

        //---------------------------------------------------------------------
        // GetSourceZoneAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenFileIsLocal_ThenGetSourceZoneReturnsLocalMachine()
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, "test");

            var zone = await Quarantine
                .GetZoneAsync(filePath)
                .ConfigureAwait(true);

            Assert.AreEqual(Quarantine.Zone.LocalMachine, zone);
        }
    }
}
